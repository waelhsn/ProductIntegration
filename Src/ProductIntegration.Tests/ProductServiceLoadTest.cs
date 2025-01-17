using System.Net;

namespace ProductIntegrationTest
{
    public class ProductServiceLoadTest
    {
        [Fact]
        public async Task ProductService_ShouldHandle10RequestsPerSecondUnder800ms()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                //Content = new StringContent("[{ \"id\": \"1\", \"productName\": \"Product1\" }]") // Mock JSON response
            });

            var httpClient = new HttpClient(mockHttpMessageHandler);
            var productService = new ProductService(httpClient);

            int totalRequests = 10; // 10 requests per second
            var tasks = new List<Task>();
            var responseTimes = new List<double>();

            // Act
            for (int i = 0; i < totalRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var startTime = DateTime.UtcNow;
                    await productService.GetProductsAsync();
                    var elapsedTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    responseTimes.Add(elapsedTime);
                }));
                Thread.Sleep(100); // 100ms delay between requests (10 req/sec)
            }

            await Task.WhenAll(tasks); // Wait for all requests to complete

            // Assert
            Assert.All(responseTimes, time => Assert.True(time < 800, $"Response time exceeded 800ms: {time}ms"));
        }
    }

}