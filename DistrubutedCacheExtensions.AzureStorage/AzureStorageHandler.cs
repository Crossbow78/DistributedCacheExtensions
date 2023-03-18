using Azure.Identity;
using Azure.Storage.Blobs;
using DistributedCacheExtensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace DistrubutedCacheExtensions.AzureStorage
{
    public class AzureStorageConfiguration
    {
        public Uri ContainerUrl { get; set; } = new("https://sasebpoc.blob.core.windows.net/ebx");
    }

    internal class AzureStorageHandler : IStorageHandler
    {
        private readonly AzureStorageConfiguration _options;
        private readonly ILogger _logger;

        public AzureStorageHandler(IOptions<AzureStorageConfiguration> options, ILogger<AzureStorageHandler> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        private BlobContainerClient _client;
        private BlobContainerClient Client => _client ??= Connect();

        private BlobContainerClient Connect()
        {
            var tokenCredential = new DefaultAzureCredential();
            var client = new BlobContainerClient(_options.ContainerUrl, tokenCredential);
            client.CreateIfNotExists();
            return client;
        }

        public async Task Delete(string reference)
        {
            _logger.LogDebug("Deleting all content from blob '{BlobName}'", reference);
            await Client.DeleteBlobIfExistsAsync(reference, Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);
        }

        public async Task<byte[]> Load(string reference)
        {
            _logger.LogDebug("Reading byte content from blob '{BlobName}'", reference);
            var blobClient = Client.GetBlobClient(reference);
            var blob = await blobClient.DownloadContentAsync();
            return blob.Value.Content.ToArray();
        }

        public async Task Save(string reference, byte[] value)
        {
            _logger.LogDebug("Writing byte content to blob '{BlobName}'", reference);
            await Client.UploadBlobAsync(reference, BinaryData.FromBytes(value));
        }
    }
}
