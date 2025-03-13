using HtmlAgilityPack;
using ScraperApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ScraperApp.Scrapers
{
    public class ProductListScraper : ScraperBase
    {
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
                    nav.HtmlDocument = await GetHtmlDocument(currentUrl);

                    if (nav.HtmlDocument == null)
                        throw new InvalidOperationException("Failed to load HTML document.");

                    allProductLinks.AddRange(ProductsList(nav));

                    var nextPageLink = nav.HtmlDocument!.DocumentNode.SelectSingleNode("//a[@aria-label='Go to next page']");
                    if (nextPageLink != null)
                    {
                        string relativeUrl = nextPageLink.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(relativeUrl))
                        {
                            currentUrl = relativeUrl.StartsWith("/") ? nav.UrlBase! + relativeUrl : relativeUrl;
                        }
                    }
                    else
                    {
                        hasNextPage = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while scraping the product list: {ex.Message}");
                    hasNextPage = false; // Stop the loop if an error occurs
                }
            }
            return allProductLinks;
        }

        private List<ListProducts> ProductsList(NavUrls nav)
        {
            List<ListProducts> listProducts = new List<ListProducts>();


            var nodes = nav.HtmlDocument!.DocumentNode.SelectNodes("//div[contains(@class, 'productitem')]//a[contains(@class, 'productitem--image-link')]");
            if (nodes != null)
            {
                var categoryNode = nav.HtmlDocument.DocumentNode.SelectSingleNode("//h1[contains(@class, 'collection--title')]");
                var category = categoryNode != null ? categoryNode.InnerText.Trim() : "Unknown Category";

                foreach (var node in nodes)
                {
                    ListProducts singleProduct = new ListProducts();
                    var relativeUrl = node.GetAttributeValue("href", string.Empty);
                    var fullUrl = relativeUrl.StartsWith("/") ? nav.UrlBase!.TrimEnd('/') + "/" + relativeUrl.TrimStart('/') : relativeUrl;

                    singleProduct.Category = category;
                    singleProduct.productUrl = fullUrl;

                    listProducts.Add(singleProduct);
                }
            }
            else
            {
                Console.WriteLine("No product nodes found.");
            }
            return listProducts;
        }
    }
}
