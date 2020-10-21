module FsElo.Domain.Scoreboard.Events

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
      BoardId: BoardId
      PlayerId: PlayerId
      Name: PlayerName
      Date: System.DateTimeOffset
}

and ScoreEntered = {
    BoardId: BoardId
    ScoreId: ScoreId
    Players: PlayerId * PlayerId 
    Score: Score
    Date: System.DateTimeOffset
}

and ScoreWithdrawn = {
    BoardId: BoardId
    ScoreId: ScoreId
    Date: System.DateTimeOffset
}