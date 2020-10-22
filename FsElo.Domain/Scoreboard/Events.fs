namespace FsElo.Domain.Scoreboard.Events

type BoardId = BoardId of string


type PlayerId = System.Guid
type PlayerName = PlayerName of string

type ScoreId = System.Guid
type ScoreType = TableTennis
type Score = int * int

type Event =
    | ScoreboardOpened of ScoreboardOpened
    | ScoreboardClosed of ScoreboardClosed
    | PlayerRegistered of PlayerRegistered
    | ScoreEntered of ScoreEntered
    | ScoreWithdrawn of ScoreWithdrawn
    | ScoreFixed of ScoreEntered
    
and ScoreboardOpened = {
      BoardId: BoardId
      Type: ScoreType
      Date: System.DateTimeOffset
}

and ScoreboardClosed = {
      BoardId: BoardId
      Date: System.DateTimeOffset
}

and PlayerRegistered = {
      PlayerId: PlayerId
      Name: PlayerName
      Date: System.DateTimeOffset
}

and ScoreEntered = {
    ScoreId: ScoreId
    Players: PlayerId * PlayerId 
    Score: Score
    Date: System.DateTimeOffset
}

and ScoreWithdrawn = {
    ScoreId: ScoreId
    Date: System.DateTimeOffset
}

// exceptions
type ScoreboardException(msg: string) =
    inherit System.Exception(msg)
type BoardNotOpenException(msg: string) =
    inherit ScoreboardException(msg)
    
type BoardAlreadyOpenedException(msg: string) =
    inherit ScoreboardException(msg)
    
type PlayerAlreadyRegisteredException(msg: string) =
    inherit ScoreboardException(msg)
    
type PlayerNotRegisteredException(msg: string) =
    inherit ScoreboardException(msg)

type ScoreNotFoundException(msg: string) =
    inherit ScoreboardException(msg)
    
type InvalidScoreException(msg: string) =
    inherit ScoreboardException(msg)

type InvalidPlayerNameException(msg: string) =
    inherit ScoreboardException(msg)    
