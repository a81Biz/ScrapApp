using ScraperApp.Models;
using ScraperApp.Scrapers;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

class Program
{
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(10);

    static async Task Main(string[] args)
    {
        // Configurar un logger simple
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(); // Agregar salida a la consola
        });

        ILogger<ProductListScraper> listLogger = loggerFactory.CreateLogger<ProductListScraper>();
        ILogger<ProductDetailScraper> detailLogger = loggerFactory.CreateLogger<ProductDetailScraper>();

        // Crear una instancia de ApiService
        var apiService = new ApiService();

        List<ListProducts> listProducts = new List<ListProducts>();
        List<string> retriedUrls = new List<string>();

        // Instancia de scrapers
        var productDetail = new ProductDetailScraper(apiService, detailLogger);
        var productList = new ProductListScraper(apiService, listLogger);
        var hrefsScraper = new HrefsScraper();

        // URL de prueba
        string url = "https://www.mtgwolf.com/";

        try
        {
            // Obtener todas las URLs de navegación
            NavUrls navUrls = await hrefsScraper.NavScraper(url);

            // ✅ Ejecutar ScrapeProductList en paralelo para todas las URLs
            var scrapeTasks = navUrls.Urls.Select(async navUrl =>
            {
                var taskNavUrls = new NavUrls
                {
                    UrlBase = navUrls.UrlBase,
                    UrlProdcutList = navUrl,
                    HtmlDocument = null
                };

                try
                {
                    await semaphore.WaitAsync();
                    var values = await productList.ScrapeProductList(taskNavUrls);
                    return values;
                }
                catch (Exception ex)
                {
                    listLogger.LogError($"❌ Error en ScrapeProductList para {navUrl}: {ex.Message}");
                    return new List<ListProducts>();
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            var allLists = await Task.WhenAll(scrapeTasks);
            listProducts = allLists.SelectMany(x => x).ToList();

            // ✅ Ejecutar ScrapeProductDetail en paralelo
            var productTasks = listProducts.Select(async product =>
            {
                try
                {
                    bool saveProduct = await productDetail.ScrapeProductDetail(product);
                    return (product, saveProduct);
                }
                catch (Exception ex)
                {
                    detailLogger.LogError($"❌ Error en ScrapeProductDetail para {product.productUrl}: {ex.Message}");
                    return (product, false);
                }
            }).ToList();

            var productResults = await Task.WhenAll(productTasks);

        }
        catch (Exception ex)
        {
            listLogger.LogError($"❌ Error general en la ejecución: {ex.Message}");
        }
    }
}
