using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public interface ILogger
    {
        void Log(string message);
    }

    public class Logger : ILogger
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Logger));

        public Logger()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        public void Log(string message)
        {
            log.Info(message);
        }
    }
}
