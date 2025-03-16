using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperApp.Models
{
    public static class StructureConfigurations
    {
        public static readonly List<Structure> Structures = new List<Structure>
        {
            // 🔹 Estructura Antigua (Listados de Productos)
            new Structure
            {
                Name = "OldStructure",
                NextPageSelector = "//a[@aria-label='Go to next page']",
                ProductContainerSelector = "//div[contains(@class, 'productitem')]//a[contains(@class, 'productitem--image-link')]",
                ProductUrlSelector = "href",
                CategoryName = "//h2//span",
                ImageSelector = ".//img",

                // 📌 Nuevas Propiedades para Detalles del Producto
                ProductNameSelector = "//h1[contains(@class, 'product-title')]",
                ProductPriceSelector = "//div[@data-product-pricing]//span[@data-price']",
                ProductDescriptionSelector = "//div[@data-product-description']",
                ProductImageSelector = "//div[contains(@class, 'product-gallery--image-background')]//img"
            },

            // 🔹 Estructura Nueva (Listados de Productos)
            new Structure
            {
                Name = "NewStructure",
                NextPageSelector = "//a[contains(@aria-label, 'Next Page')]",
                ProductContainerSelector = "//ul[contains(@data-hook, 'product-list-wrapper')]//a",
                ProductUrlSelector = "href",
                CategoryName = "//main//h1",
                ImageSelector = ".//img",

                // 📌 Nuevas Propiedades para Detalles del Producto
                ProductNameSelector = "//h1[contains(@data-hook, 'product-title')]",
                ProductPriceSelector = "//div[contains(@data-hook, 'product-price')]//span[contains(@data-hook, 'formatted-primary-price')]",
                ProductDescriptionSelector = "//pre[contains(@data-hook, 'description')]",
                ProductImageSelector = "//div[contains(@data-hook, 'ProductImageDataHook.ProductImage')]//img"
            }
        };
    }
}
