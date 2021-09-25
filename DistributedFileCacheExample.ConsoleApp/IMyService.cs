namespace DistributedFileCacheExample.ConsoleApp
{
    public interface IMyService
    {
        string DoSomething(string key);

        void Reset(string key);
    }
}