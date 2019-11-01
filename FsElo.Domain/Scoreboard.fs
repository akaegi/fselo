module FsElo.Domain.Scoreboard

open PlayerRegistration
open SwissTableTennisBoard


// ----- Commands -----

type Command =
    | OpenScoreboard of OpenScoreboard
    | RegisterPlayer of RegisterPlayer

and OpenScoreboard = 
    { Name: string
      Type: ScoreboardType }

and ScoreboardType = SwissTableTennis

and RegisterPlayer = 
    { Name: string
      Args: RegiterPlayerArgs }

and RegiterPlayerArgs = 
    | SwissTableTennisArgs of SwissTableTennisRegisterPlayer


// ----- Exceptions -----

exception BoardNotOpenException of string
let boardNotOpen s = raise (BoardNotOpenException s) 


// ----- Scoreboard Validation -----
type ValidBoardName = ValidBoardName of string

let validateBoardName(bn: string) =
    if (isNull bn) || bn.Length < 3 // TODO AK: Use regex here (see README)
        then invalidArg "boardName" "bliblablu"
        else ValidBoardName bn

// ----- Events -----

type Event =
    | ScoreboardOpened of ScoreboardOpened
    | ScoreboardClosed of ScoreboardClosed
    | PlayerRegistered of PlayerRegistered
    | PlayerAlreadyRegistered of string
    
and ScoreboardOpened = 
    { Name: ValidBoardName
      Type: ScoreboardType
      Date: System.DateTimeOffset }

and ScoreboardClosed = 
    { Name: ValidBoardName
      Date: System.DateTimeOffset }

and PlayerRegistered = 
    { Id: PlayerId
      Name: ValidPlayerName
      Date: System.DateTimeOffset }


// ----- State ----
type State = 
    | Initial
    | Opened of OpenScoreboardState
    | Closed
    // static member Initial = Initial

and OpenScoreboardState =
    { BoardType: ScoreboardType
      PlayerRegistry: PlayerRegistry }


// ----- Command Handlers -----

let openScoreboard (args: OpenScoreboard) = function
    | Initial -> 
        let boardName = validateBoardName args.Name
        [ScoreboardOpened { Name = boardName; Type = args.Type; Date = System.DateTimeOffset.Now }]
    | _ -> invalidOp "Scoreboard can only be opened once"

let registerPlayer (p: RegisterPlayer) = function
    | Opened state -> 
        let (ValidPlayerName name) as playerName = validatePlayerName p.Name
        let boardRules = boardRules state.BoardType
        boardRules.validateRegisterPlayerArgs p.Args
        let args' = boardRules.initRegisterPlayerArgs p.Args
        if not (PlayerRegistry.playerExists playerName state.PlayerRegistry)
            then [PlayerRegistered { Id = createPlayerId (); Name = playerName; Date=System.DateTimeOffset.Now }]    
            else [PlayerAlreadyRegistered name]
    | _ -> boardNotOpen "Player can only be registered when board is open"

let handle (c: Command) =
    match c with
    | OpenScoreboard arg -> openScoreboard arg
    | RegisterPlayer p -> registerPlayer p
    

// ----- State change function -----
type State with 
    static member Evolve (s: State) = function
        | PlayerRegistered p -> 
            let reg' = PlayerRegistry.update p.Name p.Id s.PlayerRegistry
            { s with PlayerRegistry = reg' }
        | PlayerAlreadyRegistered _ -> s
