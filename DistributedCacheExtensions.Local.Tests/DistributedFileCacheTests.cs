using DistributedCacheExtensions.Abstractions;
using DistributedCacheExtensions.Abstractions.Internal;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using System.Linq;
using System.Text;
using Xunit;

namespace DistributedCacheExtensions.Local.Tests
{
    public class DistributedFileCacheTests
    {
        private readonly DistributedCache _sut;
        private readonly IMetadataHandler _metadataHandler;
        private readonly IStorageHandler _storageHandler;
        private readonly ILogger<DistributedCache> _logger;

        private byte[] DefaultContent => Encoding.UTF8.GetBytes("myvalue");

        public DistributedFileCacheTests()
        {
            _logger = NullLogger<DistributedCache>.Instance;
            _metadataHandler = Substitute.For<IMetadataHandler>();
            _storageHandler = Substitute.For<IStorageHandler>();

            _sut = new DistributedCache(_logger, _storageHandler, _metadataHandler);
        }

        [Fact]
        public void Set_StoresValue()
        {
            // Arrange
            var cacheMetadata = new CacheMetadata
            {
                Key = "key",
                Reference = "reference",
            };
            _metadataHandler.Get(default).ReturnsForAnyArgs(cacheMetadata);

            // Act
            _sut.Set("key", DefaultContent, new DistributedCacheEntryOptions());

            // Assert
            _storageHandler.Received(1).Save("reference", Arg.Is<byte[]>(x => x.SequenceEqual(DefaultContent)));
            _metadataHandler.Received(1).Get("key");
            _metadataHandler.Received(1).Set(cacheMetadata);
        }

        [Fact]
        public void Get_RetrievesValue()
        {
            // Arrange
            var cacheMetadata = new CacheMetadata
            {
                Key = "key",
                Reference = "reference",
            };
            _metadataHandler.Get(default).ReturnsForAnyArgs(cacheMetadata);
            _storageHandler.Load(default).ReturnsForAnyArgs(DefaultContent);

            // Act
            var result = _sut.Get("key");

            // Assert
            _storageHandler.Received(1).Load("reference");
            _metadataHandler.Received(1).Get("key");
            _metadataHandler.Received(1).Set(cacheMetadata);
            result.Should().BeEquivalentTo(DefaultContent);
        }

        [Fact]
        public void Set_WithExpiration_WritesExpirationMetadata()
        {
            // Arrange
            var cacheMetadata = new CacheMetadata
            {
                Key = "key",
                Reference = "reference",
            };
            _metadataHandler.Get(default).ReturnsForAnyArgs(cacheMetadata);
            var expiration = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(10),
                AbsoluteExpiration = DateTimeOffset.Parse("2025-01-01T12:34:56Z"),
            };

            // Act
            _sut.Set("key", DefaultContent, expiration);

            // Assert
            _storageHandler.Received(1).Save("reference", Arg.Is<byte[]>(x => x.SequenceEqual(DefaultContent)));
            _metadataHandler.Received(1).Get("key");
            _metadataHandler.Received(1).Set(Arg.Is<ICacheMetadata>(x =>
                x.Key == "key"
                && x.AbsoluteExpiration == expiration.AbsoluteExpiration
                && x.SlidingExpiration == expiration.SlidingExpiration));
        }

        [Fact]
        public void Remove_DeletesCachedValue()
        {
            // Arrange
            var cacheMetadata = new CacheMetadata
            {
                Key = "key",
                Reference = "reference",
            };
            _metadataHandler.Get(default).ReturnsForAnyArgs(cacheMetadata);

            // Act
            _sut.Remove("key");

            // Assert
            _metadataHandler.Received(1).Expire(cacheMetadata);
            _storageHandler.Received(1).Delete("reference");
        }

        [Fact]
        public void Refresh_UpdatesMetadata()
        {
            // Arrange
            var cacheMetadata = new CacheMetadata
            {
                Key = "key",
                Reference = "reference",
            };
            _metadataHandler.Get(default).ReturnsForAnyArgs(cacheMetadata);

            // Act
            _sut.Refresh("key");

            // Assert
            _metadataHandler.Received(1).Get("key");
            _metadataHandler.Received(1).Set(cacheMetadata);
        }
    }
}
