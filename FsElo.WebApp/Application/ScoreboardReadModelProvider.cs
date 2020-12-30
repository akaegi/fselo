using System.Linq;
using System.Threading.Tasks;
using Eveneum;
using FsElo.Domain.Scoreboard.Events;
using Microsoft.Extensions.Caching.Memory;

namespace FsElo.WebApp.Application
{
    public class ScoreboardReadModelProvider
    {
        private readonly IReadStream _streamReader;
        private readonly IMemoryCache _cache;

        public ScoreboardReadModelProvider(IReadStream streamReader, ScoreboardMemoryCache cache)
        {
            _streamReader = streamReader;
            _cache = cache.Cache;
        }
        
        public async Task<ScoreListReadModel> ReadModelAsync(string boardId)
        {
            var cacheEntryValue = _cache.Get<CacheEntryValue>(boardId);
            if (cacheEntryValue == null)
            {
                var readResult = await _streamReader.ReadStream(ScoreboardCommandHandler.ToStreamId(boardId));
                if (readResult.Stream.HasValue)
                {
                    cacheEntryValue = CacheEntryValue.Create();
                    UpdateCacheEntry(boardId, cacheEntryValue, readResult.Stream.Value);
                    return cacheEntryValue.ReadModel;
                }
                else
                {
                    return new ScoreListReadModel(); // return empty read model in case scoreboard does not yet exist...
                }
            }
            else
            {
                var readResult = await _streamReader.ReadStreamFromVersion(
                    ScoreboardCommandHandler.ToStreamId(boardId), cacheEntryValue.Version);
                if (readResult.Stream.HasValue)
                {
                    UpdateCacheEntry(boardId, cacheEntryValue, readResult.Stream.Value);
                    return cacheEntryValue.ReadModel;
                }
                else
                {
                    _cache.Remove(boardId);
                    return new ScoreListReadModel(); // return empty in case scoreboard does not exist anymore...
                }
            }

        }

        private void UpdateCacheEntry(string key, CacheEntryValue value, Stream readStream)
        {
            var events = readStream.Events
                .Select(ed => ed.Body)
                .Cast<Event>();

            if (events.Any())
            {
                foreach (var @event in events)
                {
                    value.ReadModel.Apply(@event);
                }
            
                value.Version = readStream.Version;
                _cache.Set(key, value, new MemoryCacheEntryOptions {Size = value.ReadModel.Size});
            }
        }
        
        class CacheEntryValue
        {
            public ulong Version { get; set; }

            public ScoreListReadModel ReadModel { get; } = new ScoreListReadModel();

            public long Size => sizeof(ulong) + ReadModel.Size;

            public static CacheEntryValue Create()
            {
                return new CacheEntryValue();
            }
        }
    }
}