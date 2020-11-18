using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eveneum.Documents;
using FsElo.WebApp.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FsElo.WebApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();

            await host.Services.GetService<EveneumInitializer>().DoAsync();
            
            var cosmosClient = host.Services.GetService<CosmosClient>();
            var updater = host.Services.GetService<ScoreboardReadModelUpdater>();
            ChangeFeedProcessor proc = await StartReadModelUpdaterChangeFeedProcessorAsync(cosmosClient, updater);

            try
            {
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                await proc.StopAsync();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        
        private static async Task<ChangeFeedProcessor> StartReadModelUpdaterChangeFeedProcessorAsync(
            CosmosClient cosmosClient, ScoreboardReadModelUpdater updater)
        {
            var db = cosmosClient.GetDatabase("FsElo");
            await db.CreateContainerIfNotExistsAsync("ReadModelUpdater", "/id");
            Container leaseContainer = cosmosClient.GetContainer("FsElo", "ReadModelUpdater");

            ChangeFeedProcessor changeFeedProcessor = cosmosClient
                .GetContainer("FsElo", "Events")
                .GetChangeFeedProcessorBuilder<EveneumDocument>("ScoreboardReadModelBuilder", updater.HandleChangesAsync)
                .WithInstanceName("consoleHost")
                .WithLeaseContainer(leaseContainer)
                .WithStartTime(DateTime.MinValue.ToUniversalTime())
                .Build();
            

            Console.WriteLine("Starting Change Feed Processor for Product Backups...");
            await changeFeedProcessor.StartAsync();
            Console.WriteLine("Product Backup started");
            return changeFeedProcessor;
        }
    }
}