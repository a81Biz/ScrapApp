using HtmlAgilityPack;
using ScraperApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

namespace ScraperApp.Scrapers
{
    public class ProductDetailScraper : ScraperBase
    {
        private readonly ApiService _apiService;
        private readonly ILogger<ProductDetailScraper> _logger;

        public ProductDetailScraper(ApiService apiService, ILogger<ProductDetailScraper> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ScrapeProductDetail(ListProducts listProducts)
        {
            if (string.IsNullOrWhiteSpace(listProducts.productUrl))
            {
                throw new ArgumentException("Product URL is null or whitespace.", nameof(listProducts.productUrl));
            }

            _logger.LogInformation($"🔍 Scraping product detail from: {listProducts.productUrl}");

            // Cargar HTML del producto
            HtmlDocument? document = await GetHtmlDocument(listProducts.productUrl!);
            if (document == null)
            {
                _logger.LogError($"❌ Failed to load HTML document for URL: {listProducts.productUrl}");
                return false;
            }

            // Detectar estructura
            Structure structure = ValidateStructure(document);
            if (structure == null)
            {
                _logger.LogError($"❌ Structure not recognized for URL: {listProducts.productUrl}");
                return false;
            }

            // Extraer detalles del producto
            Product product = ExtractProductDetails(document, listProducts, structure);

            // Verificar si el producto ya existe en WooCommerce
            List<Product> existingProducts = await SearchProductByName(product.Name);
            if (existingProducts.Count > 0)
            {
                _logger.LogInformation($"✅ Product '{product.Name}' already exists. Skipping...");
                return false;
            }

            return await PostProduct(product);
        }

        private Structure ValidateStructure(HtmlDocument document)
        {
            foreach (var structure in StructureConfigurations.Structures)
            {
                var productNameNode = document.DocumentNode.SelectSingleNode(structure.ProductNameSelector);
                if (productNameNode != null)
                {
                    _logger.LogInformation($"✅ Structure detected: {structure.Name}");
                    return structure;
                }
            }

            return null;
        }

        private Product ExtractProductDetails(HtmlDocument document, ListProducts listProducts, Structure structure)
        {
            var product = new Product
            {
                Categories = new List<Category> { new Category { Id = listProducts.Category! } },
                MetaData = new List<ProductMetaData>
                {
                    new ProductMetaData { Key = "original_product_url", Value = listProducts.productUrl },
                    new ProductMetaData { Key = "vendor_url", Value = listProducts.baseUrl! }
                }
            };

            // Extraer nombre del producto
            var productNameNode = document.DocumentNode.SelectSingleNode(structure.ProductNameSelector);
            product.Name = productNameNode != null ? productNameNode.InnerText.Trim() : "Unnamed Product";

            // Extraer precio
            var priceNode = document.DocumentNode.SelectSingleNode(structure.ProductPriceSelector);
            if (priceNode != null && decimal.TryParse(priceNode.InnerText.Replace("$", "").Replace(",", ""),
                                                      NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                product.RegularPrice = price.ToString();
            }

            // Extraer descripción
            var descriptionNode = document.DocumentNode.SelectSingleNode(structure.ProductDescriptionSelector);
            product.Description = descriptionNode != null ? descriptionNode.InnerText.Trim() : string.Empty;

            // Extraer imágenes
            var imageNodes = document.DocumentNode.SelectNodes(structure.ProductImageSelector);
            product.Images = imageNodes != null
                ? imageNodes.Select(img =>
                {
                    var imgUrl = img.GetAttributeValue("src", string.Empty);
                    return new ProductImage { Src = FormatImageUrl(imgUrl, listProducts.baseUrl) };
                }).ToList()
                : new List<ProductImage>();

            return product;
        }

        private async Task<List<Product>> SearchProductByName(string productName)
        {
            string encodedProductName = HttpUtility.UrlEncode(productName);
            return await _apiService.DataAsync<List<Product>>(HttpMethod.Get, "products", $"?search={encodedProductName}");
        }

        private async Task<bool> PostProduct(Product product)
        {
            try
            {
                Product createdProduct = await _apiService.DataAsync<Product>(HttpMethod.Post, "products", "", product);
                return createdProduct != null && createdProduct.Id > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error while posting product: {ex.Message}");
                return false;
            }
        }

        private string FormatImageUrl(string imgUrl, string baseUrl)
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
    }
}
