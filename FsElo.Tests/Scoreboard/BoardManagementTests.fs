module FsElo.Tests.BoardManagementTests

open System
open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.Scoreboard
open ScoreboardSpecification
open Xunit

[<Fact>]
let ``board with null id cannot be opened`` () =
    given []
    |> ``when`` (OpenScoreboard { BoardId = null; Type = TableTennis })
    |> thenThrows<ArgumentNullException>
    
[<Fact>]
let ``board with empty id cannot be opened`` () =
    given []
    |> ``when`` (OpenScoreboard { BoardId = ""; Type = TableTennis })
    |> thenThrows<ArgumentException>
    
[<Fact>]
let ``board with too short id cannot be opened`` () =
    given []
    |> ``when`` (OpenScoreboard { BoardId = "ab"; Type = TableTennis })
    |> thenThrows<ArgumentException>
    
[<Fact>]
let ``board with invalid id cannot be opened`` () =
    given []
    |> ``when`` (OpenScoreboard { BoardId = "ab*!"; Type = TableTennis })
    |> thenThrows<ArgumentException>
    
[<Fact>]
let ``board with valid id can be opened`` () =
    given []
    |> ``when`` (OpenScoreboard { BoardId = "season-2020"; Type = TableTennis })
    |> thenMatches (fun events ->
        match events with
        | [ScoreboardOpened o] -> o.BoardId = (BoardId "season-2020")
        | _ -> false)

[<Fact>]
let ``an open board can be closed`` () =
    let date = DateTimeOffset.Parse("2020-10-19")
    let boardId = BoardId "season-2020"

    given [ScoreboardOpened { Type = TableTennis; BoardId = boardId; Date = date }]
    |> ``when`` CloseScoreboard
    |> thenMatches (fun events ->
        match events with
        | [ScoreboardClosed o] -> o.BoardId = (boardId)
        | _ -> false)