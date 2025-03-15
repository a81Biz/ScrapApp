using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScraperApp.Models
{
    public class ProductCategory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("parent")]
        public int Parent { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }

        [JsonProperty("image")]
        public ImageData Image { get; set; } = new ImageData();

        [JsonProperty("menu_order")]
        public int MenuOrder { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }

    public class ImageData
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonProperty("date_created_gmt")]
        public DateTime DateCreatedGmt { get; set; }

        [JsonProperty("date_modified")]
        public DateTime DateModified { get; set; }

        [JsonProperty("date_modified_gmt")]
        public DateTime DateModifiedGmt { get; set; }

        [JsonProperty("src")]
        public string? Src { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("alt")]
        public string Alt { get; set; }
    }

    public class Links
    {
        [JsonProperty("self")]
        public List<LinkItem> Self { get; set; }

        [JsonProperty("collection")]
        public List<LinkItem> Collection { get; set; }
    }

    public class LinkItem
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("targetHints")]
        public TargetHints TargetHints { get; set; }
    }

    public class TargetHints
    {
        [JsonProperty("allow")]
        public List<string> Allow { get; set; }
    }
}
