using NLog.Targets;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog.Config;

namespace YetAnotherGarminConnectClient
{
    public class Logger
    {
        private static string TARGET_NAME = "logmemory";
        private static string ERRORS_TARGET_NAME = "logmemory_errors";
        private static bool _isCreated = false;
        public static void CreateLogger()
        {
            if (!_isCreated)
            {
                _isCreated = true;

                LoggingConfiguration configuration = new LoggingConfiguration();

                MemoryTarget target = new MemoryTarget(TARGET_NAME);
                target.Layout = "${message}";

                configuration.AddTarget(target);
                LoggingRule traceRule = new LoggingRule("*", NLog.LogLevel.Trace, target);
                configuration.LoggingRules.Add(traceRule);

                MemoryTarget errorsTarget = new MemoryTarget(ERRORS_TARGET_NAME);
                errorsTarget.Layout = "${message}";

                configuration.AddTarget(errorsTarget);
                LoggingRule errorsRule = new LoggingRule("*", NLog.LogLevel.Error, errorsTarget);
                configuration.LoggingRules.Add(errorsRule);
                LogManager.Configuration = configuration;
            }
        }

        public static IList<string> GetLogs()
        {
            if (!_isCreated)
            {
                return new List<string>();
            }

            var target = LogManager.Configuration.FindTargetByName<MemoryTarget>(TARGET_NAME);
            var logEvents = target.Logs;
            return logEvents;
        }

        public static IList<string> GetErrorLogs()
        {
            if (!_isCreated)
            {
                return new List<string>();
            }

            var target = LogManager.Configuration.FindTargetByName<MemoryTarget>(ERRORS_TARGET_NAME);
            var logEvents = target.Logs;
            return logEvents;
        }
    }
}
