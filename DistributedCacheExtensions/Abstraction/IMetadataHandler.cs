namespace DistributedCacheExtensions.Abstraction
{
    internal interface IMetadataHandler
    {
        ICacheMetadata Get(string key);

        void Set(ICacheMetadata cacheMetadata);

        void Expire(ICacheMetadata cacheMetadata);
    }
}