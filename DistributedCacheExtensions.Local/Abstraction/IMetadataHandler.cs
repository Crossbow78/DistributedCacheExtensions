using System.Threading.Tasks;

namespace DistributedCacheExtensions.Local.Abstraction
{
    internal interface IMetadataHandler
    {
        Task<ICacheMetadata> Get(string key);

        Task Set(ICacheMetadata cacheMetadata);

        Task Expire(ICacheMetadata cacheMetadata);
    }
}