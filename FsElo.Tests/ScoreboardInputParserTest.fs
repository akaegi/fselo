module FsElo.Tests.ScoreboardInputParserTest

open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.Scoreboard
open FsElo.Domain.ScoreboardInputParser
open Xunit
open Xunit.Sdk

let input (inp: string) = inp

let parsesTo (boardId: string) (pred: Command -> bool) (input: string) =
    match parse input with
    | Ok parseResult ->
        boardId = parseResult.BoardId |> Assert.True
        pred parseResult.Command |> Assert.True
    | Error e -> raise (XunitException e)
    
let failsToParse (input: string) =
    match parse input with
    | Ok _ -> raise (XunitException "Expected parse failure")
    | Error _ -> ()
    
    
[<Fact>]
let ``open scoreboard`` () =
    input "open scoreboard tt b1"
    |> parsesTo "b1" (fun cmd ->
        match cmd with
        | OpenScoreboard o -> o.BoardId = "b1" && o.Type = TableTennis
        | _ -> false)

[<Fact>]
let ``open scoreboard with missing board id`` () =
    input "open scoreboard"
    |> failsToParse
    
[<Fact>]
let ``register player `` () =
    input "register player b1 p1"
    |> parsesTo "b1" (fun cmd ->
        match cmd with
        | RegisterPlayer r -> r.Name = "p1"
        | _ -> false)

[<Fact>]
let ``register player with board flag`` () =
    input "register player p1 --board b1"
    |> parsesTo "b1" (fun cmd ->
        match cmd with
        | RegisterPlayer r -> r.Name = "p1"
        | _ -> false)