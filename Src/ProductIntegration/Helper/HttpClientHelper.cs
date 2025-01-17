using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProductIntegration
{
    public static class HttpClientHelper
    {
        private static HttpClient HttpClient = new();

        public static async Task<JArray> GetDataFromSystemA()
        {
            string systemAEndpoint = Environment.GetEnvironmentVariable("Get_All_Products");
            if (string.IsNullOrEmpty(systemAEndpoint))
            {
                throw new InvalidOperationException("Environment variable 'Get_All_Products' is not set.");
            }

            var response = await HttpClient.GetStringAsync(systemAEndpoint);
            return JArray.Parse(response);
        }

        public static async Task<JArray> GetDataFromSystemB()
        {
            string systemBEndpoint = Environment.GetEnvironmentVariable("Get_All_Prices");
            if (string.IsNullOrEmpty(systemBEndpoint))
            {
                throw new InvalidOperationException("Environment variable 'Get_All_Prices' is not set.");
            }

            var response = await HttpClient.GetStringAsync(systemBEndpoint);
            return JArray.Parse(response);
        }
    }
}