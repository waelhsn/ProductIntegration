using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using ProductIntegration.Helper;
using Microsoft.AspNetCore.Http;
using ProductIntegration.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System;


namespace ProductIntegration
{
    public static class ProductPriceUpdater
    {
        [FunctionName("ProductPriceUpdater")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Log that the function is processing an HTTP POST request.
            log.LogInformation("Processing HTTP POST request to update product prices.");

            try
            {
                // Read the body of the incoming HTTP request as a string.
                string requestBody = await req.ReadAsStringAsync();
                // Deserialize the JSON request body into a list of ProductPriceModel objects.
                var updates = JsonConvert.DeserializeObject<List<ProductPriceModel>>(requestBody);

                // Check if the request body is empty or invalid.
                if (updates == null || updates.Count == 0)
                {
                    // Return a BadRequest response if the body is invalid.
                    return new BadRequestObjectResult("Invalid request body. Provide a valid list of products to update.");
                }

                // Call the method to update product prices in Cosmos DB.
                await UpdatePricesInCosmosDbAsync(updates, log);

                // Return a success response after updating prices.
                return new OkObjectResult("Product prices updated successfully.");
            }
            catch (CosmosException ex) // Catch errors specific to Cosmos DB.
            {
                // Log the error details from Cosmos DB.
                log.LogError($"Cosmos DB error: {ex.Message}");
                // Return the error status code from Cosmos DB.
                return new StatusCodeResult((int)ex.StatusCode);
            }
            catch (Exception ex) // Catch general errors.
            {
                // Log unexpected errors that occur during execution.
                log.LogError($"Unexpected error: {ex.Message}");
                // Return a generic 500 Internal Server Error response.
                return new StatusCodeResult(500);
            }
        }

        // Private helper method to update prices in Cosmos DB.
        public static async Task UpdatePricesInCosmosDbAsync(List<ProductPriceModel> updates, ILogger log)
        {
            // Get a reference to the Cosmos DB container named "Items" in the database "ToDoList".
            var container = CosmosDbHelper.GetContainer("ToDoList", "Items");

            // Log the start of the price update process.
            log.LogInformation("Starting the price update process...");

            // Loop through each product in the list to update its price.
            foreach (var update in updates)
            {
                try
                {
                    // Log the product ID being processed.
                    log.LogInformation($"Updating price for productId: {update.ProductId}");

                    // Create a query to find the product by its ID in the Cosmos DB.
                    var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                        .WithParameter("@id", update.ProductId);

                    // Get the query results using a feed iterator.
                    var feedIterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

                    // Check if the query returned any results.
                    if (feedIterator.HasMoreResults)
                    {
                        // Read the next set of results from the iterator.
                        var response = await feedIterator.ReadNextAsync();
                        // Get the first product from the query result.
                        var product = response.FirstOrDefault();

                        // Check if the product exists in the database.
                        if (product != null)
                        {
                            // Update the product's price with the new value.
                            product["productPrice"] = update.ProductPrice;

                            // Save the updated product back to the Cosmos DB (upsert operation).
                            await container.UpsertItemAsync(product);

                            // Log that the price was successfully updated.
                            log.LogInformation($"Successfully updated price for productId: {update.ProductId}");
                        }
                        else
                        {
                            // Log a warning if the product was not found in the database.
                            log.LogWarning($"Product with productId: {update.ProductId} not found in Cosmos DB.");
                        }
                    }
                }
                catch (Exception ex) // Catch any errors during the update process.
                {
                    // Log the error details for the specific product.
                    log.LogError($"Error while updating price for productId: {update.ProductId}. Exception: {ex.Message}");
                }
            }

            // Log that the price update process has completed.
            log.LogInformation("Price update process completed.");
        }
    }
}