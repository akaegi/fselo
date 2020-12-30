module Program

open System
open System.Globalization
open FsElo.Domain.ScoreboardInputParser

let parse = parseScoreboardCommand CultureInfo.CurrentCulture (TimeSpan.FromHours(2.))

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
