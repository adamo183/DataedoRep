using log4net.Config;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace ConsoleApp
{
    internal class Program
    {
        private static void Main()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository);

            var serviceProvider = new ServiceCollection()
                .AddSingleton<ILogger, Logger>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger>();

            var reader = new Parser(logger);
            reader.Do("sampleFile1.csv", "dataSource.csv");
        }
    }
}
