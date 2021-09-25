using DistributedCacheExtensions.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;

namespace DistributedCacheExtensions.Internal
{
    internal class FileMetadataHandler : BaseMetadataHandler, IMetadataHandler
    {
        private readonly ILogger _logger;
        public FileMetadataHandler(ILoggerFactory loggerFactory, IOptions<DistributedFileCacheOptions> options)
            : this(loggerFactory, options, new DateTimeProvider(), new FileSystem())
        {
        }

        public FileMetadataHandler(ILoggerFactory loggerFactory, IOptions<DistributedFileCacheOptions> options, IDateTimeProvider dateTimeProvider, IFileSystem fileSystem)
            : base(loggerFactory, options, dateTimeProvider, fileSystem)
        {
            _logger = loggerFactory?.CreateLogger<FileMetadataHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public override void Expire(ICacheMetadata cacheMetadata)
        {
            var metadataFileInfo = _fileSystem.FileInfo.FromFileName(cacheMetadata.FileInfo.FullName + ".metadata");
            metadataFileInfo.Delete();
            metadataFileInfo.Refresh();
            cacheMetadata.FileInfo.Delete();
            cacheMetadata.FileInfo.Refresh();
        }

        public override ICacheMetadata Get(string key)
        {
            var reference = _dateTimeProvider.Now.UtcDateTime;

            var fileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));

            var contentFile = GetCacheFile(fileName);
            var metadataFile = GetCacheFile(fileName + ".metadata");

            var contentFileInfo = _fileSystem.FileInfo.FromFileName(contentFile);
            var metadataFileInfo = _fileSystem.FileInfo.FromFileName(metadataFile);

            ExpirationMetadata metadata = null;
            if (metadataFileInfo.Exists)
            {
                metadata = JsonSerializer.Deserialize<ExpirationMetadata>(_fileSystem.File.ReadAllText(metadataFileInfo.FullName));
            }

            var cacheMetadata = new CacheMetadata
            {
                Key = key,
                FileInfo = contentFileInfo,
                AbsoluteExpiration = metadata?.AbsoluteExpiration,
                SlidingExpiration = metadata?.SlidingExpirationMs != null ? TimeSpan.FromMilliseconds(metadata.SlidingExpirationMs.Value) : null,
                SlidingExpirationMoment = metadata?.SlidingExpirationMoment
            };

            CheckExpiration(cacheMetadata, reference);

            return cacheMetadata;
        }

        public override void Set(ICacheMetadata cacheMetadata)
        {
            var referenceTime = _dateTimeProvider.Now.UtcDateTime;

            var metadataFileInfo = _fileSystem.FileInfo.FromFileName(cacheMetadata.FileInfo.FullName + ".metadata");

            var expirationMetadata = new ExpirationMetadata
            {
                AbsoluteExpiration = cacheMetadata.AbsoluteExpiration,
                SlidingExpirationMs = cacheMetadata.SlidingExpiration?.TotalMilliseconds,
                SlidingExpirationMoment = cacheMetadata.SlidingExpiration.HasValue ? referenceTime.Add(cacheMetadata.SlidingExpiration.Value) : null,
            };

            _logger.LogDebug("Setting absolute expiration for {key} to {expirationDate}", cacheMetadata.Key, cacheMetadata.AbsoluteExpiration);
            _logger.LogDebug("Setting sliding expiration for {key} to {expirationTime}", cacheMetadata.Key, cacheMetadata.SlidingExpiration);

            _fileSystem.File.WriteAllText(metadataFileInfo.FullName, JsonSerializer.Serialize(expirationMetadata));
        }

        private class ExpirationMetadata
        {
            public DateTime? AbsoluteExpiration { get; set; }
            public double? SlidingExpirationMs { get; set; }
            public DateTime? SlidingExpirationMoment { get; set; }
        }
    }
}
