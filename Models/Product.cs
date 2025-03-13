using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperApp.Models
{
    public class Product
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public string? Url { get; set; }
        public string? Vendor { get; set; }
        public List<string> Images { get; set; } = new List<string>(); // Evita null en listas
    }
}
