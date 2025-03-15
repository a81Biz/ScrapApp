using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ScraperApp.Models
{
    public class Product
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("regular_price")]
        public string RegularPrice { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("categories")]
        public List<Category> Categories { get; set; }

        [JsonProperty("images")]
        public List<ProductImage> Images { get; set; }

        [JsonProperty("stock_status")]
        public string StockStatus { get; set; }

        [JsonProperty("meta_data")]
        public List<ProductMetaData> MetaData { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class Category
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class ProductImage
    {
        [JsonProperty("src")]
        public string Src { get; set; }
    }

    public class ProductMetaData
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
