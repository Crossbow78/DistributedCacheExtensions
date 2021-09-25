using DistributedCacheExtensions.Abstraction;
using DistributedCacheExtensions.Internal;
using System.Collections.Concurrent;
using System.IO.Abstractions.TestingHelpers;

namespace DistributedCacheExtensions.Tests.Mocks
{
    internal class MockMetadataHandler : IMetadataHandler
    {
        public MockMetadataHandler(IMockFileDataAccessor mockFileDataAccessor)
        {
            _mockFileDataAccessor = mockFileDataAccessor;
        }

        private readonly ConcurrentDictionary<string, ICacheMetadata> _cache = new();
        private readonly IMockFileDataAccessor _mockFileDataAccessor;

        public ConcurrentDictionary<string, ICacheMetadata> CachedMetadata => _cache;

        public ICacheMetadata Get(string key) => _cache.GetOrAdd(key, new CacheMetadata
        {
            Key = key,
            FileInfo = new MockFileInfo(_mockFileDataAccessor, key),
            AbsoluteExpiration = null,
            SlidingExpiration = null,
            SlidingExpirationMoment = null,
        });

        public void Set(ICacheMetadata cacheMetadata) => _cache[cacheMetadata.Key] = cacheMetadata;

        public void Expire(ICacheMetadata cacheMetadata) => _cache.TryRemove(cacheMetadata.Key, out _);
    }
}
