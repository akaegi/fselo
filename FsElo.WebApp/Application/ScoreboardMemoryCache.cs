using Microsoft.Extensions.Caching.Memory;

namespace FsElo.WebApp.Application
{
    public class ScoreboardMemoryCache
    {
        public ScoreboardMemoryCache(int cacheSizeLimit)
        {
            Cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = cacheSizeLimit
            });
        }

        public IMemoryCache Cache { get; }
    }
}