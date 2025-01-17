using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

public static class ServiceBusHelper
{
    public static string ServiceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
    public static string ServiceBusQueueName = Environment.GetEnvironmentVariable("ServiceBusQueueName");

    public static async Task SendEventToServiceBusAsync(dynamic product, ILogger log)
    {
        try
        {
            if (string.IsNullOrEmpty(ServiceBusConnectionString) || string.IsNullOrEmpty(ServiceBusQueueName))
            {
                log.LogError("ServiceBus connection string or queue name is not configured.");
                return;
            }

            var client = new QueueClient(ServiceBusConnectionString, ServiceBusQueueName);
            var eventMessage = new
            {
                EventType = "ProductUpdated",
                ProductId = product["id"],
                ProductName = product["productName"],
                // Include the "weight" if it's part of the product and has been updated
                Weight = product["weight"],
                ProductDetails = product
            };

            var messageBody = JsonConvert.SerializeObject(eventMessage);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            // Send the message to the ServiceBus queue
            await client.SendAsync(message);

            log.LogInformation($"Event sent to ServiceBus for product: {product["productId"]}");
        }
        catch (Exception ex)
        {
            log.LogError($"Error sending event to ServiceBus: {ex.Message}");
        }
    }
}
