using System;
using Microsoft.IdentityModel.Abstractions;

namespace daemon_console
{
class MyIdentityLogger : IIdentityLogger
    {
        public EventLogLevel MinLogLevel = EventLogLevel.Critical;
        public MyIdentityLogger()
        {
            //Try to pull the log level from an environment variable
            var msalEnvLogLevel = Environment.GetEnvironmentVariable("MSAL_LOG_LEVEL");

            if (Enum.TryParse(msalEnvLogLevel, out EventLogLevel msalLogLevel))
            {
                MinLogLevel = msalLogLevel;
            }
            else
            {
                //Recommended default log level
                MinLogLevel = EventLogLevel.Informational;
            }
        }

        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return eventLogLevel <= MinLogLevel;
        }

        public void Log(LogEntry entry)
        {
            //Log Message here:
            Console.WriteLine(entry.Message);
        }
    }
}