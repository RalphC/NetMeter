using System;
using log4net;
using System.Diagnostics;

namespace Valkyrie.Logging
{
    public sealed class LoggingManager
    {
        /**
         * Get the Logger for a class - no argument needed because the calling class
         * name is derived automatically from the call stack.
         *
         * @return Logger
         */
        public static ILog getLoggerForClass()
        {
            StackTrace stack = new StackTrace();
            String callerName = stack.GetFrame(1).GetMethod().Name;
            return LogManager.GetLogger(callerName);
        }
    }
}
