module FsElo.Domain.ScoreboardInputParser

open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.Scoreboard
open FParsec

let isWhitespace = function
    | ' ' | '\t' | '\n' | '\r' -> true
    | _ -> false

let pBoardId: Parser<string, unit> = many1SatisfyL (isWhitespace >> not) "board id"

// --board [boardId]
let pBoardFlag: Parser<string, unit> =
    pstring "--board"
    >>. spaces1
    >>. pBoardId

let pPlayerName: Parser<string, unit> = many1SatisfyL (isWhitespace >> not) "player name"

let pScoreType: Parser<ScoreType, unit> =
    (pstring "tt" <|> pstring "tabletennis") >>. (preturn TableTennis)
    <?> "ScoreType (tt)"

type ScoreboardInputParseResult = {
    BoardId: string
    Command: Command;
}

type ScoreboardInputParser = Parser<ScoreboardInputParseResult, unit>

// open scoreboard tt [boardId]
let pOpenScoreboard: ScoreboardInputParser =
    pstring "open scoreboard"
    >>. spaces1
    >>. pScoreType
    .>> spaces1
    .>>. pBoardId
    |>> (fun (typ, boardId) -> { BoardId = boardId
                                 Command = OpenScoreboard { BoardId = boardId; Type = typ } })
    .>> spaces
    .>> eof

// register player [playerName] --board [boardId]
// register player [boardId] [playerName]
let pRegisterPlayer: ScoreboardInputParser =
    let v1 = 
        pstring "register player"
        >>. spaces1
        >>. pPlayerName
        .>> spaces1
        .>>. pBoardFlag
        |>> (fun (p, b) -> (b, p))
    
    let v2 = 
        pstring "register player"
        >>. spaces1
        >>. pBoardId
        .>> spaces1
        .>>. pPlayerName
        
    attempt v1
    <|> v2
    |>> (fun (boardId, playerName) -> { BoardId = boardId
                                        Command = RegisterPlayer { Name = playerName } })

let parser =
    pOpenScoreboard
    <|> pRegisterPlayer
    

let parse (input: string): Result<ScoreboardInputParseResult, string> =
    
    match run parser input with
    | Success (result, _, _) -> Result.Ok result
    | Failure (msg, _, _) -> Result.Error msg
    
