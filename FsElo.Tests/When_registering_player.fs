module FsElo.Tests.``When registering player``

open Xunit
open System
open FsElo.Domain.PlayerRegistration
open FsElo.Domain.Scoreboard
open ScoreboardSpecification


[<Fact>]
let ``provided a valid and unique name it should register player`` () =
    given []
    |> ``when`` (RegisterPlayer { Name = "aka" })
    |> thenMatches (fun events -> 
        match events with
        | [PlayerRegistered r] -> r.Name = (ValidPlayerName "aka")
        | _ -> false)


[<Fact>]
let ``with an already existing name it should say PlayerAlreadyRegistered`` () =
    given [PlayerRegistered { Name = (ValidPlayerName "aka"); Date = DateTimeOffset.Now; Id = System.Guid.Empty }]
    |> ``when`` (RegisterPlayer { Name = "aka" })
    |> ``then`` [PlayerAlreadyRegistered "aka"]


[<Fact>]
let ``without a name it should throw`` () =
    given []
    |> ``when`` (RegisterPlayer { Name = null })
    |> thenThrows<ArgumentException>


[<Fact>]
let ``with a too short name it should throw`` () =
    given []
    |> ``when`` (RegisterPlayer { Name = "ak" })
    |> thenThrows<ArgumentException>
 