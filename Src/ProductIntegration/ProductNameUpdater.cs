using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using ProductIntegration.Helper;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;

namespace ProductIntegration
{
    public class ProductNameUpdater
    {
        [FunctionName("ProductNameUpdater")]
        public static async Task Run([TimerTrigger("0 */15 * * * *", RunOnStartup = true)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Starting product update process...");

            try
            {
                // Fetch data from System A using HttpClientHelper
                var products = await HttpClientHelper.GetDataFromSystemA();
                var container = CosmosDbHelper.GetContainer("ToDoList", "Items");

                // A HashSet to track productIds that have already been processed
                var processedProductIds = new HashSet<string>();

                foreach (JObject product in products)
                {
                    string productId = product["productId"].ToString();

                    // If the productId has already been processed, skip this iteration
                    if (processedProductIds.Contains(productId))
                    {
                        log.LogInformation($"Skipping duplicate productId: {productId}");
                        continue;
                    }

                    // Add the productId to the HashSet to mark it as processed
                    processedProductIds.Add(productId);

                    var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                        .WithParameter("@id", product["productId"]);

                    var feedIterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

                    if (feedIterator.HasMoreResults)
                    {
                        var response = await feedIterator.ReadNextAsync();
                        var existingProduct = response.FirstOrDefault();

                        if (existingProduct != null)
                        {
                            bool isUpdated = false;

                            // Loop through all properties of the incoming product JSON
                            foreach (JProperty property in product.Properties())
                            {
                                // Check if the property exists in the existing product and if it differs
                                if (existingProduct[property.Name] == null || existingProduct[property.Name]?.ToString() != property.Value?.ToString())
                                {
                                    existingProduct[property.Name] = property.Value;
                                    isUpdated = true;
                                }
                            }

                            // If any properties were updated, mark the status as "Updated"
                            if (isUpdated)
                            {
                                existingProduct["Status"] = "Updated";
                                // Upsert the updated product back to Cosmos DB
                                await container.UpsertItemAsync(existingProduct);

                                log.LogInformation($"Updated product with ID: {product["productId"]}");
                            }
                            else
                            {
                                log.LogInformation($"No changes detected for product with ID: {product["productId"]}");
                            }
                        }
                        else
                        {
                            // If no matching product is found, insert the new product with 'productId' as 'id'
                            var newProduct = new JObject(product);

                            // Set 'id' to be the same as the 'productId'
                            newProduct["id"] = product["productId"];
                            newProduct["Status"] = "New";  // Mark it as a new product

                            // Insert the new product into Cosmos DB
                            await container.CreateItemAsync(newProduct);

                            log.LogInformation($"No matching product found. Added new product with ID: {product["productId"]}");
                        }
                    }
                }

                log.LogInformation("Product update process completed successfully.");
            }
            catch (Exception ex)
            {
                log.LogError($"Error updating product data: {ex.Message}");
            }
        }
    }
}
