## Base functionality

### Commands (on Scoreboard aggregate)

openScoreboard boardId boardType
    requires boardId to be alphanumeric of the form [0-9a-zA-Z][0-9a-zA-Z-_]{2,}
    requires board to not yet exist
    requires boardType to be 'tt'
    => ScoreboardOpened


closeScoreboard boardId
    requires board to be open
    => ScoreboardClosed


registerPlayer boardId playerName
    requires playerName to be unique in board 
    requires playerName to have length >= 3 chars
    => PlayerRegistered (with playerId)


enterScore boardId flags player1 player2 result date
    requires board to be open
    requires player1 and player2 to be registered already
    [tt] requires (isTableTennisScore result)
    [tt] requires no score between player1 and player2 within the last hour* (unless flags.allowDuplicateEntry)
    [tt] score must be of the form `n:m` where n > 0, m > 0
    => ScoreEntered (with scoreId)
    * should prevent most unintentional "duplicate" score entries


withdrawScore boardId scoreId
    => ScoreWithdrawn


fixScore boardId scoreId result date
    requires board to be open
    => ScoreFixed


### Queries

tt-elo boardId
    aliases: tt-board, tt-leaderboard, tt-scoreboard
    requires board to exist
    requires board to be of type 'tt'

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
    | Scores for `player`                              |
    ----------------------------------------------------
    | Date              | Other player         | Score |
    ----------------------------------------------------
    | 2019-10-22 10:12  | leonard.authier      | 1:2   |
    ----------------------------------------------------
    

statistic boardId player
    ???


### User Frontend

https://xtermjs.org/




## Extension Ideas

- renamePlayer boardId oldName newName
