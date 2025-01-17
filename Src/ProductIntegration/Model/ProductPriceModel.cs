using Newtonsoft.Json;

namespace ProductIntegration.Model
{
    public class ProductPriceModel
    {
        [JsonProperty("productId")]
        public string ProductId { get; set; }

        [JsonProperty("productPrice")]
        public decimal ProductPrice { get; set; }
    }
}
