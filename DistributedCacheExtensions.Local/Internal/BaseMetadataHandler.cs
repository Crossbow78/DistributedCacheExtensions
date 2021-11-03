using DistributedCacheExtensions.Local.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DistributedCacheExtensions.Local.Internal
{
    internal abstract class BaseMetadataHandler : IMetadataHandler
    {
        private readonly ILogger _logger;

        protected readonly IDateTimeProvider _dateTimeProvider;
        protected readonly DistributedFileCacheOptions _options;

        public BaseMetadataHandler(ILoggerFactory loggerFactory, IOptions<DistributedFileCacheOptions> options, IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _logger = loggerFactory?.CreateLogger<BaseMetadataHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected async Task CheckExpiration(ICacheMetadata metadata, DateTime referenceUtc)
        {
            _logger.LogDebug($"Absolute: {metadata.AbsoluteExpiration}" + Environment.NewLine +
                $"Sliding: {metadata.SlidingExpiration} ({metadata.SlidingExpirationMoment})");

            bool expired = false;
            if (metadata.AbsoluteExpiration.HasValue && referenceUtc > metadata.AbsoluteExpiration)
            {
                _logger.LogDebug("Expiring {key} by absolute date", metadata.Key);
                expired = true;
            }

            if (metadata.SlidingExpirationMoment.HasValue && referenceUtc > metadata.SlidingExpirationMoment)
            {
                _logger.LogDebug("Expiring {key} by sliding date", metadata.Key);
                expired = true;
            }

            if (expired)
            {
                await Expire(metadata);
            }
        }

        protected virtual string GetStorageReference(string filename) => Path.GetFullPath(Path.Combine(_options.Path, filename));

        protected virtual string GetCacheReference(string reference) => $"{reference}.metadata";

        public abstract Task Set(ICacheMetadata cacheMetadata); 

        public abstract Task<ICacheMetadata> Get(string key);

        public abstract Task Expire(ICacheMetadata cacheMetadata);
    }
}
