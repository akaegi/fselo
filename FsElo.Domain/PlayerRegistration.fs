module FsElo.Domain.PlayerRegistration


type PlayerId = System.Guid


let createPlayerId () = System.Guid.NewGuid ()


type ValidPlayerName = ValidPlayerName of string


let validatePlayerName (pn: string) =
    if (isNull pn) || pn.Length < 3 
        then invalidArg "playerName" "Player name must be at least 3 characters"
        else ValidPlayerName pn


type PlayerRegistry = Map<ValidPlayerName, PlayerId>


module PlayerRegistry = 
    let create = Map.empty<ValidPlayerName, PlayerId>

    let playerExists (name: ValidPlayerName) (reg: PlayerRegistry) =
        reg.ContainsKey name

    let update (name: ValidPlayerName) (playerId: PlayerId) (reg: PlayerRegistry) =
        reg.Add (name, playerId) 
