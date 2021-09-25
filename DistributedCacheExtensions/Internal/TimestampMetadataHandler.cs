using DistributedCacheExtensions.Abstraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO.Abstractions;
using System.Text;

namespace DistributedCacheExtensions.Internal
{
    internal class TimestampMetadataHandler : BaseMetadataHandler, IMetadataHandler
    {
        private readonly ILogger _logger;

        public TimestampMetadataHandler(ILoggerFactory loggerFactory, IOptions<DistributedFileCacheOptions> options)
            : this(loggerFactory, options, new DateTimeProvider(), new FileSystem())
        {
        }

        public TimestampMetadataHandler(ILoggerFactory loggerFactory, IOptions<DistributedFileCacheOptions> options, IDateTimeProvider dateTimeProvider, IFileSystem fileSystem)
            : base(loggerFactory, options, dateTimeProvider, fileSystem)
        {
            _logger = loggerFactory?.CreateLogger<TimestampMetadataHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            VerifyPermissions();
        }

        public override ICacheMetadata Get(string key)
        {
            var reference = _dateTimeProvider.Now.UtcDateTime;

            var fileName = GetCacheFile(Convert.ToBase64String(Encoding.UTF8.GetBytes(key)));

            // Note: relies on disabled NTFS behavior:
            // fsutil behavior set disablelastaccess 0x3

            var fileInfo = _fileSystem.FileInfo.FromFileName(fileName);
            var metadata = new CacheMetadata
            {
                Key = key,
                FileInfo = fileInfo,
                AbsoluteExpiration = fileInfo.LastWriteTimeUtc > fileInfo.CreationTimeUtc ? fileInfo.LastWriteTimeUtc : null,
                SlidingExpiration = fileInfo.LastAccessTimeUtc > fileInfo.CreationTimeUtc ? fileInfo.LastAccessTimeUtc - fileInfo.CreationTimeUtc : null,
                SlidingExpirationMoment = fileInfo.LastAccessTimeUtc > fileInfo.CreationTimeUtc ? fileInfo.LastAccessTimeUtc : null,
            };

            CheckExpiration(metadata, reference);

            return metadata;
        }

        public override void Expire(ICacheMetadata cacheMetadata)
        {
            cacheMetadata.FileInfo.Delete();
            cacheMetadata.FileInfo.Refresh();
        }

        public override void Set(ICacheMetadata cacheMetadata)
        {
            var referenceTime = _dateTimeProvider.Now.UtcDateTime;
            var absoluteExpiration = cacheMetadata.AbsoluteExpiration ?? referenceTime.AddSeconds(-1);
            var slidingExpiration = referenceTime.Add(cacheMetadata.SlidingExpiration ?? TimeSpan.FromSeconds(-1));

            _fileSystem.File.SetCreationTimeUtc(cacheMetadata.FileInfo.FullName, referenceTime);
            _fileSystem.File.SetLastWriteTimeUtc(cacheMetadata.FileInfo.FullName, absoluteExpiration);
            _fileSystem.File.SetLastAccessTimeUtc(cacheMetadata.FileInfo.FullName, slidingExpiration);

            _logger.LogDebug("Setting absolute expiration for {key} to {expirationDate}", cacheMetadata.Key, absoluteExpiration);
            _logger.LogDebug("Setting sliding expiration for {key} to {expirationTime}", cacheMetadata.Key, cacheMetadata.SlidingExpiration);
        }

        private void VerifyPermissions()
        {
            try
            {
                var testFile = GetCacheFile("testfile");
                var testDate = new DateTime(2000, 01, 01, 01, 23, 45, DateTimeKind.Utc);
                var testContent = "TimestampMetadataHandler";
                var file = _fileSystem.FileInfo.FromFileName(testFile);

                _fileSystem.File.WriteAllText(file.FullName, testContent);
                _fileSystem.File.SetLastAccessTimeUtc(file.FullName, testDate);

                var content = _fileSystem.File.ReadAllText(file.FullName);
                if (content != testContent)
                {
                    _logger.LogWarning("Unable to save or load content - cache will be disabled");
                }

                var date = _fileSystem.File.GetLastAccessTimeUtc(file.FullName);
                if (date != testDate)
                {
                    _logger.LogWarning("Unable to control access date - sliding expiration will not function correctly"
                        + Environment.NewLine +
                        "Run the following command to gain control: fsutil behavior set disablelastaccess 0x3");
                }

                file.Delete();
                if (_fileSystem.File.Exists(file.FullName))
                {
                    _logger.LogWarning("Unable to delete file - cache may not expire");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to access cache file");
            }
        }
    }
}
