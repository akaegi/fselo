namespace FsElo.Domain.ScoreboardEntry

open System
open Newtonsoft.Json

type ScoreboardEntryOthersw = {
    
    // Cosmos DB entries must have an id field!
    [<JsonProperty("id")>]
    ScoreId: string
    
    [<JsonProperty("boardId")>]
    BoardId: string
    
    [<JsonProperty("idPlayer1")>]
    IdPlayer1: string
    
    [<JsonProperty("idPlayer2")>]
    IdPlayer2: string
    
    [<JsonProperty("score")>]
    Score: string
    
    [<JsonProperty("date")>]
    Date: DateTimeOffset  
}