using Microsoft.Azure.Cosmos;  
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs; 
using Microsoft.Extensions.Logging;
using Newtonsoft.Json; 
using ProductIntegration.Helper; 
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProductIntegration 
{
    public class ProductStatusUpdater
    {
        [FunctionName("ProductStatusUpdater")]
        public static async Task Run([TimerTrigger("0 */5 * * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            log.LogInformation("Checking for updated or new products...");

            try
            {
                // Get the Cosmos DB container
                var container = CosmosDbHelper.GetContainer("ToDoList", "Items");

                // Query for products with Status = "Updated" or "New"
                var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.Status IN ('Updated', 'New')")
                    .WithParameter("@statusUpdated", "Updated") // Define query parameter for "Updated" status
                    .WithParameter("@statusNew", "New"); // Define query parameter for "New" status

                // Create query iterator to fetch data
                var feedIterator = container.GetItemQueryIterator<dynamic>(queryDefinition); 
                var productsToSend = new List<dynamic>(); // List to store products to send

                while (feedIterator.HasMoreResults) // Loop while there are more results to fetch
                {
                    var response = await feedIterator.ReadNextAsync(); // Fetch the next set of results
                    productsToSend.AddRange(response); // Add the results to the list
                }

                if (productsToSend.Count != 0) // Check if there are products to send
                {
                    log.LogInformation($"Found {productsToSend.Count} products to send."); // Log the count of products to send

                    // Send each product to Service Bus
                    foreach (var product in productsToSend) // Loop through each product
                    {
                        var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(product))) // Create a Service Bus message
                        {
                            ContentType = "application/json", // Set the message content type to JSON
                            Label = "ProductUpdate", // Add a label to the message
                            MessageId = Guid.NewGuid().ToString() // Generate a unique message ID
                        };

                        // Send message to Service Bus
                        await SendMessageToServiceBusAsync(message);

                        // Update the status in Cosmos DB to "Sent"
                        product["Status"] = "Sent"; // Set product status to "Sent"
                        await container.UpsertItemAsync(product); // Update the product in Cosmos DB

                        log.LogInformation($"Sent product with ID: {product["productId"]} to Service Bus and updated status to 'Sent'."); // Log the product ID and status update
                    }
                }
                else
                {
                    log.LogInformation("No updated or new products found."); // Log if no products are found
                }
            }
            catch (Exception ex) // Handle exceptions
            {
                log.LogError($"Error occurred while checking for products: {ex.Message}"); // Log the error
            }
        }

        public static async Task SendMessageToServiceBusAsync(Message message)
        {
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString"); // Get the Service Bus connection string from environment variables
            var serviceBusQueueName = Environment.GetEnvironmentVariable("ServiceBusQueueName"); // Get the Service Bus queue name from environment variables
            try
            {
                // Create a Service Bus client
                var client = new QueueClient(serviceBusConnectionString, serviceBusQueueName);

                // Send the message to Service Bus
                await client.SendAsync(message);

                // Close the connection to the Service Bus client
                await client.CloseAsync();
            }
            catch (Exception ex) // Handle exceptions
            {
                throw new Exception($"Error sending message to Service Bus: {ex.Message}", ex); // Rethrow exception with additional context
            }
        }
    }
}
