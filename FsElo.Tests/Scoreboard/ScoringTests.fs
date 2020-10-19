module FsElo.Tests.ScoringTests

open System
open Xunit
open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.ScoreValidation
open FsElo.Domain.Scoreboard.Scoreboard
open FsElo.Tests.ScoreboardSpecification

let boardOpened = ScoreboardOpened {
    Type = TableTennis
    BoardId = BoardId "season-2020"
    Date = DateTimeOffset.Now
}

let p1 = {
    Id = Guid.NewGuid()
    Name = PlayerName "Leo"
    Date = DateTimeOffset.Now
}

let p2 = {
    Id = Guid.NewGuid()
    Name = PlayerName "Mattias"
    Date = DateTimeOffset.Now
}

let p3 = {
    Id = Guid.NewGuid()
    Name = PlayerName "Kristoffer"
    Date = DateTimeOffset.Now
}

let anytime = DateTimeOffset.Now

[<Fact>]
let ``score cannot be entered if one of the players is *not yet* registered`` () =
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2]
    |> ``when`` (EnterScore {
            Players = (p1.Name, PlayerName "Mr. X")
            Score = ""
            Date = anytime
        })
    |> thenThrows<PlayerNotRegisteredException>

[<Fact>]
let ``invalid score cannot be entered`` () =
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2]
    |> ``when`` (EnterScore { Players = (p1.Name, p2.Name); Score = "2:1a"; Date = anytime})
    |> thenThrows<InvalidScoreException>
    
[<Fact>]
let ``score of two registered players can be entered`` () =
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2]
    |> ``when`` (EnterScore { Players = (p1.Name, p2.Name); Score = "2:1"; Date = anytime})
    |> thenMatches (fun events ->
        match events with
        | [ScoreEntered e] -> not (e.ScoreId = Guid.Empty) && e.Score = (2, 1)
        | _ -> false)

[<Fact>]
let ``score is rejected if there is another score of the same players within the last hour`` () =
    let date1 = anytime
    let score1 = { ScoreId = Guid.NewGuid(); Players = (p1.Id, p2.Id); Date = date1; Score = (1, 1) }
    let halfHour = TimeSpan.FromMinutes(30.0)
    
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2; ScoreEntered score1]
    |> ``when`` (EnterScore { Players = (p1.Name, p2.Name); Date = date1.Add(halfHour); Score = "1:2" })
    |> thenThrows<ScoreEntryException>

[<Fact>]
let ``score is entered if there is another score of *different* players within the last hour`` () =
    let date1 = anytime
    let score1 = { ScoreId = Guid.NewGuid(); Players = (p1.Id, p2.Id); Date = date1; Score = (1, 1) }
    let halfHour = TimeSpan.FromMinutes(30.0)
    
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2; PlayerRegistered p3; ScoreEntered score1]
    |> ``when`` (EnterScore { Players = (p1.Name, p3.Name); Date = date1.Add(halfHour); Score = "1:2" })
    |> thenMatches (fun entries -> entries.Length > 0)

[<Fact>]
let ``score can be withdrawn`` () =
    let score1 = { ScoreId = Guid.NewGuid(); Players = (p1.Id, p2.Id); Date = anytime; Score = (1, 1) }
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2; PlayerRegistered p3; ScoreEntered score1]
    |> ``when`` (WithdrawScore score1.ScoreId)
    |> thenMatches (fun events ->
        match events with
        | [ScoreWithdrawn w] -> w.ScoreId = score1.ScoreId
        | _ -> false)

    
[<Fact>]
let ``score can be fixed`` () =
    let score1 = { ScoreId = Guid.NewGuid(); Players = (p1.Id, p2.Id); Date = anytime; Score = (1, 1) }
    given [boardOpened; PlayerRegistered p1; PlayerRegistered p2; PlayerRegistered p3; ScoreEntered score1]
    |> ``when`` (FixScore { ScoreId = score1.ScoreId; Score = "1:2"; Date = score1.Date })
    |> thenMatches (fun events ->
        match events with
        | [ScoreFixed f] -> f.Score = (1, 2)
        | _ -> false)
