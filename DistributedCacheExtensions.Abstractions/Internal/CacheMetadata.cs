using System;

namespace DistributedCacheExtensions.Abstractions.Internal
{
    public record CacheMetadata : ICacheMetadata
    {
        public string Key { get; set; }
        public string Reference { get; set; }
        public DateTime? AbsoluteExpiration { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public DateTime? SlidingExpirationMoment { get; set; }
    }
}
