using ScraperApp.Models;
using ScraperApp.Scrapers;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
class Program
{
    static async Task Main(string[] args)
    {
        List<ListProducts> listProducts = new List<ListProducts>();
        List<ListProducts> failedUrls = new List<ListProducts>();
        List<string> retriedUrls = new List<string>();
        int countProducts = 0;

        // Instancia del scraper específico
        var productDetail = new ProductDetailScraper();
        var productList = new ProductListScraper();
        var hrefsScraper = new HrefsScraper();

        // URL de prueba
        string url = "https://www.redqueen.mx/";
        Stopwatch stopwatch = new Stopwatch();

        try
        {
            // Obtener todas las URLs de navegación
            NavUrls navUrls = await hrefsScraper.NavScraper(url);

            // Ejecutar ScrapeProductList en paralelo para todas las URLs
            var scrapeTasks = navUrls.Urls.Select(async navUrl =>
            {
                navUrls.UrlProdcutList = navUrl;
                return await productList.ScrapeProductList(navUrls);
            }).ToList();

            var allLists = await Task.WhenAll(scrapeTasks); // Esperar todas las tareas
            listProducts = allLists.SelectMany(x => x).ToList(); // Combinar todas las listas

            // ✅ Contar productos encontrados
            countProducts += listProducts.Count;

            // ✅ Ejecutar ScrapeProductDetail en paralelo
            var productTasks = listProducts.Select(async product =>
            {
                bool saveProduct = await productDetail.ScrapeProductDetail(product);
                return (product, saveProduct);
            }).ToList();

            var productResults = await Task.WhenAll(productTasks);

            // ✅ Agregar productos fallidos
            failedUrls.AddRange(productResults.Where(p => !p.saveProduct).Select(p => p.product));


            if (failedUrls.Count > 0)
            {
                Console.WriteLine($"🔄 Reintentando {failedUrls.Count} productos que fallaron...");

                foreach (var failedProduct in failedUrls)
                {
                    bool retrySave = await productDetail.ScrapeProductDetail(failedProduct);

                    if (!retrySave)
                    {
                        Console.WriteLine($"❌ Falló nuevamente el scraping de: {failedProduct.productUrl}");
                    }
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            TimeSpan elapsedTime = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();

            Console.WriteLine($"Total execution time: {elapsedTime.Minutes} min {elapsedTime.Seconds} sec");
            Console.WriteLine($"Total products found: {countProducts}");
            Console.WriteLine($"Total failed initial attempts: {failedUrls.Count}");
            Console.WriteLine($"Total retried and succeeded: {retriedUrls.Count}");
            Console.WriteLine($"Failed after retry (still pending): {failedUrls.Count - retriedUrls.Count}");
        }
    }
}
