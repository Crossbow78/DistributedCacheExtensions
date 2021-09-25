using DistributedCacheExtensions.Abstraction;
using DistributedCacheExtensions.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DistributedCacheExtensions.Tests")]

namespace DistributedCacheExtensions
{
    internal class DistributedFileCache : IDistributedCache
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IMetadataHandler _metadataHandler;

        public DistributedFileCache(ILogger<DistributedFileCache> logger, IMetadataHandler metadataHandler)
            : this(logger, metadataHandler, new FileSystem())
        {
        }

        public DistributedFileCache(ILogger<DistributedFileCache> logger, IMetadataHandler metadataHandler, IFileSystem fileSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _metadataHandler = metadataHandler ?? throw new ArgumentNullException(nameof(metadataHandler));
        }

        public byte[] Get(string key)
        {
            var cacheMetadata = _metadataHandler.Get(key);
            _logger.LogDebug("Reading data for {key}", key);
            if (cacheMetadata.FileInfo.Exists)
            {
                var content = _fileSystem.File.ReadAllBytes(cacheMetadata.FileInfo.FullName);
                _metadataHandler.Set(cacheMetadata);
                return content;
            }
            return null;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            var cacheMetadata = _metadataHandler.Get(key);
            _logger.LogDebug("Reading data for {key}", key);
            if (cacheMetadata.FileInfo.Exists)
            {
                var content = await _fileSystem.File.ReadAllBytesAsync(cacheMetadata.FileInfo.FullName, token);
                _metadataHandler.Set(cacheMetadata);
                return content;
            }
            return null;
        }

        public void Refresh(string key)
        {
            _logger.LogDebug("Refreshing {key}", key);
            var cacheMetadata = _metadataHandler.Get(key);
            _metadataHandler.Set(cacheMetadata);
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            _logger.LogDebug("Refreshing {key}", key);
            var cacheMetadata = _metadataHandler.Get(key);
            _metadataHandler.Set(cacheMetadata);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            var cacheMetadata = _metadataHandler.Get(key);
            if (cacheMetadata.FileInfo.Exists)
            {
                _logger.LogDebug("Deleting data for {key}", key);
                _metadataHandler.Expire(cacheMetadata);
            }
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var cacheMetadata = _metadataHandler.Get(key);
            _logger.LogDebug("Writing data for {key}", cacheMetadata.Key);
            _fileSystem.File.WriteAllBytes(cacheMetadata.FileInfo.FullName, value);
            cacheMetadata.FileInfo.Refresh();

            SetExpiration(cacheMetadata, options);
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            var cacheMetadata = _metadataHandler.Get(key);
            _logger.LogDebug("Writing data for {key}", cacheMetadata.Key);
            await _fileSystem.File.WriteAllBytesAsync(cacheMetadata.FileInfo.FullName, value, token);
            cacheMetadata.FileInfo.Refresh();

            SetExpiration(cacheMetadata, options);
        }

        private void SetExpiration(ICacheMetadata cacheMetadata, DistributedCacheEntryOptions options)
        {
            cacheMetadata.AbsoluteExpiration = options.GetAbsoluteExpiration();
            cacheMetadata.SlidingExpiration = options.SlidingExpiration;
            _metadataHandler.Set(cacheMetadata);
        }
    }
}
