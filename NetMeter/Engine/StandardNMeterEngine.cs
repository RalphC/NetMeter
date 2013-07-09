using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.Threads;

namespace NetMeter.Engine
{
    public class StandardNetMeterEngine : NetMeterEngine
    {
        private volatile static StandardNetMeterEngine engine;

        private String host;

        private List<AbstractThreadGroup> groups = new List<AbstractThreadGroup>();

        public static void stopEngineNow()
        {
            if (engine != null)
            {// May be null if called from Unit test
                engine.stopTest(true);
            }
        }

        public static void stopEngine()
        {
            if (engine != null)
            { // May be null if called from Unit test
                engine.stopTest(false);
            }
        }

        public static bool stopThread(String threadName)
        {
            return stopThread(threadName, false);
        }

        public static bool stopThreadNow(String threadName)
        {
            return stopThread(threadName, true);
        }

        private static bool stopThread(String threadName, bool now) 
        {
            if (engine == null) 
            {
                return false;// e.g. not yet started
            }
            bool wasStopped = false;
            // ConcurrentHashMap does not need sync. here
            foreach ( AbstractThreadGroup threadGroup in engine.groups)
            {
                wasStopped = wasStopped || threadGroup.stopThread(threadName, now);
            }
            return wasStopped;
        }

        public StandardNetMeterEngine() 
        {
            StandardNetMeterEngine(null);
        }

        public StandardNetMeterEngine(String host) 
        {
            this.host = host;
            // Hack to allow external control
            engine = this;
        }
    }
}
