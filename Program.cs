using ScraperApp.Models;
using ScraperApp.Scrapers;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
class Program
{
    static async Task Main(string[] args)
    {
        List<Product> products = new List<Product>();
        List<string> failedUrls = new List<string>();
        List<string> retriedUrls = new List<string>();

        // Instancia del scraper específico
        var productDetail = new ProductDetailScraper();
        var productList = new ProductListScraper();
        var hrefsScraper = new HrefsScraper();

        // URL de prueba
        string url = "https://www.redqueen.mx/";

        Stopwatch stopwatch = new Stopwatch(); // Crea un nuevo Stopwatch
        stopwatch.Start();  // Comienza a medir el tiempo

        try
        {
            NavUrls navUrls = await hrefsScraper.NavScraper(url);  // Asumiendo que es un método asíncrono.

            foreach (var navUrl in navUrls.Urls)
            {
                navUrls.UrlProdcutList = navUrl;
                List<ListProducts> listProducts = await productList.ScrapeProductList(navUrls);

                List<Task<Product>> productTasks = new List<Task<Product>>();
                foreach (var product in listProducts)
                {
                    productTasks.Add(productDetail.ScrapeProductDetail(product));
                }

                try
                {
                    Product[] productResponses = await Task.WhenAll(productTasks);
                    foreach (var productResponse in productResponses)
                    {
                        if (productResponse != null)
                        {
                            products.Add(productResponse);
                        }
                    }
                }
                catch (Exception)
                {
                    failedUrls.Add(navUrl);
                }

                foreach (var failedUrl in failedUrls)
                {
                    try
                    {
                        var product = new ListProducts { productUrl = failedUrl };
                        Product productResponse = await productDetail.ScrapeProductDetail(product);
                        products.Add(productResponse);
                        retriedUrls.Add(failedUrl);
                    }
                    catch (Exception)
                    {
                        // Falló el reintento, mantenemos la URL en la lista de fallidos
                    }
                }
            }

            string jsonProducts = JsonSerializer.Serialize(products);
            Console.WriteLine(jsonProducts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Total products found: {products.Count}");
            Console.WriteLine($"Total failed initial attempts: {failedUrls.Count}");
            Console.WriteLine($"Total retried and succeeded: {retriedUrls.Count}");
            Console.WriteLine($"Failed after retry (still pending): {failedUrls.Count - retriedUrls.Count}");

            // Mostrar las URL que aún están pendientes
            foreach (var failedUrl in failedUrls)
            {
                if (!retriedUrls.Contains(failedUrl))
                    Console.WriteLine($"Pending URL: {failedUrl}");
            }
        }
    }
}
