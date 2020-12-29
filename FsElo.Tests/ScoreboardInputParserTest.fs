module FsElo.Tests.ScoreboardInputParserTest

open System
open System.Globalization
open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.Scoreboard
open FsElo.Domain.ScoreboardInputParser
open Xunit
open Xunit.Sdk

let input (inp: string) = inp

let parse = parseScoreboardCommand CultureInfo.CurrentCulture (TimeSpan.FromHours(2.))

let parsesTo (pred: Command -> bool) (input: string) =
    match parse input with
    | Ok cmd ->
        pred cmd |> Assert.True
    | Error e -> raise (XunitException e)
    
let failsToParse (input: string) =
    match parse input with
    | Ok _ -> raise (XunitException "Expected parse failure")
    | Error _ -> ()
    
    
[<Fact>]
let ``open scoreboard`` () =
    input "open scoreboard tt b1"
    |> parsesTo (fun cmd ->
        match cmd with
        | OpenScoreboard o -> o.BoardId = "b1" && o.Type = TableTennis
        | _ -> false)

[<Fact>]
let ``open scoreboard with missing board id`` () =
    input "open scoreboard"
    |> failsToParse
    
[<Fact>]
let ``register player`` () =
    input "register player p1"
    |> parsesTo (fun cmd ->
        match cmd with
        | RegisterPlayer r -> r.Name = "p1"
        | _ -> false)

[<Fact>]
let ``enter score`` () =
    input "enter score alpha beta 1:2"
    |> parsesTo (fun cmd ->
        match cmd with
        | EnterScore e -> e.Score = "1:2" && e.Players = ("alpha", "beta")
        | _ -> false)