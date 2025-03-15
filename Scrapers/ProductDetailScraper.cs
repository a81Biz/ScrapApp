using HtmlAgilityPack;
using ScraperApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ScraperApp.Scrapers
{
    public class ProductDetailScraper : ScraperBase
    {

        private readonly ApiService _apiService;
        public ProductDetailScraper()
        {
            _apiService = new ApiService();
        }
        public async Task<bool> ScrapeProductDetail(ListProducts listProducts)
        {
            if (string.IsNullOrWhiteSpace(listProducts.productUrl))
            {
                throw new ArgumentException("Product URL is null or whitespace.", nameof(listProducts.productUrl));
            }

            var product = new Product();

            HtmlDocument? document = await GetHtmlDocument(listProducts.productUrl!);

            if (document == null)
            {
                Console.WriteLine($"Failed to load HTML document for URL: {listProducts.productUrl}");
                return false;
            }

            // Extracción del nombre del producto
            string? productName = document.DocumentNode
                           .SelectSingleNode("//h1[contains(@class, 'product-title')]")
                           ?.InnerText.Trim();

            if (string.IsNullOrEmpty(productName))
            {
                Console.WriteLine("Product name not found.");
                return false;
            }

            List<Product> existingProducts = await SearchProductByName(productName);

            if (existingProducts.Count > 0)
            {
                Console.WriteLine($"Product '{productName}' already exists in WooCommerce. Skipping...");
                return false;
            }

            product.Name = productName;
            product.Categories = new List<Category> { new Category { Id = listProducts.Category! } };

            var priceNode = document.DocumentNode
                            .SelectSingleNode("//div[@data-product-pricing]//span[@data-price]")
                            ?.InnerText.Trim();

            if (decimal.TryParse(priceNode?.Replace("$", "").Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                product.RegularPrice = price.ToString();
            }

            var description = document.DocumentNode.SelectSingleNode("//div[@data-product-description]");
            product.Description = description != null ? description.InnerText.Trim() : string.Empty;

            var vendor = document.DocumentNode.SelectSingleNode("//div[@class='product-vendor']//a");

            product.MetaData = new List<ProductMetaData>
            {
            new ProductMetaData { Key = "original_product_url", Value = listProducts.productUrl },
            new ProductMetaData { Key = "vendor_url", Value = listProducts.baseUrl! }
            };

            var imageNodes = document.DocumentNode.SelectNodes("//div[contains(@class, 'product-gallery--image-background')]//img");
            product.Images = imageNodes != null ? imageNodes.Select(img =>
            {
                var imgUrl = img.GetAttributeValue("src", string.Empty);
                return new ProductImage {
                    Src = imgUrl.StartsWith("//") ? "https:" + imgUrl
                    : imgUrl.StartsWith("/") ? listProducts.baseUrl + imgUrl
                    : imgUrl
             };}).ToList() : new List<ProductImage>();

            return await PostProduct(product);
        }

        private async Task<List<Product>> SearchProductByName(string productName)
        {
            string encodedProductName = HttpUtility.HtmlEncode(productName);
            encodedProductName = Uri.EscapeDataString(encodedProductName);

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
                return false;
            }
        }
    }
}