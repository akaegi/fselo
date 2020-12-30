using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using FsElo.Domain.Scoreboard.Events;

namespace FsElo.WebApp.Application
{
    public class ScoreListReadModel
    {
        private readonly IDictionary<Guid, string> _playerNames = new Dictionary<Guid, string>();
        private readonly IDictionary<string, Guid> _playerIds = new Dictionary<string, Guid>();
        
        /// <summary>Score list ordered by date</summary>
        private readonly List<ScoreEntered> _scores = new List<ScoreEntered>();

        private static readonly long _sizeOfScoreEntered =
            16 /* score id */
            + 2 * 16 /* player ids */
            + 2 * 8 /* score */
            + 16 /* Date */;

        public long Size { get; private set; } = 0;
        
        /// <summary>
        /// Returns all scores for the given player (optionally against the given adversary only)
        /// ordered by date descending.
        /// </summary>
        public IEnumerable<ScoreListEntry> ScoreList(string player, string adversary = null)
        {
            if (!_playerIds.TryGetValue(player, out Guid playerId))
            {
                return Enumerable.Empty<ScoreListEntry>();
            }

            var q = _scores.Where(e => e.Players.Item1 == playerId || e.Players.Item2 == playerId);

            if (adversary != null && _playerIds.TryGetValue(adversary, out Guid adversaryId))
            {
                q = q.Where(e => e.Players.Item1 == adversaryId || e.Players.Item2 == adversaryId);
            }

            return q
                .OrderByDescending(e => e.Date)
                .Select(CreateScoreListEntry);
        }

        private ScoreListEntry CreateScoreListEntry(ScoreEntered e)
        {
            _playerNames.TryGetValue(e.Players.Item1, out string name1);
            _playerNames.TryGetValue(e.Players.Item2, out string name2);

            return new ScoreListEntry
            {
                Date = e.Date,
                Score = $"{e.Score.Item1}:{e.Score.Item2}",
                Player1 = name1 ?? "unknown",
                Player2 = name2 ?? "unknown",
            };
        }

        public void Apply(Event @event)
        {
            switch (@event)
            {
                case Event.PlayerRegistered p:
                    SetPlayerName(p.Item);
                    break;
                case Event.ScoreEntered s:
                    EnterScore(s.Item);
                    Size += SizeOfEntry(s.Item);
                    break;
                case Event.ScoreFixed s:
                    FixScore(s.Item);
                    break;
                case Event.ScoreWithdrawn s:
                    var removed = RemoveScore(s.Item);
                    if (removed != null)
                    {
                        Size -= SizeOfEntry(removed);
                    }
                    break;
            }
        }

        private long SizeOfEntry(ScoreEntered s) => _sizeOfScoreEntered;

        private void SetPlayerName(PlayerRegistered p)
        {
            _playerNames[p.PlayerId] = p.Name.Item;
            _playerIds[p.Name.Item] = p.PlayerId;
        }
        
        private void EnterScore(ScoreEntered s)
        {
            int index = FindInsertIndex(s.Date);
            _scores.Insert(index, s);
        }

        private void FixScore(ScoreEntered s)
        {
            int index = GetScoreIndex(s.ScoreId, s.Date);
            _scores[index] = s;
        }

        private ScoreEntered RemoveScore(ScoreWithdrawn s)
        {
            int index = GetScoreIndex(s.ScoreId, s.Date);
            ScoreEntered removedItem = index >= 0 ? _scores[index] : null;
            _scores.RemoveAt(index);
            return removedItem;
        }
        
        private int GetScoreIndex(Guid scoreId, DateTimeOffset scoreDate)
        {
            int index = FindInsertIndex(scoreDate);

            // find score with id
            var existing = _scores
                .Select((e, ix) => new {Entry = e, Ix = ix})
                .Skip(index)
                .TakeWhile(x => x.Entry.Date == scoreDate)
                .FirstOrDefault(x => x.Entry.ScoreId == scoreId);

            if (existing == null)
                throw new InvalidOperationException($"Expecting score list to contain an entry with id {scoreId}" +
                                                    $"and date {scoreDate}");
            
            return existing.Ix;
        }
        
        /// <summary>
        /// Gets the index in the sorted score list for the given date:
        /// I.e. ix = FindInsertIndex(dt) => forall j &lt; ix: _scores[j].Date &lt; dt
        /// </summary>
        private int FindInsertIndex(DateTimeOffset dt)
        {
            int l = 0;
            int r = _scores.Count;
            while (l < r)
            {
                int m = (l + r) / 2;
                if (_scores[m].Date < dt)
                {
                    l = m + 1;
                }
                else
                {
                    r = m;
                }
            }

            return l;
        }
    }

    public class ScoreListEntry
    {
        public DateTimeOffset Date { get; set; }
        
        public string Player1 { get; set; }
        
        public string Player2 { get; set; }
        
        public string Score { get; set; }
    }
}