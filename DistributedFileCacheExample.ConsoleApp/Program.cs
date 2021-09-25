using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace DistributedFileCacheExample.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = Startup.GetServiceProvider();

            var service = serviceProvider.GetRequiredService<IMyService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            for (int i = 0; i < 2; i++)
            {
                var val = service.DoSomething($"key{i}");
                logger.LogInformation($"{i}: {val}");
            }

            Console.ReadLine();

            service.Reset("key0");
        }
    }
}


