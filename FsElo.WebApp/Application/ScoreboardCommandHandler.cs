using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Eveneum;
using FsElo.Domain;
using FsElo.Domain.Scoreboard;
using FsElo.Domain.Scoreboard.Events;

namespace FsElo.WebApp.Application
{
    public class ScoreboardCommandHandler
    {
        private readonly IReadStream _streamReader;
        private readonly IWriteToStream _streamWriter;

        public ScoreboardCommandHandler(IReadStream streamReader, IWriteToStream streamWriter)
        {
            _streamReader = streamReader;
            _streamWriter = streamWriter;
        }

        public async Task<ScoreboardCommandHandlerResult> HandleAsync(
            UserInfo userInfo, string boardId, string commandInput)
        {
            Scoreboard.Command command = ParseCommand(commandInput);
            if (command is Scoreboard.Command.OpenScoreboard o)
            {
                boardId = o.Item.BoardId;
            }

            string streamId = ToStreamId(boardId);
            var (version, events) = await ReadEventsAsync(streamId);
            var state = Evolve(events);
            var newEvents = Scoreboard.handle(command).Invoke(state);
            await WriteStreamAsync(streamId, version, newEvents, userInfo.User);

            string message = BuildResultMessage(newEvents);
            return new ScoreboardCommandHandlerResult(streamId, message);
        }

        public static string ToStreamId(string boardId) => boardId;

        private string BuildResultMessage(IEnumerable<Event> events)
        {
            string describe(Event e) =>
                e switch
                {
                    Event.ScoreboardOpened _ => "Scoreboard opened",
                    Event.ScoreboardClosed _ => "Scoreboard closed",
                    Event.PlayerRegistered _ => "Player registered",
                    Event.ScoreEntered _ => "Score entered",
                    Event.ScoreWithdrawn _ => "Score withdrawn",
                    Event.ScoreFixed _ => "Score fixed",
                    _ => "Unknown event",
                };

            var lines = events.Select(describe);
            return String.Join("\n", lines);
        }

        private Scoreboard.State Evolve(IEnumerable<Event> events)
        {
            var state = Scoreboard.State.Initial;
            foreach (var @event in events)
            {
                var newState = Scoreboard.State.Evolve(state, @event);
                state = newState;
            }

            return state;
        }

        private async Task<ValueTuple<ulong?, IEnumerable<Event>>> ReadEventsAsync(string streamId)
        {
            var readResult = await _streamReader.ReadStream(streamId);

            ulong? version = readResult.Stream?.Version;
            var events = (readResult.Stream?.Events ?? new EventData[0])
                .Select(ed => ed.Body)
                .Cast<Event>();

            return (version, events);
        }

        
        private async Task WriteStreamAsync(string streamId, ulong? expectedVersion, 
            IEnumerable<Event> newEvents, string user)
        {
            var eventDatas = newEvents
                .Select((e, ix) => new EventData
                {
                    StreamId = streamId,
                    Body = e,
                    Metadata = new
                    {
                        User = user
                    },
                    Version = (expectedVersion ?? 0) + (ulong) ix
                })
                .ToArray();
            await _streamWriter.WriteToStream(streamId, eventDatas, expectedVersion);
        }
        
        private static Scoreboard.Command ParseCommand(string commandInput)
        {
            var result =
                ScoreboardInputParser.parseScoreboardCommand(CultureInfo.CurrentCulture, TimeSpan.FromHours(2), commandInput);
            if (result.IsError)
            {
                throw new ScoreboardException(result.ErrorValue);
            }

            return result.ResultValue;
        }
    }

    public class ScoreboardCommandHandlerResult
    {
        public ScoreboardCommandHandlerResult(string streamId, string message)
        {
            StreamId = streamId;
            Message = message;
        }

        public string StreamId { get; }
        
        public string Message { get; }
        
    }
}