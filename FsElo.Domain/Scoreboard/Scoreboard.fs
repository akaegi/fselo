module FsElo.Domain.Scoreboard.Scoreboard

open System
open System.Text.RegularExpressions
open FsElo.Domain.Scoreboard.Events
open FsElo.Domain.Scoreboard.ScoreList
open FsElo.Domain.Scoreboard.ScoreValidation
open FsElo.Domain.Scoreboard.PlayerRegistration

// ----- Commands -----

type Command =
    | OpenScoreboard of OpenScoreboard
    | CloseScoreboard
    | RegisterPlayer of RegisterPlayer
    | EnterScore of EnterScore
    | WithdrawScore of ScoreId
    | FixScore of FixScore

and OpenScoreboard = {
      BoardId: string
      Type: ScoreType
}


and RegisterPlayer = {
    Name: string
}

and EnterScore = {
    Players: string * string
    Score: string
    Date: DateTimeOffset option
}

and FixScore = {
    ScoreId: ScoreId
    Score: string
    Date: DateTimeOffset
}


// ----- Exceptions -----

exception BoardNotOpenException of string
let boardNotOpen s = raise (BoardNotOpenException s)

exception BoardAlreadyOpenedException of string
let boardAlreadyOpened s = raise (BoardAlreadyOpenedException s) 

exception PlayerAlreadyRegisteredException of string
let playerAlreadyRegistered s = raise (PlayerAlreadyRegisteredException s)

exception PlayerNotRegisteredException of string
let playerNotRegistered s = raise (PlayerNotRegisteredException s)

exception ScoreNotFoundException of string
let scoreNotFound s = raise (ScoreNotFoundException s)

exception ScoreEntryNotAllowed 

// ----- Scoreboard Validation -----

let validateBoardId(s: string) =
    let m = Regex.Match(s, "[0-9a-zA-Z][0-9a-zA-Z-_]{2,}")
    if m.Success
        then (BoardId m.Value)
        else invalidArg "boardName" (sprintf "%s is not a valid board name" s)

// ----- State ----
type State = 
    | Initial
    | Opened of OpenScoreboardState
    | Closed
    static member InitialState = Initial

and OpenScoreboardState = {
      BoardId: BoardId
      BoardType: ScoreType
      PlayerRegistry: PlayerRegistry
      ScoreList: ScoreList
}

type ScoreEntered with
    member this.ToScoreEntry: ScoreEntry = {
        ScoreId = this.ScoreId
        Date = this.Date
        Players = this.Players
        Score = this.Score
    }
    
// ----- Command Handlers -----

let openScoreboard (args: OpenScoreboard) = function
    | Initial -> 
        let boardName = validateBoardId args.BoardId
        [ScoreboardOpened { BoardId = boardName; Type = args.Type; Date = DateTimeOffset.Now }]
    | _ -> boardAlreadyOpened "Scoreboard cannot be opened twice"

let closeScoreboard = function
    | Opened s -> [ScoreboardClosed { Date = DateTimeOffset.Now; BoardId = s.BoardId; }]
    | _ -> invalidOp "Only an opened board can be closed"
        
let registerPlayer (args: RegisterPlayer) = function
    | Opened state -> 
        let (PlayerName name) as playerName = validatePlayerName args.Name
        if (PlayerRegistry.playerExists playerName state.PlayerRegistry)
            then playerAlreadyRegistered (sprintf "Player %s already registered on board" name)
            else [PlayerRegistered { BoardId = state.BoardId; PlayerId = createPlayerId ()
                                     Name = playerName; Date = DateTimeOffset.Now;  }]
    | _ -> boardNotOpen "Player can only be registered when board is open"

let enterScore (args: EnterScore) = function
    | Opened state ->
        let lookupOrThrow name =
            match PlayerRegistry.lookup name state.PlayerRegistry with
            | Some id -> id
            | None ->
                playerNotRegistered (sprintf "Player %s is not registered" name)
        let (n1, n2) = args.Players
        let players = (lookupOrThrow n1, lookupOrThrow n2)
        let date = args.Date |> Option.defaultValue DateTimeOffset.Now
        let score = validateScore date  players args.Score state.BoardType state.ScoreList 
        [ScoreEntered { BoardId = state.BoardId; ScoreId = createScoreId ()
                        Score = score; Players = players; Date = date }]
    | _ -> boardNotOpen "Score can only be entered when board is open"
    
let withdrawScore (id: ScoreId) = function
    | Opened state ->
        match ScoreList.tryFind id state.ScoreList with
        | None -> []
        | Some _ -> [ScoreWithdrawn { BoardId = state.BoardId; ScoreId = id; Date = DateTimeOffset.Now }]
    | _ -> boardNotOpen "Score can only be withdrawn when board is open"
    
let fixScore (args: FixScore) = function
    | Opened state ->
        match ScoreList.tryFind args.ScoreId state.ScoreList with
        | None -> scoreNotFound (sprintf "Score with id %s not found" (args.ScoreId.ToString()))
        | Some oldScore ->
            let l' = ScoreList.remove args.ScoreId state.ScoreList
            let newScore = validateScore args.Date  oldScore.Players args.Score state.BoardType l'
            [ScoreFixed { BoardId = state.BoardId; ScoreId = args.ScoreId
                          Score = newScore; Players = oldScore.Players; Date = args.Date }]
    | _ -> boardNotOpen "Score can only be fixed when board is open"
    
let handle (c: Command) =
    match c with
    | OpenScoreboard arg -> openScoreboard arg
    | CloseScoreboard -> closeScoreboard
    | RegisterPlayer p -> registerPlayer p
    | EnterScore s -> enterScore s
    | WithdrawScore w -> withdrawScore w
    | FixScore f -> fixScore f
    

// ----- State change function -----
type State with 
    static member Evolve (s: State) (event: Event): State =
        let invalidState () =
            invalidOp (sprintf "Unexpected event %s in state %s" (event.ToString()) (s.ToString()))
            
        match event with
        | ScoreboardOpened e -> Opened {
                BoardId = e.BoardId
                BoardType = e.Type
                PlayerRegistry = PlayerRegistry.create
                ScoreList = ScoreList.empty
            }
        | ScoreboardClosed _ -> Closed
        | PlayerRegistered p ->
            match s with
            | Opened s' -> 
                let reg' = PlayerRegistry.update p.Name p.PlayerId s'.PlayerRegistry
                Opened { s' with PlayerRegistry = reg' }
            | _ -> invalidState ()
        | ScoreEntered e ->
            match s with
            | Opened s' ->
                let l' = ScoreList.add e.ToScoreEntry s'.ScoreList
                Opened { s' with ScoreList = l' }
            | _ -> invalidState ()
        | ScoreWithdrawn w ->
            match s with
            | Opened s' ->
                let l' = ScoreList.remove w.ScoreId s'.ScoreList
                Opened { s' with ScoreList = l' }
            | _ -> invalidState ()
        | ScoreFixed f ->
            match s with
            | Opened s' ->
                let l' =
                    s'.ScoreList
                        |> ScoreList.remove (f.ScoreId)
                        |> ScoreList.add (f.ToScoreEntry)
                Opened { s' with ScoreList = l' }
            | _ -> invalidState ()
