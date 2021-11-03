using DistributedCacheExtensions.Local.Abstraction;
using DistributedCacheExtensions.Local.Internal;
using System.Collections.Concurrent;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;

namespace DistributedCacheExtensions.Local.Tests.Mocks
{
    internal class MockMetadataHandler : IMetadataHandler
    {
        private readonly ConcurrentDictionary<string, ICacheMetadata> _cache = new();

        public ConcurrentDictionary<string, ICacheMetadata> CachedMetadata => _cache;

        public Task<ICacheMetadata> Get(string key) => Task.FromResult(_cache.GetOrAdd(key, new CacheMetadata
        {
            Key = key,
            Reference = key,
            AbsoluteExpiration = null,
            SlidingExpiration = null,
            SlidingExpirationMoment = null,
        }));

        public Task Set(ICacheMetadata cacheMetadata) => Task.FromResult(_cache[cacheMetadata.Key] = cacheMetadata);

        public Task Expire(ICacheMetadata cacheMetadata) => Task.FromResult(_cache.TryRemove(cacheMetadata.Key, out _));
    }
}
