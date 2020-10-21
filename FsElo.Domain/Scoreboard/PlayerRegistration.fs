module FsElo.Domain.Scoreboard.PlayerRegistration

open FsElo.Domain.Scoreboard.Events


let createPlayerId (): PlayerId = System.Guid.NewGuid()

let validatePlayerName (pn: string): PlayerName =
    if isNull pn then nullArg "Argument must not be null"
    elif pn.Length < 3 then invalidArg "playerName" "Player name must be at least 3 characters"
    else (PlayerName pn)


type PlayerRegistry = Map<PlayerName, PlayerId>


module PlayerRegistry = 
    let create = Map.empty<PlayerName, PlayerId>

    let playerExists (name: PlayerName) (reg: PlayerRegistry) =
        reg.ContainsKey name

    let update (name: PlayerName) (playerId: PlayerId) (reg: PlayerRegistry) =
        reg.Add (name, playerId) 

    let lookup (name: string) (reg: PlayerRegistry) =
        reg.TryFind (PlayerName name)