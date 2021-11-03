using DistributedCacheExtensions.Local.Internal;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DistributedCacheExtensions.Local.Tests
{
    public class FileStorageHandlerTests
    {
        private readonly MockFileSystem _fileSystem = new MockFileSystem();
        private readonly FileStorageHandler _sut;

        private byte[] DefaultContent => Encoding.UTF8.GetBytes("myvalue");

        public FileStorageHandlerTests()
        {
            _sut = new FileStorageHandler(NullLogger<FileStorageHandler>.Instance, _fileSystem);
        }

        [Fact]
        public async Task Save_StoresValueInFileAsync()
        {
            // Act
            await _sut.Save("reference", DefaultContent);

            // Assert
            _fileSystem.AllFiles.Should().ContainSingle("reference");
            _fileSystem.GetFile("reference").Contents.Should().BeEquivalentTo(DefaultContent);
        }

        [Fact]
        public async Task Load_RetrievesStoredValue()
        {
            // Arrange
            _fileSystem.AddFile("reference", new MockFileData(DefaultContent));
            
            // Act
            var result = await _sut.Load("reference");

            // Assert
            result.Should().BeEquivalentTo(DefaultContent);
        }

        [Fact]
        public async Task Load_NonExisting_ReturnsNullAsync()
        {
            // Act
            var result = await _sut.Load("reference");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync()
        {
            // Arrange
            _fileSystem.AddFile("reference", new MockFileData(DefaultContent));

            // Act
            await _sut.Delete("reference");

            // Assert
            _fileSystem.AllFiles.Should().NotContain("reference");
        }
    }
}
