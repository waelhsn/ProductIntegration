using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ProductIntegration.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductIntegration
{
    public class GetProducts
    {
        [FunctionName("GetProducts")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id?}")] HttpRequest req, // Define an HTTP-triggered Azure Function with an optional route parameter "id"
            string id, // Optional ID parameter for querying a specific product
            ILogger log) // Logger for logging information, warnings, and errors
        {
            log.LogInformation($"Processing HTTP GET request for {(id == null ? "all products" : $"product ID: {id}")}."); // Log whether we're processing all products or a specific product

            try
            {
                // Get the Cosmos DB container from the helper
                var container = CosmosDbHelper.GetContainer("ToDoList", "Items");

                if (string.IsNullOrEmpty(id)) // Check if the ID is not provided
                {
                    // Retrieve all products if no ID is provided
                    var items = await GetAllItemsAsync(container, log);
                    return new OkObjectResult(items); // Return the list of products with HTTP 200
                }
                else
                {
                    // Retrieve a specific product by ID
                    var item = await GetItemByIdAsync(container, id, log);
                    if (item != null) // Check if the product is found 
                    {
                        return new OkObjectResult(item); // Return the product with HTTP 200
                    }
                    else
                    {
                        // Return HTTP 404 if the product is not found
                        return new NotFoundObjectResult($"Product with ID '{id}' not found.");
                    }
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) // Handle Cosmos DB not found exception
            {
                log.LogError($"Cosmos DB resource not found: {ex.Message}"); // Log the error
                return new NotFoundObjectResult("Cosmos DB resource not found."); // Return HTTP 404
            }
            catch (Exception ex) // Handle other exceptions
            {
                log.LogError($"An unexpected error occurred: {ex.Message}"); // Log the error
                return new StatusCodeResult(500); // Return HTTP 500
            }
        }

        public static async Task<List<dynamic>> GetAllItemsAsync(Container container, ILogger log)
        {
            var items = new List<dynamic>(); // Initialize a list to store the retrieved items
            try
            {
                // Get the SQL query statement from environment variables or use default
                var statement = Environment.GetEnvironmentVariable("SELECT_STATEMENT") ?? "SELECT * FROM c";
                var query = container.GetItemQueryIterator<dynamic>(statement); // Create a query iterator for the statement

                while (query.HasMoreResults) // Loop while there are more results to fetch
                {
                    var response = await query.ReadNextAsync(); // Fetch the next set of results
                    items.AddRange(response); // Add the results to the list
                    log.LogInformation($"Fetched {response.Count} items."); // Log the count of fetched items
                }
            }
            catch (Exception ex) // Handle exceptions
            {
                log.LogError($"Error occurred while querying all items: {ex.Message}"); // Log the error
                throw; // Re-throw the exception to be handled by the caller
            }

            return items; // Return the list of items
        }

        public static async Task<dynamic> GetItemByIdAsync(Container container, string id, ILogger log)
        {
            try
            {
                // Correct query statement with WHERE clause
                var statement = "SELECT * FROM c WHERE c.id = @id";

                // Define a query to fetch a specific item by ID
                var queryDefinition = new QueryDefinition(statement)
                    .WithParameter("@id", id); // Set the parameter "id" with the provided ID value

                var queryIterator = container.GetItemQueryIterator<dynamic>(queryDefinition); // Create a query iterator

                if (queryIterator.HasMoreResults) // Check if there are more results to fetch
                {
                    var response = await queryIterator.ReadNextAsync(); // Fetch the next set of results
                    return response.FirstOrDefault(); // Return the first result or null if none
                }
            }
            catch (Exception ex) // Handle exceptions
            {
                log.LogError($"Error occurred while querying item by ID '{id}': {ex.Message}"); // Log the error
            }

            return null; // Return null if the item is not found or an error occurs
        }

    }
}
