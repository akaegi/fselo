module FsElo.Domain.Scoreboard.ScoreValidation

open System
open System.Text.RegularExpressions
open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.ScoreList

let createScoreId (): ScoreId = Guid.NewGuid()

let invalidScore s = raise (InvalidScoreException s)

let private parseScoreWithColon (s: string): Score =
    let m = Regex.Match(s, "^([0-9]):([0-9])$")
    if m.Success then
        let s1 = (m.Groups.Item 1).Value |> int
        let s2 = (m.Groups.Item 2).Value |> int
        (s1, s2)
    else invalidScore "Expected a score of the form m:n"

// ---- Rules -----
let private requireValidTTScore (s: Score): unit =
    let (m, n) = s
    if m <= 0 && n <= 0 then
        invalidScore (sprintf "%i:%i is not a valid score for table tennis" m n)

type private RequireNoExistingScoreInRangeArgs = {
    From: DateTimeOffset
    To: DateTimeOffset
    Players: PlayerId * PlayerId
    List: ScoreList    
}

let private oneHour = TimeSpan.FromHours(1.0)

let private requireNoExistingScoreInRange (players: PlayerId * PlayerId)
                                          (timerange: DateTimeOffset * DateTimeOffset)
                                          (list: ScoreList): unit =
    let entries =
        ScoreList.findInRange timerange list
        |> List.filter (fun e -> e.Players = players)

    if entries.Length > 0 then
        invalidScore "Possible duplicate score"
        
    
let validateScore (date: DateTimeOffset)
               (players: PlayerId * PlayerId)
               (score: string)
               (scoreType: ScoreType)
               (list: ScoreList): Score =
    match scoreType with
    | TableTennis ->
        let (n, m) = parseScoreWithColon score
        requireValidTTScore (n, m)
        requireNoExistingScoreInRange players (date.Subtract(oneHour), date) list
        (n, m)
