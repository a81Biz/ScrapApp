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
        public string baseUrl { get; set; }
        public string productUrl { get; set; }
        public int Category { get; set; }

        // 🔹 Sobrescribimos Equals() para comparar por `productUrl`
        public override bool Equals(object obj)
        {
            return obj is ListProducts product && productUrl == product.productUrl;
        }

        // 🔹 Sobrescribimos GetHashCode() para asegurar la comparación en listas y HashSet
        public override int GetHashCode()
        {
            return productUrl.GetHashCode();
        }
    }
}
