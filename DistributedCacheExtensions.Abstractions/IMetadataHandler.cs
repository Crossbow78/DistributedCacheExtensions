using System.Threading.Tasks;

namespace DistributedCacheExtensions.Abstractions
{
    public interface IMetadataHandler
    {
        Task<ICacheMetadata> Get(string key);

        Task Set(ICacheMetadata cacheMetadata);

        Task Expire(ICacheMetadata cacheMetadata);
    }
}