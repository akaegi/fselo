## Base functionality

### Commands (on Scoreboard aggregate)

openScoreboard boardId
    requires `boardId` to be alphanumeric [0-9a-Z_]+
    requires board to not yet exist


closeScoreboard boardId
    requires board to be open


registerPlayer boardId playerName
    requires playerName to be unique in board 
    requires playerName to have length >= 3 chars
    => PlayerRegistered (with playerId)


enterScore boardId flags player1 player2 result [date]
    requires board to be open
    requires player1 and player2 to exist in board
    requires (isTableTennisScore result)
    requires no score between player1 and player2 within the last hour* (unless flags.allowDuplicateEntry)
    * should prevent most unintentional "duplicate" score entries
    => ScoreEntered (with scoreId)


withdrawScore boardId scoreId
    ScoreWithdrawn

fixScore boardId scoreId result [date]
    requires board to be open
    requires scoreId to refer to the last score between two players
    => ScoreFixed


### Queries

ranking boardId
    aliases: leaderboard, scoreboard
    requires board to exist

    ----------------------------------------------------
    | Rk | Player               | Elo  | +/- last week | 
    ----------------------------------------------------
    |  1 | christoph.heiniger   |   80 |             0 |
    |  2 | matthias.heinzmann   |   37 |            +2 |
    |  3 | leonard.authier      |   38 |             0 |
    |  4 | akaegi               |   37 |            +1 |
    ----------------------------------------------------


forecast boardId player1 player2
    TODO


scores boardId player [otherPlayer]
    * ordered by score date descending
    * if otherPlayer given, filter scores against otherPlayer only

    ----------------------------------------------------
    | Date              | Player               | Score |
    ----------------------------------------------------
    | 2019-10-22 10:12  | leonard.authier      | 1:2   |
    ----------------------------------------------------
    

statistic boardId player


### User Frontend

https://xtermjs.org/




## Extension Ideas

- renamePlayer boardId oldName newName

