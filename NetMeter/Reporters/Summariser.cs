using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.TestElements;
using System.Runtime.Serialization;
using NetMeter.Samplers;
using log4net;
using Valkyrie.Logging;
using System.Threading;
using NetMeter.Threads;
using NetMeter.Visualizers;

namespace NetMeter.Reporters
{
    /**
     * Generate a summary of the test run so far to the log file and/or standard
     * output. Both running and differential totals are shown. Output is generated
     * every n seconds (default 3 minutes) on the appropriate time boundary, so that
     * multiple test runs on the same time will be synchronised.
     *
     * This is mainly intended for batch (non-GUI) runs
     *
     * Note that the RunningSample start and end times relate to the samples,
     * not the reporting interval.
     *
     * Since the first sample in a delta is likely to have started in the previous reporting interval,
     * this means that the delta interval is likely to be longer than the reporting interval.
     *
     * Also, the sum of the delta intervals will be larger than the overall elapsed time.
     *
     * Data is accumulated according to the test element name.
     *
     */
    public class ResultCollector : AbstractTestElement, ISerializable, ExecutionListener, TestStateListener
    {
        private static ILog log = LoggingManager.GetLoggerForClass();

        /** interval between summaries (in seconds) default 3 minutes */
        private static Int64 INTERVAL = 3 * 60;

        /** Write messages to log file ? */
        private static Boolean TOLOG = true;

        private static Boolean TOCONSOLE = true;

        /*
         * Ensure that a report is not skipped if we are slightly late in checking
         * the time.
         */
        private static Int32 INTERVAL_WINDOW = 5; // in seconds

        /**
         * Lock used to protect accumulators update + instanceCount update
         */
        private static Object LOCK = new Object();

        /*
         * This map allows summarisers with the same name to contribute to the same totals.
         */
        //@GuardedBy("accumulators") - needed to ensure consistency between this and instanceCount
        private static ConcurrentDictionary<String, Totals> accumulators = new ConcurrentDictionary<String, Totals>();

        //@GuardedBy("accumulators")
        private static int instanceCount; // number of active tests

        /*
         * Cached copy of Totals for this instance.
         * The variables do not need to be synchronised,
         * as they are not shared between threads
         * However the contents do need to be synchronized.
         */
        //@GuardedBy("myTotals")
        private Totals myTotals = null;

        // Name of the accumulator. Set up by testStarted().
        private String myName;

        /*
         * Constructor is initially called once for each occurrence in the test plan.
         * For GUI, several more instances are created.
         * Then clear is called at start of test.
         * Called several times during test startup.
         * The name will not necessarily have been set at this point.
         */
        public ResultCollector() 
            : base()
        {
            Monitor.Enter(LOCK);
            try
            {
                accumulators.Clear();
                instanceCount = 0;
            }
            finally
            {
                Monitor.Exit(LOCK);
            }
        }

        /**
         * Constructor for use during startup (intended for non-GUI use)
         *
         * @param name of summariser
         */
        public ResultCollector(String name)
            : this()
        {
            SetName(name);
        }


        /**
         * Accumulates the sample in two SampleResult objects - one for running
         * totals, and the other for deltas.
         *
         * @see org.apache.jmeter.samplers.SampleListener#sampleOccurred(org.apache.jmeter.samplers.SampleEvent)
         */
        public void ExecutionOccurred(ExecutionEvent e) 
        {
            ExecuteResult s = e.getResult();

            Int64 now = DateTime.Now.Ticks;// in seconds

            RunningSample myDelta = null;
            RunningSample myTotal = null;
            Boolean reportNow = false;

            /*
             * Have we reached the reporting boundary?
             * Need to allow for a margin of error, otherwise can miss the slot.
             * Also need to check we've not hit the window already
             */
            Monitor.Enter(myTotals);
            try
            {
                if (s != null) 
                {
                    myTotals.delta.AddSample(s);
                }

                if ((now > myTotals.last + INTERVAL_WINDOW) && (now % INTERVAL <= INTERVAL_WINDOW)) 
                {
                    reportNow = true;

                    // copy the data to minimise the synch time
                    myDelta = new RunningSample(myTotals.delta);
                    myTotals.MoveDelta();
                    myTotal = new RunningSample(myTotals.total);

                    myTotals.last = now; // stop double-reporting
                }
            }
            finally
            {
                Monitor.Exit(myTotals);
            }

            if (reportNow) 
            {
                String str;
                str = Format(myName, myDelta, "+");
                if (TOLOG) 
                {
                    log.Info(str);
                }
                if (TOCONSOLE)
                {
                    System.Console.WriteLine(str);
                }

                // Only if we have updated them
                if (myTotal != null && myDelta != null &&myTotal.getNumSamples() != myDelta.getNumSamples()) 
                {
                    str = Format(myName, myTotal, "=");
                    if (TOLOG)
                    {
                        log.Info(str);
                    }
                    if (TOCONSOLE) 
                    {
                        System.Console.WriteLine(str);
                    }
                }
            }
        }

        /**
         * @param myTotal
         * @param string
         * @return
         */
        private static String Format(String name, RunningSample s, String type) 
        {
            StringBuilder tmp = new StringBuilder(20); // for intermediate use
            StringBuilder sb = new StringBuilder(100); // output line buffer
            sb.Append(name);
            sb.Append(" ");
            sb.Append(type);
            sb.Append(" ");
            sb.Append(s.getNumSamples());
            sb.Append(" in ");
            long elapsed = s.getElapsed();
            long elapsedSec = (elapsed + 500) / 1000; // rounded seconds
            if (elapsedSec > 100       // No point displaying decimals (less than 1% error)
             || (elapsed - elapsedSec * 1000) < 50 // decimal would be zero
             ) 
            {
                sb.Append(elapsedSec);
            } 
            else 
            {
                double elapsedSecf = elapsed / 1000.0d; // fractional seconds
                sb.Append(elapsedSecf); // This will round
            }
            sb.Append("s = ");
            if (elapsed > 0) 
            {
                sb.Append(s.getRate());
            } 
            else
            {
                sb.Append("******");// Rate is effectively infinite
            }
            sb.Append("/s Avg: ");
            sb.Append(s.getAverage());
            sb.Append(" Min: ");
            sb.Append(s.getMin());
            sb.Append(" Max: ");
            sb.Append(s.getMax());
            sb.Append(" Err: ");
            sb.Append(s.getErrorCount());
            sb.Append(" (");
            sb.Append(s.getErrorPercentageString());
            sb.Append(")");
            if ("+".Equals(type)) 
            {
                ThreadCounts tc = NetMeterContextManager.GetThreadCounts();
                sb.Append(" Active: ");
                sb.Append(tc.activeThreads);
                sb.Append(" Started: ");
                sb.Append(tc.startedThreads);
                sb.Append(" Finished: ");
                sb.Append(tc.finishedThreads);
            }
            return sb.ToString();
        }

        /*
         * The testStarted/testEnded methods are called at the start and end of a test.
         *
         * However, when a test is run on multiple nodes, there is no guarantee that all the
         * testStarted() methods will be called before all the threadStart() or sampleOccurred()
         * methods for other threads - nor that testEnded() will only be called after all
         * sampleOccurred() calls. The ordering is only guaranteed within a single test.
         *
         */


        /** {@inheritDoc} */
        public void TestStarted() 
        {
            TestStarted("local");
        }

        /** {@inheritDoc} */
        public void TestEnded() 
        {
            TestEnded("local");
        }

        /**
         * Called once for each Summariser in the test plan.
         * There may be more than one summariser with the same name,
         * however they will all be called before the test proper starts.
         * <p>
         * However, note that this applies to a single test only.
         * When running in client-server mode, testStarted() may be
         * invoked after sampleOccurred().
         * <p>
         * {@inheritDoc}
         */
        public void TestStarted(String host) 
        {
            Monitor.Enter(LOCK);
            try
            {
                myName = GetName();
                if (!accumulators.TryGetValue(myName, out myTotals))
                {
                    myTotals = new Totals();
                    accumulators.TryAdd(myName, myTotals);
                }
                instanceCount++;
            }
            finally
            {
                Monitor.Exit(LOCK);
            }
        }

        /**
         * Called from a different thread as testStarted() but using the same instance.
         * So synch is needed to fetch the accumulator, and the myName field will already be set up.
         * <p>
         * {@inheritDoc}
         */
        public void TestEnded(String host) 
        {
            List<KeyValuePair<String, Totals>> totals = new List<KeyValuePair<String, Totals>>();

            Monitor.Enter(LOCK);
            try
            {
                instanceCount--;
                if (instanceCount <= 0)
                {
                    totals = accumulators.ToList();
                }
            }
            finally
            {
                Monitor.Exit(LOCK);
            }

            // We're not done yet
            if (totals.Count == 0) return;

            foreach(KeyValuePair<String, Totals> pair in totals)
            {
                String str = "";
                String name = pair.Key;
                Totals total = pair.Value;
                // Only print final delta if there were some samples in the delta
                // and there has been at least one sample reported previously
                if (total.delta.getNumSamples() > 0 && total.total.getNumSamples() >  0)
                {
                    str = Format(name, total.delta, "+");
                    if (TOLOG)
                    {
                        log.Info(str);
                    }
                    if (TOCONSOLE) {
                        System.Console.WriteLine(str);
                    }
                }
                total.MoveDelta();
                str = Format(name, total.total, "=");
                if (TOLOG) 
                {
                    log.Info(str);
                }
                if (TOCONSOLE) 
                {
                    System.Console.WriteLine(str);
                }
            }
        }



        /*
         * Contains the items needed to collect stats for a summariser
         *
         */
        public class Totals 
        {
            /** Time of last summary (to prevent double reporting) */
            public Int64 last = 0;

            public RunningSample delta = new RunningSample("DELTA", 0);

            public RunningSample total = new RunningSample("TOTAL", 0);

            /**
             * Add the delta values to the total values and clear the delta
             */
            public void MoveDelta() 
            {
                total.AddSample(delta);
                delta.Clear();
            }
        }
    }
}
