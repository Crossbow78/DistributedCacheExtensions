using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DistrubutedCacheExtensions.AzureStorage
{
    public class Class1
    {
        [Fact]
        public async Task BlobContainerClient_Test()
        {
            var blobName = Guid.NewGuid().ToString();

            Uri blobUri = new Uri("https://sasebpoc.blob.core.windows.net/ebx");
            TokenCredential tokenCredential = new DefaultAzureCredential();

            var client = new BlobContainerClient(blobUri, tokenCredential);

            var blobResponse = await client.UploadBlobAsync(blobName, BinaryData.FromString("mydata"));
            blobResponse.Value.ETag.ToString().Should().HaveLength(17);

            var delete = await client.DeleteBlobIfExistsAsync(blobName);
            delete.Value.Should().BeTrue();
        }

        [Fact]
        public async Task BlobClient_Test()
        {
            Uri blobUri = new Uri("https://sasebpoc.blob.core.windows.net/ebx/myblob");
            TokenCredential tokenCredential = new DefaultAzureCredential();

            var client = new BlobClient(blobUri, tokenCredential);
            var blobResponse = await client.UploadAsync(BinaryData.FromString("mydata"));
            blobResponse.Value.ETag.ToString().Should().HaveLength(17);

            var blobResponse2 = await client.DownloadContentAsync();
            blobResponse2.Value.Details.ETag.ToString().Should().HaveLength(17);
            blobResponse2.Value.Details.ContentLength.Should().Be(6);
            blobResponse2.Value.Content.ToString().Should().Be("mydata");

            var delete = await client.DeleteIfExistsAsync();
            delete.Value.Should().BeTrue();
        }

    }

}
