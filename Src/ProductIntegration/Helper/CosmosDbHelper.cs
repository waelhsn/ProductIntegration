using Microsoft.Azure.Cosmos;
using System;

namespace ProductIntegration.Helper
{
    public static class CosmosDbHelper
    {
        public static string EndpointUri;
        public static string PrimaryKey;
        public static CosmosClient CosmosClient;

        static CosmosDbHelper()
        {
            try
            {
                EndpointUri = Environment.GetEnvironmentVariable("CosmosDB_URL") ?? throw new ArgumentNullException("CosmosDB_URL");
                PrimaryKey = Environment.GetEnvironmentVariable("Primary_Key") ?? throw new ArgumentNullException("Primary_Key");

                CosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize Cosmos DB client.", ex);
            }
        }

        public static Container GetContainer(string databaseName, string containerName)
        {
            var database = CosmosClient.GetDatabase(databaseName);
            return database.GetContainer(containerName);
        }
    }
}
