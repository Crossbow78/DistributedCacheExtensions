using DistributedCacheExtensions.Abstractions;
using DistributedCacheExtensions.Abstractions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;

namespace DistributedCacheExtensions.Local.Internal
{
    internal class TimestampMetadataHandler : BaseMetadataHandler, IMetadataHandler
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public TimestampMetadataHandler(ILoggerFactory loggerFactory, IOptions<DistributedFileCacheOptions> options, IDateTimeProvider dateTimeProvider, IFileSystem fileSystem)
            : base(loggerFactory, options, dateTimeProvider)
        {
            _logger = loggerFactory?.CreateLogger<TimestampMetadataHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            VerifyPermissions();
        }

        public override async Task<ICacheMetadata> Get(string key)
        {
            var reference = _dateTimeProvider.Now.UtcDateTime;

            var fileName = GetStorageReference(Convert.ToBase64String(Encoding.UTF8.GetBytes(key)));

            // Note: relies on disabled NTFS behavior:
            // fsutil behavior set disablelastaccess 0x3

            var fileInfo = _fileSystem.FileInfo.FromFileName(fileName);
            var metadata = new CacheMetadata
            {
                Key = key,
                Reference = fileName,
                AbsoluteExpiration = fileInfo.LastWriteTimeUtc > fileInfo.CreationTimeUtc ? fileInfo.LastWriteTimeUtc : null,
                SlidingExpiration = fileInfo.LastAccessTimeUtc > fileInfo.CreationTimeUtc ? fileInfo.LastAccessTimeUtc - fileInfo.CreationTimeUtc : null,
                SlidingExpirationMoment = fileInfo.LastAccessTimeUtc > fileInfo.CreationTimeUtc ? fileInfo.LastAccessTimeUtc : null,
            };

            await CheckExpiration(metadata, reference);

            return metadata;
        }

        public override Task Expire(ICacheMetadata cacheMetadata)
        {
            var fileInfo = _fileSystem.FileInfo.FromFileName(cacheMetadata.Reference);
            fileInfo.Delete();
            return Task.CompletedTask;
        }

        public override Task Set(ICacheMetadata cacheMetadata)
        {
            var referenceTime = _dateTimeProvider.Now.UtcDateTime;
            var absoluteExpiration = cacheMetadata.AbsoluteExpiration ?? referenceTime.AddSeconds(-1);
            var slidingExpiration = referenceTime.Add(cacheMetadata.SlidingExpiration ?? TimeSpan.FromSeconds(-1));

            var fileInfo = _fileSystem.FileInfo.FromFileName(cacheMetadata.Reference);
            _fileSystem.File.SetCreationTimeUtc(fileInfo.FullName, referenceTime);
            _fileSystem.File.SetLastWriteTimeUtc(fileInfo.FullName, absoluteExpiration);
            _fileSystem.File.SetLastAccessTimeUtc(fileInfo.FullName, slidingExpiration);

            _logger.LogDebug("Setting absolute expiration for {key} to {expirationDate}", cacheMetadata.Key, absoluteExpiration);
            _logger.LogDebug("Setting sliding expiration for {key} to {expirationTime}", cacheMetadata.Key, cacheMetadata.SlidingExpiration);
            return Task.CompletedTask;
        }

        private void VerifyPermissions()
        {
            try
            {
                var testFile = GetStorageReference("testfile");
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
