using DistributedCacheExtensions.Internal;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DistributedCacheExtensions.Tests
{
    // Exercises the real FileMetadataHandler through DistributedFileCache,
    // with focus on recovery from empty/corrupt .metadata sidecar files.
    public class FileMetadataHandlerTests
    {
        private readonly MockFileSystem _fileSystem = new();
        private readonly RecordingLoggerFactory _loggerFactory = new();
        private readonly DistributedFileCacheOptions _options;
        private readonly FileMetadataHandler _metadataHandler;
        private readonly DistributedFileCache _sut;

        public FileMetadataHandlerTests()
        {
            _options = new DistributedFileCacheOptions { Path = _fileSystem.Path.GetFullPath("cache") };
            _fileSystem.AddDirectory(_options.Path);
            _metadataHandler = new FileMetadataHandler(_loggerFactory, Options.Create(_options), new DateTimeProvider(), _fileSystem);
            _sut = new DistributedFileCache(_loggerFactory.CreateLogger<DistributedFileCache>(), _metadataHandler, _fileSystem);
        }

        [Theory]
        [InlineData("")]
        [InlineData("<not json>")]
        [InlineData("null")]
        public async Task GetAsync_WhenMetadataFileIsUnusable_ReturnsCacheMissAndRemovesCorruptFiles(string sidecarContent)
        {
            // Arrange - a poisoned pair: unusable sidecar alongside an otherwise healthy data file
            SeedSidecar("key", sidecarContent, dataContent: Encoding.UTF8.GetBytes("myvalue"));

            // Act
            var result = await _sut.GetAsync("key");
            var subsequentResult = await _sut.GetAsync("key");

            // Assert - miss instead of exception, corrupt pair removed, one warning, and the key self-heals
            result.Should().BeNull();
            subsequentResult.Should().BeNull();
            _fileSystem.File.Exists(ContentPath("key")).Should().BeFalse();
            _fileSystem.File.Exists(MetadataPath("key")).Should().BeFalse();
            _loggerFactory.Warnings.Should().ContainSingle();
        }

        [Theory]
        [InlineData("")]
        [InlineData("<not json>")]
        [InlineData("null")]
        public async Task SetAsync_WhenMetadataFileIsUnusable_SucceedsAndRepopulatesHealthyPair(string sidecarContent)
        {
            // Arrange
            SeedSidecar("key", sidecarContent, dataContent: Encoding.UTF8.GetBytes("stalevalue"));
            var absoluteExpiration = 1.January(2099).At(12, 34, 56).AsUtc();
            var slidingExpiration = TimeSpan.FromMinutes(10);
            var value = Encoding.UTF8.GetBytes("myvalue");
            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpiration,
            };

            // Act
            await _sut.SetAsync("key", value, entryOptions);
            var reread = await _sut.GetAsync("key");

            // Assert - a valid data+metadata pair is left behind and can be read back
            reread.Should().BeEquivalentTo(value);
            _fileSystem.File.Exists(MetadataPath("key")).Should().BeTrue();

            var metadata = _metadataHandler.Get("key");
            metadata.AbsoluteExpiration.Should().Be(absoluteExpiration);
            metadata.SlidingExpiration.Should().Be(slidingExpiration);

            _loggerFactory.Warnings.Should().ContainSingle();
        }

        [Fact]
        public async Task GetAsync_WhenMetadataFileIsHealthy_RoundTripsValueAndExpiration()
        {
            // Arrange
            var absoluteExpiration = 1.January(2099).At(12, 34, 56).AsUtc();
            var slidingExpiration = TimeSpan.FromMinutes(10);
            var value = Encoding.UTF8.GetBytes("myvalue");
            await _sut.SetAsync("key", value, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpiration,
            });

            // Act
            var result = await _sut.GetAsync("key");

            // Assert
            result.Should().BeEquivalentTo(value);
            _fileSystem.File.Exists(MetadataPath("key")).Should().BeTrue();

            var metadata = _metadataHandler.Get("key");
            metadata.AbsoluteExpiration.Should().Be(absoluteExpiration);
            metadata.SlidingExpiration.Should().Be(slidingExpiration);

            _loggerFactory.Warnings.Should().BeEmpty();
        }

        private void SeedSidecar(string key, string sidecarContent, byte[] dataContent)
        {
            _fileSystem.AddFile(MetadataPath(key), new MockFileData(sidecarContent));
            _fileSystem.AddFile(ContentPath(key), new MockFileData(dataContent));
        }

        // Mirrors how FileMetadataHandler derives its cache paths from the key
        private string ContentPath(string key)
        {
            var fileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
            return _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_options.Path, fileName));
        }

        private string MetadataPath(string key) => ContentPath(key) + ".metadata";

        private sealed class RecordingLoggerFactory : ILoggerFactory
        {
            private readonly RecordingLogger _logger = new();

            public IReadOnlyList<LogEntry> Warnings => _logger.Warnings;

            public ILogger CreateLogger(string categoryName) => _logger;

            public void AddProvider(ILoggerProvider provider)
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class RecordingLogger : ILogger
        {
            private readonly List<LogEntry> _warnings = new();

            public IReadOnlyList<LogEntry> Warnings => _warnings;

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopDisposable.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.Warning)
                {
                    _warnings.Add(new LogEntry(logLevel, formatter(state, exception)));
                }
            }

            private sealed class NoopDisposable : IDisposable
            {
                public static readonly NoopDisposable Instance = new();

                public void Dispose()
                {
                }
            }
        }

        private sealed record LogEntry(LogLevel Level, string Message);
    }
}
