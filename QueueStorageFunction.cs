using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace QueueStorageFunction
{
    public class QueueStorageFunction
    {
        private readonly ILogger<QueueStorageFunction> _logger; // Add a logger field

        public QueueStorageFunction(ILogger<QueueStorageFunction> logger)
        {
            _logger = logger; // Initialize the logger
            
        }

        [Function("ProcessOrders")] // Attribute marking this method as an Azure Function named "ProcessOrders".
        public async Task<IActionResult> Run(
    // This parameter triggers the function on HTTP requests with the specified authorization level and method (POST).
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            // Log an informational message indicating that the order processing has started.
            _logger.LogInformation("Processing order");

            // Retrieve the value of the "name" query parameter from the request.
            // This allows for personalized responses based on the user's input.
            string name = req.Query["name"];

            // Read the body of the HTTP request asynchronously to obtain the order data.
            // This is done by creating a new StreamReader on the request body stream and reading it until the end.
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Deserialize the request body JSON into an Order object using JsonConvert.
            // This converts the JSON representation of the order into a C# Order instance.
            Order order = JsonConvert.DeserializeObject<Order>(requestBody);

            // Construct a response message based on whether the "name" parameter was provided.
            // If "name" is null or empty, provide a generic success message.
            // If "name" is present, include it in a personalized success message.
            string responseMessage = string.IsNullOrEmpty(name)
                ? "This Http trigged successfully. Pass a name in the query string or in the request body for a personalised response"
                : $"Hello, {name}. The Http trigged function executed successfully";

            // Create a QueueServiceClient instance to connect to Azure Queue Storage.
            // The connection string is used to authenticate and authorize access to the storage account.
            QueueServiceClient processOrderClient = new QueueServiceClient("DefaultEndpointsProtocol=https;AccountName=st10251759cldv6212poe;AccountKey=1MxhETG4HxbJJj3wcyrjczRZl0f0tmeSOkJr+plos3kpTc7bMCgNe97edMX4MKtuMPfQRloMm905+AStFdqYUQ==;EndpointSuffix=core.windows.net");

            // Get a reference to the specific queue named "processorders" for processing orders.
            QueueClient processOrderQueue = processOrderClient.GetQueueClient("processorders");

            // Send a message to the queue containing details about the order being processed.
            // This includes the Order ID, Order Date, Product ID, and Customer Email for tracking.
            await processOrderQueue.SendMessageAsync($"Processing Order: Order ID: {order.OrderId}  OrderDate: {order.OrderDate}   ProductID: {order.ProductId}  Customer: {order.CustomerEmail}");

            // Return an OK response along with the constructed success message.
            // This indicates that the function has successfully processed the request.
            return new OkObjectResult(responseMessage);
        }


    }

    public class Order : ITableEntity
    {
        [Key]
        public int OrderId { get; set; }

        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        //Introduce validation sample
        [Required(ErrorMessage = "Please select a customer.")]
        public string? CustomerEmail { get; set; } // FK to the Customer who made the order

        [Required(ErrorMessage = "Please select a product.")]
        public int ProductId { get; set; } // FK to the Product being ordered

        // [Required(ErrorMessage = "Please select the date.")]
        public DateTime? OrderDate { get; set; }



    }

}
