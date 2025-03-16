using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using OpenQA.Selenium;
using ScraperApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace ScraperApp.Scrapers
{
    public class ProductListScraper : ScraperBase
    {
        private readonly ApiService _apiService;
        private readonly ILogger<ProductListScraper> _logger;
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(10); // Limita a 3 tareas simultáneas

        public ProductListScraper(ApiService apiService, ILogger<ProductListScraper> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ListProducts>> ScrapeProductList(NavUrls nav)
        {
            if (nav == null || string.IsNullOrEmpty(nav.UrlBase))
            {
                _logger.LogError("NavUrls object is not properly initialized.");
                throw new ArgumentException("NavUrls object is not properly initialized.");
            }

            if (nav.UrlBase.TrimEnd('/') == nav.UrlProdcutList.TrimEnd('/'))
            {
                _logger.LogError("Index.");
                throw new ArgumentException("skip index.");
            }

            List<ListProducts> allProductLinks = new List<ListProducts>();
            HashSet<string> visitedUrls = new HashSet<string>(); // 🔹 Nuevo: Rastrea las URLs visitadas
            string currentUrl = nav.UrlProdcutList ?? throw new InvalidOperationException("URL for product list is not set.");

            await semaphore.WaitAsync();
            _logger.LogInformation($"Fetching initial HTML document from URL: {currentUrl}");
            nav.HtmlDocument = await GetHtmlDocument(currentUrl);
            semaphore.Release();

            if (nav.HtmlDocument == null)
            {
                _logger.LogError("Failed to load initial HTML document from the page.");
                return allProductLinks;
            }

            Structure structure = ValidateStructure(nav);
            if (structure == null)
            {
                _logger.LogError("The page structure is not recognized.");
                return allProductLinks;
            }

            bool hasNextPage = true;

            while (hasNextPage)
            {
                try
                {
                    // 🔹 Si la URL ya fue visitada, no repetirla
                    if (visitedUrls.Contains(currentUrl))
                    {
                        _logger.LogWarning($"Skipping already visited URL: {currentUrl}");
                        hasNextPage = false;
                        break;
                    }

                    visitedUrls.Add(currentUrl); // 🔹 Marcar la URL como visitada

                    if (currentUrl != nav.UrlProdcutList)
                    {
                        await semaphore.WaitAsync();
                        _logger.LogInformation($"Fetching HTML document from URL: {currentUrl}");
                        nav.HtmlDocument = await GetHtmlDocument(currentUrl);
                        nav.UrlProdcutList = currentUrl;
                        semaphore.Release();
                    }

                    // 🔹 Extraer productos según la estructura detectada
                    List<ListProducts> productsOnPage = await ProductsList(nav, structure);
                    allProductLinks.AddRange(productsOnPage);

                    // 🔹 Manejar paginación sin loops infinitos
                    var nextPageLink = nav.HtmlDocument.DocumentNode.SelectSingleNode(structure.NextPageSelector);
                    if (nextPageLink != null)
                    {
                        string relativeUrl = nextPageLink.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(relativeUrl) && !visitedUrls.Contains(relativeUrl))
                        {
                            currentUrl = relativeUrl.StartsWith("/") ? nav.UrlBase + relativeUrl : relativeUrl;
                        }
                        else
                        {
                            hasNextPage = false;
                        }
                    }
                    else
                    {
                        hasNextPage = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in ScrapeProductList.");
                    hasNextPage = false;
                }
            }

            return allProductLinks;
        }

        private Structure ValidateStructure(NavUrls nav)
        {
            foreach (var structure in StructureConfigurations.Structures)
            {
                var nextPageLink = nav.HtmlDocument.DocumentNode.SelectSingleNode(structure.NextPageSelector);
                if (nextPageLink != null)
                {
                    _logger.LogInformation($"✅ Estructura detectada: {structure.Name}");
                    return structure;
                }
            }

            _logger.LogError($"❌ The page structure is not recognized. {nav.UrlProdcutList}");
            return null;
        }

        private async Task<List<ListProducts>> ProductsList(NavUrls nav, Structure structure )
        {
            List<ListProducts> listProducts = new List<ListProducts>();

            try
            {
                var nodes = nav.HtmlDocument.DocumentNode.SelectNodes(structure.ProductContainerSelector);

                if (nodes == null)
                {
                    _logger.LogWarning($"No products found on the page. {nav.UrlProdcutList.ToString()}");
                    return listProducts;
                }

                string name = nav.HtmlDocument.DocumentNode.SelectSingleNode(structure.CategoryName).InnerText.Trim();

                _logger.LogInformation($"Fetching HTML document ProductsList: {nav.UrlProdcutList}");
                ProductCategory productCategory = await PostCategories(new ProductCategory() { Name = name });

                foreach (var node in nodes)
                {
                    var relativeUrl = node.GetAttributeValue(structure.ProductUrlSelector, string.Empty);
                    if (string.IsNullOrEmpty(relativeUrl))
                    {
                        _logger.LogWarning("Empty href attribute found in product link.");
                        continue;
                    }


                    ListProducts newProduct = new ListProducts()
                    {
                        baseUrl = nav.UrlBase,
                        productUrl = FormatUrl(relativeUrl, nav.UrlBase),
                        Category = productCategory.Id
                    };

                    if (listProducts.Contains(newProduct)) return listProducts;

                    // Agregar el producto a la lista
                    listProducts.Add(newProduct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ProductsList.");
            }

            return listProducts;
        }

        private string FormatUrl(string imgUrl, string baseUrl)
        {
            if (imgUrl.StartsWith("//"))
            {
                return "https:" + imgUrl;
            }
            else if (imgUrl.StartsWith("www"))
            {
                return "https://" + imgUrl;
            }
            else if (imgUrl.StartsWith("/"))
            {
                return baseUrl.TrimEnd('/') + imgUrl;
            }
            return imgUrl;
        }

        private async Task<ProductCategory> PostCategories(ProductCategory productCategory)
        {
            // Intentar crear la categoría en WooCommerce
            var response = await _apiService.DataAsyncResponse<string>(HttpMethod.Post, "products", "categories", productCategory);

            ProductCategory category = new ProductCategory();

            if (response == null) return category;

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                try
                {
                    // 🔹 Intentamos extraer `resource_id` si la categoría ya existe
                    var errorResponse = JsonSerializer.Deserialize<ErrorCategoryResponse>(response.ErrorMessage);

                    if (errorResponse?.Code == "term_exists" && errorResponse.Data != null)
                    {
                        category.Id = errorResponse.Data.ResourceId; // Usar el ID existente
                        _logger.LogInformation($"✅ Categoría ya existente, usando ID {category.Id}");
                    }
                    else
                    {
                        _logger.LogError($"❌ Error al crear categoría: {response.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error al procesar la respuesta de categoría existente: {ex.Message}");
                }
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                // 🔹 Si se creó correctamente, deserializamos la respuesta
                category = JsonSerializer.Deserialize<ProductCategory>(response.Data);
                _logger.LogInformation($"✅ Nueva categoría creada con ID {category.Id}");
            }

            return category;
        }


    }
}