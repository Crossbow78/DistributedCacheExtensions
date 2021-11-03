using System.Threading.Tasks;

namespace DistributedCacheExtensions.Local.Abstraction
{
    interface IStorageHandler
    {
        Task Save(string reference, byte[] value);

        Task<byte[]> Load(string reference);

        Task Delete(string reference);
    }
}
