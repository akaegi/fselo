using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Eveneum;
using FsElo.WebApp.Application;
using Microsoft.AspNetCore.Mvc;

namespace FsElo.WebApp.Controllers
{
    [ApiController]
    public class ScoreListController: ControllerBase
    {
        private readonly ScoreboardReadModelProvider _provider;

        public ScoreListController(ScoreboardReadModelProvider provider)
        {
            _provider = provider;
        }
        
        // TODO AK: Caching and Paging!

        [HttpGet]
        [Route("api/scores/{boardId}")]
        public async Task<IEnumerable<ScoreListEntry>> GetScores(
            [FromRoute] string boardId, 
            [FromQuery] [Required] string player, 
            [FromQuery] string adversary)
        {
            ScoreListReadModel readModel = await _provider.ReadModelAsync(boardId);
            var scores = readModel.ScoreList(player, adversary);
            return scores;
        }
    }
}