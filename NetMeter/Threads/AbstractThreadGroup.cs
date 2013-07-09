using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization;
using NetMeter.TestElements;
using NetMeter.Engine;

namespace NetMeter.Threads
{
    public abstract class AbstractThreadGroup : AbstractTestElement, ISerializable, 
    {
        /** Action to be taken when a Sampler error occurs */
        public static String ON_SAMPLE_ERROR = "ThreadGroup.on_sample_error"; // int

        /** Continue, i.e. ignore sampler errors */
        public static String ON_SAMPLE_ERROR_CONTINUE = "continue";

        /** Start next loop for current thread if sampler error occurs */
        public static String ON_SAMPLE_ERROR_START_NEXT_LOOP = "startnextloop";

        /** Stop current thread if sampler error occurs */
        public static String ON_SAMPLE_ERROR_STOPTHREAD = "stopthread";

        /** Stop test (all threads) if sampler error occurs */
        public static String ON_SAMPLE_ERROR_STOPTEST = "stoptest";

        /** Stop test NOW (all threads) if sampler error occurs */
        public static String ON_SAMPLE_ERROR_STOPTEST_NOW = "stoptestnow";

        /** Number of threads in the thread group */
        public static String NUM_THREADS = "ThreadGroup.num_threads";

        public static String MAIN_CONTROLLER = "ThreadGroup.main_controller";


        /**
         * Get the number of threads.
         *
         * @return the number of threads.
         */
        public int getThreadsNumber() 
        {
            return this.getPropertyAsInt(AbstractThreadGroup.NUM_THREADS);
        }

        /**
         * Check if a sampler error should cause thread to start next loop.
         *
         * @return true if thread should start next loop
         */
        public bool getOnErrorStartNextLoop() 
        {
            return getPropertyAsString(AbstractThreadGroup.ON_SAMPLE_ERROR).Equals(ON_SAMPLE_ERROR_START_NEXT_LOOP);
        }

        /**
         * Check if a sampler error should cause thread to stop.
         *
         * @return true if thread should stop
         */
        public bool getOnErrorStopThread() 
        {
            return getPropertyAsString(AbstractThreadGroup.ON_SAMPLE_ERROR).Equals(ON_SAMPLE_ERROR_STOPTHREAD);
        }

        /**
         * Check if a sampler error should cause test to stop.
         *
         * @return true if test (all threads) should stop
         */
        public bool getOnErrorStopTest() 
        {
            return getPropertyAsString(AbstractThreadGroup.ON_SAMPLE_ERROR).Equals(ON_SAMPLE_ERROR_STOPTEST);
        }

        /**
         * Check if a sampler error should cause test to stop now.
         *
         * @return true if test (all threads) should stop immediately
         */
        public bool getOnErrorStopTestNow()
        {
            return getPropertyAsString(AbstractThreadGroup.ON_SAMPLE_ERROR).Equals(ON_SAMPLE_ERROR_STOPTEST_NOW);
        }

        void threadFinished(NetMeterThread thread)
        {

        }


    }
}
