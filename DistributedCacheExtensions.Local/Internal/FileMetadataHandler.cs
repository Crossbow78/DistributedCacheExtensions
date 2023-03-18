using DistributedCacheExtensions.Abstractions;
using DistributedCacheExtensions.Abstractions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DistributedCacheExtensions.Local.Internal
{
    internal class FileMetadataHandler : BaseMetadataHandler, IMetadataHandler
    {
        private readonly ILogger _logger;
        private readonly IStorageHandler _storageHandler;

        public FileMetadataHandler(ILoggerFactory loggerFactory, IOptions<DistributedFileCacheOptions> options, IStorageHandler storageHandler, IDateTimeProvider dateTimeProvider)
            : base(loggerFactory, options, dateTimeProvider)
        {
            _logger = loggerFactory?.CreateLogger<FileMetadataHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _storageHandler = storageHandler ?? throw new ArgumentNullException(nameof(storageHandler));
        }

        public override async Task Expire(ICacheMetadata cacheMetadata)
        {
            var metadataReference = GetCacheReference(cacheMetadata.Reference);
            await _storageHandler.Delete(metadataReference);
            await _storageHandler.Delete(cacheMetadata.Reference);
        }

        public override async Task<ICacheMetadata> Get(string key)
        {
            var reference = _dateTimeProvider.Now.UtcDateTime;

            var contentReference = GetStorageReference(Convert.ToBase64String(Encoding.UTF8.GetBytes(key)));
            var metadataReference = GetCacheReference(contentReference);

            var metadataContent = await _storageHandler.Load(metadataReference);

            ExpirationMetadata metadata = null;
            if (metadataContent != null)
            {
                metadata = JsonSerializer.Deserialize<ExpirationMetadata>(metadataContent);
            }

            var cacheMetadata = new CacheMetadata
            {
                Key = key,
                Reference = contentReference,
                AbsoluteExpiration = metadata?.AbsoluteExpiration,
                SlidingExpiration = metadata?.SlidingExpirationMs != null ? TimeSpan.FromMilliseconds(metadata.SlidingExpirationMs.Value) : null,
                SlidingExpirationMoment = metadata?.SlidingExpirationMoment
            };

            await CheckExpiration(cacheMetadata, reference);

            return cacheMetadata;
        }

        public override async Task Set(ICacheMetadata cacheMetadata)
        {
            var referenceTime = _dateTimeProvider.Now.UtcDateTime;

            var metadataReference = GetCacheReference(cacheMetadata.Reference);

            var expirationMetadata = new ExpirationMetadata
            {
                AbsoluteExpiration = cacheMetadata.AbsoluteExpiration,
                SlidingExpirationMs = cacheMetadata.SlidingExpiration?.TotalMilliseconds,
                SlidingExpirationMoment = cacheMetadata.SlidingExpiration.HasValue ? referenceTime.Add(cacheMetadata.SlidingExpiration.Value) : null,
            };

            _logger.LogDebug("Setting absolute expiration for {key} to {expirationDate}", cacheMetadata.Key, cacheMetadata.AbsoluteExpiration);
            _logger.LogDebug("Setting sliding expiration for {key} to {expirationTime}", cacheMetadata.Key, cacheMetadata.SlidingExpiration);

            await _storageHandler.Save(metadataReference, JsonSerializer.SerializeToUtf8Bytes(expirationMetadata));
        }

        private class ExpirationMetadata
        {
            public DateTime? AbsoluteExpiration { get; set; }
            public double? SlidingExpirationMs { get; set; }
            public DateTime? SlidingExpirationMoment { get; set; }
        }
    }
}
