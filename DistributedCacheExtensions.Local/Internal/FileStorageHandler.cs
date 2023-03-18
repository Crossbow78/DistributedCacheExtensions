using DistributedCacheExtensions.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace DistributedCacheExtensions.Local.Internal
{
    public class FileStorageHandler : IStorageHandler
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public FileStorageHandler(ILogger<FileStorageHandler> logger, IFileSystem fileSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public Task Delete(string reference)
        {
            var fileInfo = _fileSystem.FileInfo.FromFileName(reference);
            _logger.LogDebug($"Deleting file '{fileInfo.FullName}'");
            fileInfo.Delete();
            return Task.CompletedTask;
        }

        public async Task<byte[]> Load(string reference)
        {
            var fileInfo = _fileSystem.FileInfo.FromFileName(reference);
            if (fileInfo.Exists)
            {
                _logger.LogDebug($"Reading byte content from file '{fileInfo.FullName}'");
                return await _fileSystem.File.ReadAllBytesAsync(fileInfo.FullName);
            }
            else
            {
                _logger.LogDebug($"Unable to find file '{fileInfo.FullName}'");
                return null;
            }
        }

        public async Task Save(string reference, byte[] value)
        {
            var fileInfo = _fileSystem.FileInfo.FromFileName(reference);
            _logger.LogDebug($"Writing byte content to file '{fileInfo.FullName}'");
            await _fileSystem.File.WriteAllBytesAsync(fileInfo.FullName, value);
        }
    }
}
