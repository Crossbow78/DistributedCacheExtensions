using Microsoft.Extensions.Options;

namespace DistributedCacheExtensions
{
    public class DistributedFileCacheOptions : IOptions<DistributedFileCacheOptions>
    {
        public string Path { get; set; }

        public MetadataHandler MetadataHandler { get; set; } = MetadataHandler.SeparateFile;

        DistributedFileCacheOptions IOptions<DistributedFileCacheOptions>.Value => this;
    }

    public enum MetadataHandler
    {
        SeparateFile = 0,
        Timestamps = 1,
    }
}
