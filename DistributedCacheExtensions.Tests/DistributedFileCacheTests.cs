using DistributedCacheExtensions.Tests.Mocks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Xunit;

namespace DistributedCacheExtensions.Tests
{
    public class DistributedFileCacheTests
    {
        private readonly DistributedFileCache _sut;
        private readonly MockFileSystem _fileSystem = new();
        private readonly MockMetadataHandler _metadataHandler;
        private readonly ILogger<DistributedFileCache> _logger;

        public DistributedFileCacheTests()
        {
            _logger = NullLogger<DistributedFileCache>.Instance;
            _metadataHandler = new MockMetadataHandler(_fileSystem);

            _sut = new DistributedFileCache(_logger, _metadataHandler, _fileSystem);
        }

        [Fact]
        public void Set_StoresValueInFile()
        {
            // Act
            _sut.Set("key", Encoding.UTF8.GetBytes("myvalue"), new DistributedCacheEntryOptions());

            // Assert
            _fileSystem.AllFiles.Should().ContainSingle("key");
            _fileSystem.GetFile("key").Contents.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("myvalue"));
        }

        [Fact]
        public void Get_RetrievesStoredValue()
        {
            // Arrange
            _sut.Set("key", Encoding.UTF8.GetBytes("myvalue"), new DistributedCacheEntryOptions());

            // Act
            var result = _sut.Get("key");

            // Assert
            result.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("myvalue"));
        }

        [Fact]
        public void Set_NoExpiration_WritesCorrectMetadata()
        {
            // Act
            _sut.Set("key", Encoding.UTF8.GetBytes("myvalue"), new DistributedCacheEntryOptions());

            // Assert
            var cachedMetadata = _metadataHandler.CachedMetadata.Should().ContainSingle("key").Which.Value;
            cachedMetadata.AbsoluteExpiration.Should().BeNull();
            cachedMetadata.SlidingExpiration.Should().BeNull();
        }

        [Fact]
        public void Set_WithExpiration_WritesCorrectMetadata()
        {
            // Act
            _sut.Set("key", Encoding.UTF8.GetBytes("myvalue"), new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(5),
                AbsoluteExpiration = 1.January(2020).At(12, 34, 56).AsUtc(),
            });

            // Assert
            var cachedMetadata = _metadataHandler.CachedMetadata.Should().ContainSingle("key").Which.Value;
            cachedMetadata.AbsoluteExpiration.Should().Be(1.January(2020).At(12, 34, 56).AsUtc());
            cachedMetadata.SlidingExpiration.Should().Be(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Remove_DeletesFile()
        {
            // Arrange
            _sut.Set("key", Encoding.UTF8.GetBytes("myvalue"), new DistributedCacheEntryOptions());

            // Act
            _sut.Remove("key");

            // Assert
            _fileSystem.AllFiles.Should().NotContain("key");
        }
    }
}
