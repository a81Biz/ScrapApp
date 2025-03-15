using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperApp.Models
{
    public class ListProducts
    {
        public string? baseUrl { get; set; }
        public string? productUrl { get; set; }
        public int Category { get; set; }
        public HtmlDocument? htmlDocument { get; set; }
    }
}
