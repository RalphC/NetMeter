using NetMeter.Engine;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using Valkyrie.Collections;

namespace NetMeter.Threads
{
    class NetMeterThreadGroup : AbstractThreadGroup
    { 
        /** Ramp-up time */
        public static sealed String RAMP_TIME = "ThreadGroup.ramp_time";

        /** Whether thread startup is delayed until required */
        public static sealed String DELAYED_START = "ThreadGroup.delayedStart";

        /** Whether scheduler is being used */
        public static sealed String SCHEDULER = "ThreadGroup.scheduler";

        /** Scheduler absolute start time */
        public static sealed String START_TIME = "ThreadGroup.start_time";

        /** Scheduler absolute end time */
        public static sealed String END_TIME = "ThreadGroup.end_time";

        /** Scheduler duration, overrides end time */
        public static sealed String DURATION = "ThreadGroup.duration";

        /** Scheduler start delay, overrides start time */
        public static sealed String DELAY = "ThreadGroup.delay";

        private volatile bool running = false;

        /**
         * Are we using delayed startup?
         */
        private bool delayedStartup;

        /**
         * No-arg constructor.
         */
        public NetMeterThreadGroup()
        {
        }

        /**
         * Set whether scheduler is being used
         *
         * @param Scheduler true is scheduler is to be used
         */
        public void SetScheduler(bool Scheduler) 
        {
            setProperty(new BooleanProperty(SCHEDULER, Scheduler));
        }

        // List of active threads
        private ConcurrentDictionary<NetMeterThread, Thread> allThreads = new ConcurrentDictionary<NetMeterThread, Thread>();

        public void start(int groupCount, ListenerNotifier notifier, OrderedHashTree threadGroupTree, StandardNetMeterEngine engine)
        {
            int threadNumber = GetThreadsNumber();
            // TODO : log

            Int32 now = System.DateTime.Now.Millisecond;
            NetMeterContext context = NetMeterContextManager.getContext();
            for (int i = 0; running && i < threadNumber; i++)
            {
                NetMeterThread nmThread = CreateThread(groupCount, notifier, threadGroupTree, engine, i, context);
                Thread newThread = new Thread(nmThread.run);
                RegisterStartedThread(nmThread, newThread);
                newThread.Start();
            }

            // TODO : log

        }

        /**
         * Register Thread when it starts
         * @param jMeterThread {@link JMeterThread}
         * @param newThread Thread
         */
        private void RegisterStartedThread(NetMeterThread nmThread, Thread newThread)
        {
            allThreads.TryAdd(nmThread, newThread);
        }

        private NetMeterThread CreateThread(int groupCount, ListenerNotifier notifier, OrderedHashTree threadGroupTree,
                StandardNetMeterEngine engine, int i, NetMeterContext context) 
        {
            String groupName = GetName();
            NetMeterThread nmThread = new NetMeterThread(CloneTree(threadGroupTree), this, notifier);
            nmThread.SetThreadNum(i);
            nmThread.SetThreadGroup(this);
            nmThread.SetInitialContext(context);
            String threadName = groupName + " " + (groupCount) + "-" + (i + 1);
            nmThread.SetThreadName(threadName);
            nmThread.SetEngine(engine);
            nmThread.SetOnErrorStopTest(GetOnErrorStopTest());
            nmThread.SetOnErrorStopTestNow(GetOnErrorStopTestNow());
            nmThread.SetOnErrorStopThread(GetOnErrorStopThread());
            nmThread.SetOnErrorStartNextLoop(GetOnErrorStartNextLoop());
            return nmThread;
        }
    }
}
