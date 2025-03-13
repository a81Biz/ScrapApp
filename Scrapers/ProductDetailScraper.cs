using HtmlAgilityPack;
using ScraperApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperApp.Scrapers
{
    public class ProductDetailScraper : ScraperBase
    {
        public async Task<Product> ScrapeProductDetail(ListProducts listProducts)
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
                return null; // Consider handling this scenario more gracefully
            }

            // Extracción del nombre del producto
            string? productName = document.DocumentNode
                           .SelectSingleNode("//h1[contains(@class, 'product-title')]")
                           ?.InnerText.Trim();

            if (string.IsNullOrEmpty(productName))
            {
                Console.WriteLine("Product name not found.");
                return null; // Finaliza la ejecución y devuelve null si no se encuentra el nombre del producto
            }

            product.Name = productName;
            product.Url = listProducts.productUrl;
            product.Category = listProducts.Category!;

            var priceNode = document.DocumentNode
                            .SelectSingleNode("//div[@data-product-pricing]//span[@data-price]")
                            ?.InnerText.Trim();

            if (decimal.TryParse(priceNode?.Replace("$", "").Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                product.Price = price;
            }

            var description = document.DocumentNode.SelectSingleNode("//div[@data-product-description]");
            product.Description = description != null ? description.InnerText.Trim() : string.Empty;

            var vendor = document.DocumentNode.SelectSingleNode("//div[@class='product-vendor']//a");

            product.Vendor = vendor != null ? vendor.InnerText.Trim() : string.Empty;

            var imageNodes = document.DocumentNode.SelectNodes("//div[contains(@class, 'product-gallery--image-background')]//img");
            if (imageNodes != null)
            {
                product.Images = imageNodes.Select(img => {
                    var imgUrl = img.GetAttributeValue("src", string.Empty);
                    return imgUrl.StartsWith("//") ? "https:" + imgUrl : imgUrl.StartsWith("/") ? listProducts.baseUrl + imgUrl : imgUrl;
                }).ToList();
            }

            return product;
        }
    }
}