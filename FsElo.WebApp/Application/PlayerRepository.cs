using System;
using System.Threading.Tasks;
using FsElo.Domain.ScoreboardEntry;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace FsElo.WebApp.Application
{
    public class PlayerRepository
    {
        const string ContainerName = "Players";
        
        private readonly CosmosClient _cosmosClient;

        public PlayerRepository(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        public async Task UpdatePlayerAsync(string boardId, Guid playerId, string playerName)
        {
            var players = await PreparePlayersContainerAsync();
            var player = new PlayerEntry {Id = ToId(playerId), BoardId = boardId, Name = playerName};
            await players.UpsertItemAsync(player, new PartitionKey(boardId));
        }

        private string ToId(Guid id)
        {
            return id.ToString();
        }

        private async Task<Container> PreparePlayersContainerAsync()
        {
            var db = _cosmosClient.GetDatabase("FsElo");
            await db.CreateContainerIfNotExistsAsync(ContainerName, "/boardId");
            return _cosmosClient.GetContainer("FsElo", ContainerName);
        }
    }

    public class PlayerEntry
    {
        // Cosmos DB entries must have an id field!
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        
        [JsonProperty(PropertyName = "boardId")]
        public string BoardId { get; set; }
        
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}