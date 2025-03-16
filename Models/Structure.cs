using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperApp.Models
{
    public class Structure
    {
        public string Name { get; set; }
        public string NextPageSelector { get; set; } // Selector para el botón "Siguiente página"
        public string ProductContainerSelector { get; set; } // Selector para los contenedores de productos
        public string ProductUrlSelector { get; set; } // Selector para la URL del producto
        public string ImageSelector { get; set; } // Selector para la imagen del producto
        public string CategoryName { get; set; }
        public string ProductNameSelector { get; set; }
        public string ProductPriceSelector { get; set; }
        public string ProductDescriptionSelector { get; set; }
        public string ProductImageSelector { get; set; }
    }
}
