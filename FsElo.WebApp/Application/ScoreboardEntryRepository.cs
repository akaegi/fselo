using System;
using System.Threading.Tasks;
using FsElo.Domain.Scoreboard.Events;
using FsElo.Domain.ScoreboardEntry;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace FsElo.WebApp.Application
{
    public class ScoreboardEntryRepository
    {
        const string ContainerName = "Scores";
        
        private readonly CosmosClient _cosmosClient;

        public ScoreboardEntryRepository(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        public async Task RemoveScoreAsync(string boardId, Guid scoreId)
        {
            var scores = await PrepareScoresContainerAsync();
            await scores.DeleteItemAsync<ScoreboardEntry>(ToId(scoreId), new PartitionKey(boardId));
        }

        private async Task<Container> PrepareScoresContainerAsync()
        {
            var db = _cosmosClient.GetDatabase("FsElo");
            // partition key is lower key as is JSON property entry.boardId
            await db.CreateContainerIfNotExistsAsync(ContainerName, "/boardId");
            return _cosmosClient.GetContainer("FsElo", ContainerName);
        }

        public async Task UpdateScoreEntryAsync(string boardId, ScoreEntered e)
        {
            string scoreId = ToId(e.ScoreId);
            
            string score = $"{e.Score.Item1}:{e.Score.Item2}";
            var entry = new ScoreboardEntry(scoreId, boardId, 
                ToId(e.Players.Item1), ToId(e.Players.Item2), score, e.Date);
            
            var scores = await PrepareScoresContainerAsync();
            await scores.UpsertItemAsync(entry, new PartitionKey(boardId));
        }

        private string ToId(Guid scoreId)
        {
            return scoreId.ToString();
        }
    }
    
    public class ScoreboardEntry2
    {
        [JsonProperty("scoreId")]
        public string ScoreId { get; set; }
    
        [JsonProperty("boardId")]
        public string BoardId { get; set; }
    
        [JsonProperty("idPlayer1")]
        public string IdPlayer1 { get; set; }
    
        [JsonProperty("idPlayer2")]
        public string IdPlayer2 { get; set; }
    
        [JsonProperty("score")]
        public string Score { get; set; }
    
        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }  
    }
}