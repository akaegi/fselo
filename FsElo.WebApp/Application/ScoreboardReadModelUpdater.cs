using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eveneum.Documents;
using Eveneum.Serialization;
using FsElo.Domain.Scoreboard.Events;

namespace FsElo.WebApp.Application
{
    // Scores:  | BoardId | ScoreId | Player 1 | Score 1 | Player 2 | Score 2 | Date |
    // Players: | BoardId | Player id | Player name

    public class ScoreboardReadModelUpdater
    {
        private readonly EveneumDocumentSerializer _serializer;
        private readonly ScoreboardEntryRepository _score;
        private readonly PlayerRepository _players;

        public ScoreboardReadModelUpdater(EveneumDocumentSerializer serializer, ScoreboardEntryRepository score,
            PlayerRepository players)
        {
            _players = players;
            _score = score;
            _serializer = serializer;
        }
        
        public async Task HandleChangesAsync(IReadOnlyCollection<EveneumDocument> changes, CancellationToken ct)
        {
            var documents = changes
                .Where(d => d.DocumentType == DocumentType.Event);

            foreach (EveneumDocument d in documents)
            {
                Event @event = DeserializeEvent(d);
                await UpdateReadModelAsync(d.StreamId, @event);
            }
        }

        private async Task UpdateReadModelAsync(string streamId, Event @event)
        {
            switch (@event)
            {
                case Event.PlayerRegistered p:
                    await UpdatePlayerNameAsync(streamId, p.Item);
                    break;
                case Event.ScoreEntered e:
                    await UpdateScoreAsync(streamId, e.Item);
                    break;
                case Event.ScoreFixed f:
                    await UpdateScoreAsync(streamId, f.Item);
                    break;
                case Event.ScoreWithdrawn w:
                    await RemoveScoreAsync(streamId, w.Item);
                    break;
            }
        }

        private async Task RemoveScoreAsync(string streamId, ScoreWithdrawn e)
        {
            await _score.RemoveScoreAsync(ToBoardId(streamId), e.ScoreId);
        }

        private async Task UpdateScoreAsync(string streamId, ScoreEntered e)
        {
            await _score.UpdateScoreEntryAsync(ToBoardId(streamId), e);
        }

        private string ToBoardId(string streamId) => streamId;

        private async Task UpdatePlayerNameAsync(string streamId, PlayerRegistered e)
        {
            await _players.UpdatePlayerAsync(ToBoardId(streamId), e.PlayerId, e.Name.Item);
        }

        private Event DeserializeEvent(EveneumDocument d)
        {
            var eventData = _serializer.DeserializeEvent(d);
            return (Event) eventData.Body;
        }
    }
}