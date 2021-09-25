using DistributedCacheExtensions.Abstraction;
using System;
using System.IO.Abstractions;

namespace DistributedCacheExtensions.Internal
{
    internal class CacheMetadata : ICacheMetadata
    {
        public string Key { get; set; }
        public IFileInfo FileInfo { get; set; }
        public DateTime? AbsoluteExpiration { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public DateTime? SlidingExpirationMoment { get; set; }
    }
}
