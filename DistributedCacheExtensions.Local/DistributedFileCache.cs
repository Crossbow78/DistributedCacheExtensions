using DistributedCacheExtensions.Local.Abstraction;
using DistributedCacheExtensions.Local.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DistributedCacheExtensions.Local.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DistributedCacheExtensions.Local
{
    internal class DistributedFileCache : IDistributedCache
    {
        private readonly ILogger _logger;
        private readonly IMetadataHandler _metadataHandler;
        private readonly IStorageHandler _storageHandler;

        public DistributedFileCache(ILogger<DistributedFileCache> logger, IStorageHandler storageHandler, IMetadataHandler metadataHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storageHandler = storageHandler ?? throw new ArgumentNullException(nameof(storageHandler));
            _metadataHandler = metadataHandler ?? throw new ArgumentNullException(nameof(metadataHandler));
        }

        public byte[] Get(string key)
            => GetAsync(key).GetAwaiter().GetResult();

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            var cacheMetadata = await _metadataHandler.Get(key);
            _logger.LogDebug("Reading data for {key}", key);
            var content = await _storageHandler.Load(cacheMetadata.Reference);
            if (content != null)
            {
                await _metadataHandler.Set(cacheMetadata);
                return content;
            }
            return null;
        }

        public void Refresh(string key)
            => RefreshAsync(key).GetAwaiter().GetResult();

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            _logger.LogDebug("Refreshing {key}", key);
            var cacheMetadata = await _metadataHandler.Get(key);
            await _metadataHandler.Set(cacheMetadata);
        }

        public void Remove(string key)
            => RemoveAsync(key).GetAwaiter().GetResult();

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            var cacheMetadata = await _metadataHandler.Get(key);
            _logger.LogDebug("Deleting data for {key}", key);
            await _storageHandler.Delete(cacheMetadata.Reference);
            await _metadataHandler.Expire(cacheMetadata);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            => SetAsync(key, value, options).GetAwaiter().GetResult();

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            var cacheMetadata = await _metadataHandler.Get(key);
            _logger.LogDebug("Writing data for {key}", cacheMetadata.Key);
            await _storageHandler.Save(cacheMetadata.Reference, value);
            await SetExpiration(cacheMetadata, options);
        }

        private async Task SetExpiration(ICacheMetadata cacheMetadata, DistributedCacheEntryOptions options)
        {
            cacheMetadata.AbsoluteExpiration = options.GetAbsoluteExpiration();
            cacheMetadata.SlidingExpiration = options.SlidingExpiration;
            await _metadataHandler.Set(cacheMetadata);
        }
    }
}
