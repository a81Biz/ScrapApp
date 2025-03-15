using HtmlAgilityPack;
using ScraperApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace ScraperApp.Scrapers
{
    public class ProductListScraper : ScraperBase
    {
        private readonly ApiService _apiService;
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(3); // Limita a 3 tareas simultáneas
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(300) }; // Timeout aumentado

        public ProductListScraper()
        {
            _apiService = new ApiService();
        }

        public async Task<List<ListProducts>> ScrapeProductList(NavUrls nav)
        {
            if (nav == null || string.IsNullOrEmpty(nav.UrlBase))
                throw new ArgumentException("NavUrls object is not properly initialized.");

            List<ListProducts> allProductLinks = new List<ListProducts>();
            string currentUrl = nav.UrlProdcutList ?? throw new InvalidOperationException("URL for product list is not set.");
            bool hasNextPage = true;

            while (hasNextPage)
            {
                try
                {
                    // Limitar concurrencia con SemaphoreSlim
                    await semaphore.WaitAsync();
                    nav.HtmlDocument = await GetHtmlDocument(currentUrl);
                    semaphore.Release();

                    if (nav.HtmlDocument == null)
                    {
                        Console.WriteLine("❌ Error: No se pudo cargar el HTML de la página.");
                        return allProductLinks;
                    }

                    // ✅ Ahora se espera correctamente la lista de productos
                    List<ListProducts> productsOnPage = await ProductsList(nav);
                    allProductLinks.AddRange(productsOnPage);

                    var nextPageLink = nav.HtmlDocument.DocumentNode.SelectSingleNode("//a[@aria-label='Go to next page']");
                    if (nextPageLink != null)
                    {
                        string relativeUrl = nextPageLink.GetAttributeValue("href", string.Empty);
                        currentUrl = relativeUrl.StartsWith("/") ? nav.UrlBase + relativeUrl : relativeUrl;
                    }
                    else
                    {
                        hasNextPage = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error en ScrapeProductList: {ex.Message}");
                    hasNextPage = false;
                }
            }
            return allProductLinks;
        }

        private async Task<List<ListProducts>> ProductsList(NavUrls nav)
        {
            List<ListProducts> listProducts = new List<ListProducts>();

            try
            {
                var nodes = nav.HtmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'productitem')]//a[contains(@class, 'productitem--image-link')]");
                if (nodes != null)
                {
                    ProductCategory productCategory = new ProductCategory();

                    var img = nav.HtmlDocument.DocumentNode.SelectSingleNode("//figure[contains(@class, 'collection--image')]//img");
                    var imgUrl = img != null ? img.GetAttributeValue("src", string.Empty) : null;

                    if (!string.IsNullOrEmpty(imgUrl))
                    {
                        if (imgUrl.StartsWith("//"))
                        {
                            imgUrl = "https:" + imgUrl;  // ✅ Corrige el formato de `https://`
                        }
                        else if (imgUrl.StartsWith("www"))
                        {
                            imgUrl = "https://" + imgUrl;  // ✅ Asegura que URLs con `www` tengan `https://`
                        }
                        else if (imgUrl.StartsWith("/"))
                        {
                            imgUrl = nav.UrlBase.TrimEnd('/') + imgUrl;  // ✅ Concatena correctamente `nav.UrlBase`
                        }

                        productCategory.Image.Src = imgUrl; // Asignar la URL corregida a la imagen
                    }

                    var categoryNameNode = nav.HtmlDocument.DocumentNode.SelectSingleNode("//h1[contains(@class, 'collection--title')]");
                    var categoryDescName = nav.HtmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'collection--description')]");

                    productCategory.Name = categoryNameNode != null ? categoryNameNode.InnerText.Trim() : "Unknown Category";
                    productCategory.Description = categoryDescName != null ? categoryDescName.InnerText.Trim() : " ";

                    // ✅ Asegurar ejecución secuencial
                    productCategory = await CompareCategory(productCategory);

                    foreach (var node in nodes)
                    {
                        ListProducts singleProduct = new ListProducts();
                        var relativeUrl = node.GetAttributeValue("href", string.Empty);
                        var fullUrl = relativeUrl.StartsWith("/") ? nav.UrlBase.TrimEnd('/') + "/" + relativeUrl.TrimStart('/') : relativeUrl;

                        singleProduct.Category = productCategory.Id;
                        singleProduct.productUrl = fullUrl;

                        listProducts.Add(singleProduct);
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Advertencia: No se encontraron productos en la página.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en ProductsList: {ex.Message}");
            }
            return listProducts;
        }

        private async Task<ProductCategory> CompareCategory(ProductCategory productCategory)
        {
            try
            {
                List<ProductCategory> categories = await GetCategories();
                if (categories == null || categories.Count == 0)
                {
                    Console.WriteLine("⚠️ Advertencia: No se encontraron categorías en WooCommerce.");
                    return new ProductCategory { Name = "Categoría Desconocida" };
                }

                ProductCategory? category = categories.FirstOrDefault(c => c.Slug == productCategory.Slug);
                if (category != null)
                {
                    Console.WriteLine($"✅ Categoría encontrada: {category.Name}");
                    return category;
                }
                else
                {
                    Console.WriteLine($"🆕 Creando nueva categoría: {productCategory.Name}");
                    ProductCategory newCategory = await PostCategories(productCategory);
                    return newCategory ?? new ProductCategory { Name = "Categoría No Creada" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en CompareCategory: {ex.Message}");
                return new ProductCategory { Name = "Error al Obtener Categoría" };
            }
        }

        private async Task<List<ProductCategory>> GetCategories()
        {
            try
            {
                return await _apiService.DataAsync<List<ProductCategory>>(HttpMethod.Get, "products", "categories") ?? new List<ProductCategory>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en GetCategories: {ex.Message}");
                return new List<ProductCategory>();
            }
        }

        private async Task<ProductCategory> PostCategories(ProductCategory productCategory)
        {
            try
            {
                return await _apiService.DataAsync<ProductCategory>(HttpMethod.Post, "products", "categories", productCategory) ?? new ProductCategory { Name = "Categoría No Creada" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en PostCategories: {ex.Message}");
                return new ProductCategory { Name = "Error al Crear" };
            }
        }
    }
}
