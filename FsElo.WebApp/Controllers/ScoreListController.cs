using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FsElo.WebApp.Application;
using Microsoft.AspNetCore.Mvc;

namespace FsElo.WebApp.Controllers
{
    [ApiController]
    public class ScoreListController: ControllerBase
    {
        private readonly ScoreboardReadModelDataAccess _dataAccess;

        public ScoreListController(ScoreboardReadModelDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }
        
        // TODO AK: Caching and Paging!

        [HttpGet]
        [Route("api/scores/{boardId}")]
        public IAsyncEnumerable<QueryScoreEntryResultItem> GetScores(
            [FromRoute] string boardId, 
            [FromQuery] [Required] string player, 
            [FromQuery] string adversary)
        {
            var entries = _dataAccess.QueryScoreEntriesAsync(new QueryScoreEntriesFilter
            {
                BoardId = boardId,
                Player1 = player,
                Player2 = adversary,
            });
            
            return entries;
        }
        
        /*
SELECT s.score, s.date FROM Scoreboard s
WHERE s.boardId = "test1" and s.isScoreEntry = true
ORDER BY s.date DESC 
        */
    }
}