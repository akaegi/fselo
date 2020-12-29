using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FsElo.Domain.Scoreboard.Events;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace FsElo.WebApp.Application
{
    public class ScoreboardReadModelDataAccess
    {
        const string ContainerName = "Scoreboard";
        
        private readonly CosmosClient _cosmosClient;
        private readonly IMemoryCache _cache;

        public ScoreboardReadModelDataAccess(CosmosClient cosmosClient, ScoreboardMemoryCache cache)
        {
            _cache = cache.Cache;
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

        public async Task UpdatePlayerAsync(string boardId, Guid playerId, string playerName)
        {
            // for now, we don't have to update previous scoreboard entries... 
            // (Player names currently cannot be changed)
            var scoreboard = await PrepareScoreboardContainerAsync();
            var player = new PlayerEntry {Id = ToId(playerId), BoardId = boardId, Name = playerName};
            await scoreboard.UpsertItemAsync(player, new PartitionKey(boardId));

            UpdateCache(player);
        }

        public async Task RemoveScoreEntryAsync(string boardId, Guid scoreId)
        {
            var scoreboard = await PrepareScoreboardContainerAsync();
            await scoreboard.DeleteItemAsync<ScoreEntry>(ToId(scoreId), new PartitionKey(boardId));
        }

        private Task<string> GetPlayerNameAsync(string playerId, string boardId)
        {
            return _cache.GetOrCreateAsync($"id-{playerId}", e => CreatePlayerNameCacheEntry(e, playerId, boardId));
        }
        
        private void UpdateCache(PlayerEntry entry)
        {
            // put the player name into the in-mem cache
            _cache.CreateEntry($"id-{entry.Id}")
                .SetSize(entry.Name.Length)
                .SetValue(entry.Name);
        }
        
        private async Task<string> CreatePlayerNameCacheEntry(ICacheEntry entry, string playerId, string boardId)
        {
            var scoreboard = await PrepareScoreboardContainerAsync();

            PlayerEntry playerEntry = await scoreboard.ReadItemAsync<PlayerEntry>(
                playerId, new PartitionKey(boardId));

            entry.SetSize(playerEntry.Name.Length);  // cache entry size measured in number of characters
            return playerEntry.Name;
        }

        private async Task<Container> PrepareScoreboardContainerAsync()
        {
            var db = _cosmosClient.GetDatabase("FsElo");
            // partition key is lower key as is JSON property entry.boardId
            await db.CreateContainerIfNotExistsAsync(ContainerName, "/boardId");
            return _cosmosClient.GetContainer("FsElo", ContainerName);
        }

        private string ToId(Guid scoreId) => scoreId.ToString();

        public async IAsyncEnumerable<QueryScoreEntryResultItem> QueryScoreEntriesAsync(QueryScoreEntriesFilter f)
        {
            if (String.IsNullOrEmpty(f.BoardId))
                throw new ArgumentOutOfRangeException(nameof(f.BoardId));
            
            if (string.IsNullOrEmpty(f.Player1))
                throw new ArgumentOutOfRangeException(nameof(f.Player1));

            var scoreboard = await PrepareScoreboardContainerAsync();

            var q = scoreboard.GetItemLinqQueryable<ScoreEntry>()
                .Where(e => e.BoardId == f.BoardId && e.Player1.Name == f.Player1);

            if (f.Player2 != null)
            {
                q = q.Where(e => e.Player2.Name == f.Player2);
            }

            q = q.OrderByDescending(e => e.Date);

            var iter = q.ToFeedIterator();
            
            // "transform" feed iterator to AsyncEnumerable
            while (iter.HasMoreResults)
            {
                foreach (var item in await iter.ReadNextAsync())
                {
                    yield return new QueryScoreEntryResultItem
                    {
                        Player1 = item.Player1.Name,
                        Player2 = item.Player2.Name,
                        Score = item.Score,
                        Date = item.Date,
                    };
                }
            }
        }
    }

    public class QueryScoreEntriesFilter
    {
        public string BoardId { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
    }

    public class QueryScoreEntryResultItem
    {
        public string Player1 { get; set; }
        
        public string Player2 { get; set; }
        
        public string Score { get; set; }
        
        public DateTimeOffset Date { get; set; }
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