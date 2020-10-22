using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace FsElo.WebApp
{
    public static class CosmosClientProvider
    {
        public static void ProvideCosmosClient(this IServiceCollection services)
        {
            services.AddSingleton<CosmosClient>(CreateCosmosClient);
        }

        private static CosmosClient CreateCosmosClient(IServiceProvider sp)
        {
            return new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
        }
    }
}