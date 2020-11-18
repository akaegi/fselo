using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.Tasks;
using FsElo.Domain.Scoreboard.Events;
using FsElo.WebApp.Application;
using Microsoft.AspNetCore.Mvc;

namespace FsElo.WebApp.Controllers
{
    [ApiController]
    public class ScoreboardController: ControllerBase
    {
        private readonly ScoreboardCommandHandler _handler;

        public ScoreboardController(ScoreboardCommandHandler handler)
        {
            _handler = handler;
        }
        
        [HttpPost]
        [Route("api/scoreboard")]
        public async Task<IActionResult> CreateNewBord([FromBody] string commandInput)
        {
            try
            {
                var result = await _handler.HandleAsync(UserInfo, String.Empty, commandInput);
                return Ok(result);
            }
            catch (ScoreboardException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("api/scoreboard/{boardId}")]
        public async Task<IActionResult> BoardAction(string boardId, [FromBody] string commandInput)
        {
            try
            {
                var result = await _handler.HandleAsync(UserInfo, boardId, commandInput);
                return Ok(result);
            }
            catch (ScoreboardException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private UserInfo UserInfo => new UserInfo
        {
            User = this.User?.Identity?.Name ?? String.Empty,
            Culture = CultureInfo.CurrentCulture,
            UtcOffset = TimeSpan.FromHours(2),
        };
    }
}