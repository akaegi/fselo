module FsElo.Domain.Scoreboard.ScoreList

open FsElo.Domain.Scoreboard.Events

type ScoreEntry = {
    ScoreId: ScoreId
    Date: System.DateTimeOffset
    Players: PlayerId * PlayerId
    Score: Score
}

type ScoreList = ScoreEntry list 

module ScoreList =
    let empty = []
    
    let remove (id: ScoreId) (l: ScoreList): ScoreList =
        List.filter (fun e -> not (e.ScoreId = id)) l
    
    let tryFind (id: ScoreId) (l: ScoreList): ScoreEntry option =
        List.tryFind (fun e -> e.ScoreId = id) l
        
    let add (e: ScoreEntry) (l: ScoreList): ScoreList =
        let entry = tryFind e.ScoreId l
        if entry.IsSome then
            invalidOp (sprintf "Entry with id %s already exists" (e.ScoreId.ToString()))
        else
            e :: l
        
    let findInRange (range: System.DateTimeOffset * System.DateTimeOffset) (l: ScoreList): ScoreEntry list =
        let (start, ``end``) = range
        List.filter (fun e -> start <= e.Date && e.Date <= ``end``) l