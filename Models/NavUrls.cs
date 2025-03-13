using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ScraperApp.Models
{
    public class NavUrls
    {
        public HtmlDocument? HtmlDocument { get; set; }
        public List<string> Urls { get; internal set; }
        public string? UrlBase { get; set; }
        public string? UrlProdcutList { get; set; }

        public NavUrls()
        {
            Urls = new List<string>();
        }
    }
}
