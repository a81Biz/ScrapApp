using HtmlAgilityPack;
using ScraperApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperApp.Scrapers
{
    public class HrefsScraper : ScraperBase
    {
        public async Task<NavUrls> NavScraper(string url)
        {
            NavUrls nav = new NavUrls();
            nav.UrlBase = url;

            HtmlDocument? document = await GetHtmlDocument(url);

            if (document == null)
            {
                throw new InvalidOperationException("Failed to load the HTML document.");
            }

            var navNode = document.DocumentNode.SelectSingleNode("//nav");
            if (navNode != null)
            {
                var lis = navNode.Descendants("li");
                foreach (var li in lis)
                {

                    var link = li.Descendants("a").FirstOrDefault();
                    if (link != null)
                    {
                        string href = link.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(href) && href != url && href.Contains("/"))
                        {
                            string fullUrl = href.StartsWith("/") ? url.TrimEnd('/') + href : href;
                            if (!nav.Urls.Contains(fullUrl))
                            {
                                nav.Urls.Add(fullUrl);
                            }
                        }
                    }
                }
            }
            return nav;
        }
    }
}
