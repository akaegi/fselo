using System;
using System.Threading.Tasks;
using Eveneum;
using FsElo.Domain.Scoreboard;
using Microsoft.Azure.Cosmos;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            var database = "Eveneum";
            var collection = "Events";

            var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            var dbr = await client.CreateDatabaseIfNotExistsAsync(database);
            await dbr
                .Database
                .CreateContainerIfNotExistsAsync(new ContainerProperties(collection, "/StreamId"));

            IEventStore eventStore = new EventStore(client, database, collection);
            await eventStore.Initialize();

            var streamId = "4d4b7ed6-ac4c-4e45-b061-3d65541106bf";
            StreamResponse sr = await eventStore.ReadStream(streamId);
            Stream stream = sr.Stream.Value;
            
            await eventStore.WriteToStream(
                streamId,
                new []{ new EventData(streamId, "body", "metadata", 27), }, 
                stream.Version);
            
            Console.WriteLine("Test app finished");
        }
        
        static async Task Main1(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            var database = "Eveneum";
            var collection = "Events2";

            var client = new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            var dbr = await client.CreateDatabaseIfNotExistsAsync(database);
            await dbr
                .Database
                .CreateContainerIfNotExistsAsync(new ContainerProperties(collection, "/StreamId"));

            IEventStore eventStore = new EventStore(client, database, collection);
            await eventStore.Initialize();


            var boardId = Events.BoardId.NewBoardId("season-2020");
            Events.Event evt = Events.Event.NewScoreboardOpened(new Events.ScoreboardOpened(boardId, Events.ScoreType.TableTennis,
                DateTimeOffset.Now));
            
            var streamId = Guid.NewGuid().ToString();
            EventData[] events = new[]
            {
                new EventData(streamId, "hello", "blup", 1), 
                new EventData(streamId, evt, "blup", 3), 
            };
            
            await eventStore.WriteToStream(streamId, events);
            // await eventStore.CreateSnapshot(streamId, 7, GetSnapsho1ForVersion(7));

            var stream = await eventStore.ReadStream(streamId);
            
            Console.WriteLine("Test app finished");
        }
    }
}