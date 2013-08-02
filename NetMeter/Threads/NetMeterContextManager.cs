using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NetMeter.Threads
{
    public class NetMeterContextManager
    {
        private static ThreadLocal<NetMeterContext> threadContext = new ThreadLocal<NetMeterContext>();

        private static Int64 testStart = 0;

        private static Int32 numberOfActiveThreads = 0;

        private static Int32 numberOfThreadsStarted = 0;

        private static Int32 numberOfThreadsFinished = 0;

        private static Int32 totalThreads = 0;

        private NetMeterContextManager()
        {
        }

        public static NetMeterContext GetContext()
        {
            return threadContext.Value;
        }

        public static void RemoveContext()
        {
            threadContext.Dispose();
        }

        public static void ReplaceContext(NetMeterContext context)
        {
            threadContext.Dispose();
            threadContext.Value = context;
        }


        /**
         * Method is called by the JMeterEngine class when a test run is started.
         * Zeroes numberOfActiveThreads.
         * Saves current time in a field and in the JMeter property "TESTSTART.MS"
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void StartTest()
        {
            if (0==testStart)
            {
                numberOfActiveThreads = 0;
                testStart = (Int64)DateTime.Now.TimeOfDay.TotalMilliseconds;
            }
        }

        /**
         * Increment number of active threads.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void IncrNumberOfThreads() 
        {
            numberOfActiveThreads++;
            numberOfThreadsStarted++;
        }

        /**
         * Decrement number of active threads.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void DecrNumberOfThreads() 
        {
            numberOfActiveThreads--;
            numberOfThreadsFinished++;
        }

        /**
         * Get the number of currently active threads
         * @return active thread count
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Int32 GetNumberOfThreads() 
        {
            return numberOfActiveThreads;
        }

        // return all the associated counts together
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static ThreadCounts GetThreadCounts() {
            return new ThreadCounts(numberOfActiveThreads, numberOfThreadsStarted, numberOfThreadsFinished);
        }

        /**
         * Called by MainFrame#testEnded().
         * Clears start time field.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void EndTest() 
        {
            testStart = 0;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Int64 GetTestStartTime() 
        {// NOT USED
            return testStart;
        }

        /**
         * Get the total number of threads (>= active)
         * @return total thread count
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Int32 GetTotalThreads() 
        {
            return totalThreads;
        }

        /**
         * Update the total number of threads
         * @param thisGroup number of threads in this thread group
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void AddTotalThreads(Int32 thisGroup) {
            totalThreads += thisGroup;
        }

        /**
         * Set total threads to zero; also clears started and finished counts
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void ClearTotalThreads() {
            totalThreads = 0;
            numberOfThreadsStarted = 0;
            numberOfThreadsFinished = 0;
        }
    }
}
