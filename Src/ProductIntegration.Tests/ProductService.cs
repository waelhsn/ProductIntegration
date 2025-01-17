
// The ProductService to test
public class ProductService
{
    private readonly HttpClient _httpClient;

    public ProductService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetProductsAsync()
    {
        var get_API_KEY = Environment.GetEnvironmentVariable("GetAPIKey");
        var response = await _httpClient.GetAsync($"https://case-study-productintegration.azurewebsites.net/api/products?code={get_API_KEY}");

        response.EnsureSuccessStatusCode(); // Throw an exception if not successful

        return await response.Content.ReadAsStringAsync(); // Return the response content
    }
}
