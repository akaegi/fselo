module FsElo.Tests.ScoreboardSpecification

open Xunit
open FsElo.Domain.Scoreboard


// A generic fold function that can be used on any aggregate
let inline fold events =
    let initial = (^S: (static member Initial: ^S) ()) 
    let evolve s = (^S: (static member Evolve: ^S -> (^E -> ^S)) s)
    List.fold evolve initial events


let given (initialEvents: Event list) = initialEvents


let ``when`` (command: Command) initialEvents = initialEvents, command


let ``then`` (expectedEvents: Event list) (initialEvents, command) =
    // printGiven initialEvents
    // printWhen command
    // printExpect expectedEvents

    fold initialEvents
    |> handle command
    |> (fun actualEvents -> Assert.Equal<System.Collections.Generic.IEnumerable<Event>> (expectedEvents, actualEvents))


let thenMatches (pred: (Event list -> bool)) (initialEvents, command) =
    fold initialEvents
    |> handle command
    |> pred
    |> Assert.True


let thenThrows<'Ex when 'Ex :> System.Exception> (initialEvents, command) =
    // printGiven events
    // printWhen command
    // printExpectThrows typeof<'Ex>

    (fun () ->
        fold initialEvents
        |> handle command
        |> ignore)
    |> Assert.Throws<'Ex>