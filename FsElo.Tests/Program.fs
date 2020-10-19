module Program

open System
open FsElo.Domain.ScoreboardInputParser

let rec nextInput () =
    Console.WriteLine("Enter scoreboard input:")
    
    let line = Console.ReadLine()
    
    match parse line with
    | Ok res -> printfn "Parsed: %A" res
    | Error e -> printfn "Parse failed! %s" e
    
    nextInput ()
    
let [<EntryPoint>] main _ =
    nextInput ()
    0
