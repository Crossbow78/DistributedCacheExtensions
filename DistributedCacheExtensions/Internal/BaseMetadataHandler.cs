using DistributedCacheExtensions.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.IO.Abstractions;

namespace DistributedCacheExtensions.Internal
{
    internal abstract class BaseMetadataHandler : IMetadataHandler
    {
        private readonly ILogger _logger;

        protected readonly IFileSystem _fileSystem;
        protected readonly IDateTimeProvider _dateTimeProvider;
        protected readonly DistributedFileCacheOptions _options;

        public BaseMetadataHandler(ILoggerFactory loggerFactory, IOptions<DistributedFileCacheOptions> options, IDateTimeProvider dateTimeProvider, IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _logger = loggerFactory?.CreateLogger<BaseMetadataHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected void CheckExpiration(ICacheMetadata metadata, DateTime referenceUtc)
        {
            _logger.LogDebug($"Exists: {metadata.FileInfo.Exists}" + Environment.NewLine +
                $"Absolute: {metadata.AbsoluteExpiration}" + Environment.NewLine +
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
                Expire(metadata);
            }
        }

        protected string GetCacheFile(string filename) => Path.GetFullPath(Path.Combine(_options.Path, filename));

        public abstract void Set(ICacheMetadata cacheMetadata); 

        public abstract ICacheMetadata Get(string key);

        public abstract void Expire(ICacheMetadata cacheMetadata);
    }
}
