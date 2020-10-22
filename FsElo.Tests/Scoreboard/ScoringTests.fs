module FsElo.Tests.ScoringTests

open System
open Xunit
open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.ScoreValidation
open FsElo.Domain.Scoreboard.Scoreboard
open FsElo.Tests.ScoreboardSpecification

let boardId = BoardId "season-2020"

let boardOpened = ScoreboardOpened {
    Type = TableTennis
    BoardId = boardId
    Date = DateTimeOffset.Now
}

let p1: PlayerRegistered = {
    PlayerId = Guid.NewGuid()
    Name = PlayerName "Leo"
    Date = DateTimeOffset.Now
}

let p2: PlayerRegistered = {
    PlayerId = Guid.NewGuid()
    Name = PlayerName "Mattias"
    Date = DateTimeOffset.Now
}

let p3: PlayerRegistered = {
    PlayerId = Guid.NewGuid()
    Name = PlayerName "Kristoffer"
    Date = DateTimeOffset.Now
}

let anytime = DateTimeOffset.Now

let mkScoreEntered (p1: PlayerRegistered) (p2: PlayerRegistered)
                   (score: Score) (date: DateTimeOffset): ScoreEntered =
    { ScoreId = Guid.NewGuid(); Players = (p1.PlayerId, p2.PlayerId); Date = date; Score = score }


[<Fact>]
let ``score cannot be entered if one of the players is *not yet* registered`` () =
    let (PlayerName n1) = p1.Name
    
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2]
    |> ``when`` (EnterScore { Players = (n1, "Mr. X"); Score = ""; Date = None })
    |> thenThrowsAny<PlayerNotRegisteredException>

[<Fact>]
let ``invalid score cannot be entered`` () =
    let (PlayerName n1) = p1.Name
    let (PlayerName n2) = p2.Name
    
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2]
    |> ``when`` (EnterScore { Players = (n1, n2); Score = "2:1a"; Date = None})
    |> thenThrowsAny<InvalidScoreException>
    
[<Fact>]
let ``score of two registered players can be entered`` () =
    let (PlayerName n1) = p1.Name
    let (PlayerName n2) = p2.Name
    
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2]
    |> ``when`` (EnterScore { Players = (n1, n2); Score = "2:1"; Date = None})
    |> thenMatches (fun events ->
        match events with
        | [ScoreEntered e] -> not (e.ScoreId = Guid.Empty) && e.Score = (2, 1)
        | _ -> false)

[<Fact>]
let ``score is rejected if there is another score of the same players within the last hour`` () =
    let date1 = anytime
    let score1 = mkScoreEntered p1 p2 (1, 1) date1
    let halfHour = TimeSpan.FromMinutes(30.0)
    let (PlayerName n1) = p1.Name
    let (PlayerName n2) = p2.Name
    
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2; ScoreEntered score1]
    |> ``when`` (EnterScore { Players = (n1, n2); Date = Some (date1.Add(halfHour)); Score = "1:2" })
    |> thenThrowsAny<ScoreboardException>

[<Fact>]
let ``score is entered if there is another score of *different* players within the last hour`` () =
    let date1 = anytime
    let score1 = mkScoreEntered p1 p2 (1, 1) date1
    let halfHour = TimeSpan.FromMinutes(30.0)
    let (PlayerName n1) = p1.Name
    let (PlayerName n3) = p3.Name
    
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2; PlayerRegistered p3; ScoreEntered score1]
    |> ``when`` (EnterScore { Players = (n1, n3); Date = Some (date1.Add(halfHour)); Score = "1:2" })
    |> thenMatches (fun entries -> entries.Length > 0)

[<Fact>]
let ``score can be withdrawn`` () =
    let score1 = mkScoreEntered p1 p2 (1, 1) anytime 
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2; PlayerRegistered p3; ScoreEntered score1]
    |> ``when`` (WithdrawScore score1.ScoreId)
    |> thenMatches (fun events ->
        match events with
        | [ScoreWithdrawn w] -> w.ScoreId = score1.ScoreId
        | _ -> false)

    
[<Fact>]
let ``score can be fixed`` () =
    let score1 = mkScoreEntered p1 p2 (1, 1) anytime
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2; PlayerRegistered p3; ScoreEntered score1]
    |> ``when`` (FixScore { ScoreId = score1.ScoreId; Score = "1:2"; Date = score1.Date })
    |> thenMatches (fun events ->
        match events with
        | [ScoreFixed f] -> f.Score = (1, 2)
        | _ -> false)
