using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScraperApp.Models
{
    public class ErrorCategoryResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public ErrorData Data { get; set; }
    }

    public class ErrorData
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("resource_id")]
        public int ResourceId { get; set; }
    }
}
