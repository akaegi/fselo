using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eveneum.Documents;
using Eveneum.Serialization;
using FsElo.Domain.Scoreboard.Events;

namespace FsElo.WebApp.Application
{
    /// <remarks>Kept for doc purposes only</remarks>
    [Obsolete]
    public class ScoreboardReadModelUpdater
    {
        private readonly EveneumDocumentSerializer _serializer;

        public ScoreboardReadModelUpdater(EveneumDocumentSerializer serializer)
        {
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

        private Task UpdateReadModelAsync(string streamId, Event @event)
        {
            string boardId = ToBoardId(streamId);
            return Task.CompletedTask;
        }

        private string ToBoardId(string streamId) => streamId;

        private Event DeserializeEvent(EveneumDocument d)
        {
            var eventData = _serializer.DeserializeEvent(d);
            return (Event) eventData.Body;
        }
    }
}