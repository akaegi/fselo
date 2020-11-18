using Microsoft.AspNetCore.Mvc;

namespace FsElo.WebApp.Controllers
{
    [ApiController]
    public class ScoreListController: ControllerBase
    {
        
        
        /*
SELECT s.score, s.date FROM Scoreboard s
WHERE s.boardId = "test1" and s.isScoreEntry = true
ORDER BY s.date DESC 
        */
    }
}