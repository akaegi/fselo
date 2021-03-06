﻿using System;
using System.Threading.Tasks;
using Eveneum;
using FsElo.Domain.Scoreboard;
using FsElo.Domain.Scoreboard.Events;
using Microsoft.Azure.Cosmos;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var dt = DateTimeOffset.Parse("2020.10.21 17:17 +02:00");
            
            var tz = TimeZoneInfo.Local;

            var offset = TimeSpan.FromHours(3);
            
            var dt2 = dt.ToOffset(offset);
            Console.WriteLine(dt2);
        }
        
        static async Task Main2(string[] args)
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


            var boardId = BoardId.NewBoardId("season-2020");
            Event evt = Event.NewScoreboardOpened(new ScoreboardOpened(boardId, ScoreType.TableTennis,
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