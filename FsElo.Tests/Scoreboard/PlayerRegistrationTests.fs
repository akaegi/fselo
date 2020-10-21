module FsElo.Tests.PlayerRegistration

open System
open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.Scoreboard
open ScoreboardSpecification
open Xunit

let boardOpened = ScoreboardOpened {
    Type = TableTennis
    BoardId = BoardId "season-2020"
    Date = DateTimeOffset.Now
}

[<Fact>]
let ``cannot register player with null name`` () =
    given [boardOpened]
    |> ``when`` (RegisterPlayer { Name = null })
    |> thenThrows<ArgumentNullException>

[<Fact>]
let ``cannot register player with empty name`` () =
    given [boardOpened]
    |> ``when`` (RegisterPlayer { Name = "" })
    |> thenThrows<ArgumentException>
    
[<Fact>]
let ``cannot register player with a too short name`` () =
    given [boardOpened]
    |> ``when`` (RegisterPlayer { Name = "ab" })
    |> thenThrows<ArgumentException>

[<Fact>]
let ``can register player with a valid name`` () =
    given [boardOpened]
    |> ``when`` (RegisterPlayer { Name = "Fritz" })
    |> thenMatches (fun events ->
        match events with
        | [PlayerRegistered r] -> not (r.PlayerId = Guid.Empty) && r.Name = (PlayerName "Fritz")
        | _ -> false)
