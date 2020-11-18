using System;
using System.Threading.Tasks;
using FsElo.Domain.Scoreboard.Events;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace FsElo.WebApp.Application
{
    public class ScoreboardReadModelDataAccess
    {
        const string ContainerName = "Scoreboard";
        
        private readonly CosmosClient _cosmosClient;
        private readonly IMemoryCache _cache;

        public ScoreboardReadModelDataAccess(CosmosClient cosmosClient, IMemoryCache cache)
        {
            _cache = cache;
            _cosmosClient = cosmosClient;
        }

        public async Task UpdateScoreEntryAsync(string boardId, ScoreEntered e)
        {
            string scoreId = ToId(e.ScoreId);

            var id1 = ToId(e.Players.Item1);
            var id2 = ToId(e.Players.Item2);
            string score = $"{e.Score.Item1}:{e.Score.Item2}";
            var entry = new ScoreEntry
            {
                Id = scoreId,
                BoardId = boardId,
                Player1 = new ScoreEntry.Player {Id = id1, Name = await GetPlayerNameAsync(id1, boardId)},
                Player2 = new ScoreEntry.Player {Id = id2, Name = await GetPlayerNameAsync(id2, boardId)},
                Score = score,
                Date = e.Date,
            };
            
            var scoreboard = await PrepareScoreboardContainerAsync();
            await scoreboard.UpsertItemAsync(entry, new PartitionKey(boardId));
        }

        private Task<string> GetPlayerNameAsync(string playerId, string boardId)
        {
            return _cache.GetOrCreateAsync(playerId, e => CreatePlayerNameCacheEntry(e, playerId, boardId));
        }

        private async Task<string> CreatePlayerNameCacheEntry(ICacheEntry entry, string playerId, string boardId)
        {
            var scoreboard = await PrepareScoreboardContainerAsync();

            PlayerEntry playerEntry = await scoreboard.ReadItemAsync<PlayerEntry>(
                playerId, new PartitionKey(boardId));

            entry.SetSize(playerEntry.Name.Length);  // cache entry size measured in number of characters
            return playerEntry.Name;
        }

        public async Task UpdatePlayerAsync(string boardId, Guid playerId, string playerName)
        {
            // for now, we don't have to update previous scoreboard entries... 
            // (Player names currently cannot be changed)
            var scoreboard = await PrepareScoreboardContainerAsync();
            var player = new PlayerEntry {Id = ToId(playerId), BoardId = boardId, Name = playerName};
            await scoreboard.UpsertItemAsync(player, new PartitionKey(boardId));

            // put the player name into the in-mem cache
            _cache.CreateEntry(playerId)
                .SetSize(playerName.Length)
                .SetValue(playerName);
        }
        
        public async Task RemoveScoreEntryAsync(string boardId, Guid scoreId)
        {
            var scoreboard = await PrepareScoreboardContainerAsync();
            await scoreboard.DeleteItemAsync<ScoreEntry>(ToId(scoreId), new PartitionKey(boardId));
        }

        private async Task<Container> PrepareScoreboardContainerAsync()
        {
            var db = _cosmosClient.GetDatabase("FsElo");
            // partition key is lower key as is JSON property entry.boardId
            await db.CreateContainerIfNotExistsAsync(ContainerName, "/boardId");
            return _cosmosClient.GetContainer("FsElo", ContainerName);
        }

        private string ToId(Guid scoreId) => scoreId.ToString();
    }
    
    
    public class ScoreEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    
        [JsonProperty("boardId")]
        public string BoardId { get; set; }
    
        [JsonProperty("player1")]
        public Player Player1 { get; set; }
    
        [JsonProperty("player2")]
        public Player Player2 { get; set; }
    
        [JsonProperty("score")]
        public string Score { get; set; }
    
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }  
        
        [JsonProperty(PropertyName = "isScoreEntry")]
        public const bool IsScoreEntry = true;

        public class Player
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
        
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
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

        [JsonProperty(PropertyName = "isPlayerEntry")]
        public const bool IsPlayerEntry = true;
    }
}