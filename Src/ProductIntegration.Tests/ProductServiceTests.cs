using System.Net;

namespace ProductIntegration.Tests
{
    public class ProductServiceTests
    {
        [Fact]
        public async Task GetProductsAsync_ValidRequest_ReturnsProducts()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{ \"id\": \"1\", \"name\": \"Product1\" }]") // Mock JSON response
            });

            var httpClient = new HttpClient(mockHttpMessageHandler);
            var productService = new ProductService(httpClient);

            // Act
            var result = await productService.GetProductsAsync();

            // Assert
            Assert.Equal("[{ \"id\": \"1\", \"name\": \"Product1\" }]", result);
        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _mockResponse;

        public MockHttpMessageHandler(HttpResponseMessage mockResponse)
        {
            _mockResponse = mockResponse;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_mockResponse);
        }
    }

    public class ProductService
    {
        private readonly HttpClient _httpClient;

        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetProductsAsync()
        {
            var response = await _httpClient.GetAsync("https://example.com/api/products");

            response.EnsureSuccessStatusCode(); // Throw an exception if not successful

            return await response.Content.ReadAsStringAsync(); // Return the response content
        }
    }
}
