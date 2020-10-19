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
    Players: PlayerName * PlayerName
    Score: string
    Date: DateTimeOffset
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
            else [PlayerRegistered { Id = createPlayerId (); Name = playerName; Date = DateTimeOffset.Now;  }]
    | _ -> boardNotOpen "Player can only be registered when board is open"

let enterScore (args: EnterScore) = function
    | Opened s ->
        let lookupOrThrow n =
            match PlayerRegistry.lookup n s.PlayerRegistry with
            | Some id -> id
            | None ->
                let (PlayerName pn) = n
                playerNotRegistered (sprintf "Player %s is not registered" pn)
        let (n1, n2) = args.Players
        let players = (lookupOrThrow n1, lookupOrThrow n2)
        let score = validateScore args.Date  players args.Score s.BoardType s.ScoreList 
        [ScoreEntered {
            ScoreId = createScoreId ()
            Score = score
            Players = players
            Date = args.Date
        }]
    | _ -> boardNotOpen "Score can only be entered when board is open"
    
let withdrawScore (id: ScoreId) = function
    | Opened s ->
        match ScoreList.tryFind id s.ScoreList with
        | None -> []
        | Some _ -> [ScoreWithdrawn { ScoreId = id; Date = DateTimeOffset.Now }]
    | _ -> boardNotOpen "Score can only be withdrawn when board is open"
    
let fixScore (args: FixScore) = function
    | Opened s ->
        match ScoreList.tryFind args.ScoreId s.ScoreList with
        | None -> scoreNotFound (sprintf "Score with id %s not found" (args.ScoreId.ToString()))
        | Some oldScore ->
            let l' = ScoreList.remove args.ScoreId s.ScoreList
            let newScore = validateScore args.Date  oldScore.Players args.Score s.BoardType l'
            [ScoreFixed { ScoreId = args.ScoreId; Score = newScore; Players = oldScore.Players; Date = args.Date }]
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
                let reg' = PlayerRegistry.update p.Name p.Id s'.PlayerRegistry
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
