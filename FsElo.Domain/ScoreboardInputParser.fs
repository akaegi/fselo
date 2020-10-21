module FsElo.Domain.ScoreboardInputParser

open System
open System.Globalization
open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.Scoreboard
open FParsec

let isWhitespace = function
    | ' ' | '\t' | '\n' | '\r' -> true
    | _ -> false

let pWordNoWhitespace: Parser<string, unit> =
    many1Satisfy (fun c -> not (isWhitespace c))
    
let pWordInParenthesis: Parser<string, unit> =
    skipChar '"'
    >>. many1Satisfy (fun c -> not (c = '"'))
    .>> skipChar '"'
    
let pBoardId: Parser<string, unit> =
    pWordNoWhitespace <?> "board id"

let pPlayerName: Parser<string, unit> =
    (pWordInParenthesis <|> pWordNoWhitespace) <?> "player name"

let pDate (culture: CultureInfo) (utcOffset: TimeSpan) : Parser<DateTimeOffset, unit> =
    (pWordInParenthesis <|> pWordNoWhitespace)
    >>= (fun str ->
        try
            let dt = DateTimeOffset.Parse(str, culture.DateTimeFormat)
            preturn (dt.ToOffset(utcOffset))
        with
            :? FormatException -> fail "Invalid date")

let pScoreType: Parser<ScoreType, unit> =
    (pstring "tt" <|> pstring "tabletennis") >>. (preturn TableTennis)
    <?> "ScoreType (tt)"

type ScoreboardInputParseResult = {
    BoardId: string
    Command: Command;
}

type ScoreboardInputParser = Parser<Command, unit>

// open scoreboard tt [boardId]
let pOpenScoreboard: ScoreboardInputParser =
    skipString "open scoreboard"
    >>. spaces1
    >>. pScoreType
    .>> spaces1
    .>>. pBoardId
    |>> (fun (typ, boardId) -> OpenScoreboard { BoardId = boardId; Type = typ })
    .>> spaces
    .>> eof

// register player [playerName]
let pRegisterPlayer: ScoreboardInputParser =
    skipString "register player"
    >>. spaces1
    >>. pPlayerName
    .>> eof
    |>> (fun playerName -> RegisterPlayer { Name = playerName })

// enter score [player1] [player2] [score] [date]?
let pEnterScore (culture: CultureInfo) (utcOffset: TimeSpan): ScoreboardInputParser =
    let pNoDate = preturn None
    let pDateSuffix =
        spaces1
        >>. (opt (pDate culture utcOffset))
        
    skipString "enter score"
    >>. spaces1
    >>. pPlayerName
    .>> spaces1
    .>>. pPlayerName
    .>> spaces1
    .>>. (pWordNoWhitespace <?> "score")
    .>>. (pDateSuffix <|> pNoDate)
    .>> eof
    |>> (fun (((p1, p2), score), date) -> EnterScore { Players = (p1, p2); Score = score; Date = date })
        

let parser (culture: CultureInfo) (utcOffset: TimeSpan) =
    pOpenScoreboard
    <|> pRegisterPlayer
    <|> pEnterScore culture utcOffset
    

let parseScoreboardCommand (culture: CultureInfo) (utcOffset: TimeSpan) (input: string): Result<Command, string> =
    match run (parser culture utcOffset) input  with
    | Success (result, _, _) -> Result.Ok result
    | Failure (msg, _, _) -> Result.Error msg
    
