using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization;
using NetMeter.TestElements;
using NetMeter.Engine;
using NetMeter.Control;
using NetMeter.Engine.Event;
using NetMeter.TestElements.Property;
using Valkyrie.Collections;

namespace NetMeter.Threads
{
    public abstract class AbstractThreadGroup : AbstractTestElement,Controller, NetMeterThreadMonitor, ISerializable 
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

        private sealed Int64 numberOfThreads = 0;


        public override Boolean isDone() 
        {
            return getSamplerController().isDone();
        }

        public override Sampler next()
        {
            return getSamplerController().next();
        }

        /**
         * Get the sampler controller.
         *
         * @return the sampler controller.
         */
        public Controller getSamplerController()
        {
            return (Controller) getProperty(MAIN_CONTROLLER).getObjectValue();
        }

        /**
         * Set the sampler controller.
         *
         * @param c
         *            the sampler controller.
         */
        public void setSamplerController(LoopController c) 
        {
            c.setContinueForever(false);
            setProperty(new TestElementProperty(MAIN_CONTROLLER, c));
        }

        /**
         * Add a test element.
         *
         * @param child
         *            the test element to add.
         */
        public override void addTestElement(TestElement child) 
        {
            getSamplerController().addTestElement(child);
        }

        public override Boolean addTestElementOnce(TestElement child)
        {
            if (children.putIfAbsent(child, DUMMY) == null)
            {
                addTestElement(child);
                return true;
            }
            return false;
        }

        public override void addIterationListener(LoopIterationListener lis) 
        {
            getSamplerController().addIterationListener(lis);
        }
    
        public override void removeIterationListener(LoopIterationListener iterationListener) 
        {
            getSamplerController().removeIterationListener(iterationListener);
        }

        public override void initialize() 
        {
            Controller c = getSamplerController();
            NetMeterProperty property = c.getProperty(TestElement.NAME);
            property.setObjectValue(getName()); // Copy our name into that of the controller
            property.setRunningVersion(property.isRunningVersion());// otherwise name reverts
            c.initialize();
        }

        /**
         * Start next iteration after an error
         */
        public void startNextLoop() 
        {
           ((LoopController) getSamplerController()).startNextLoop();
        }
    
        /**
         * NOOP
         */
        public override void triggerEndOfLoop()
        {// NOOP
        }

         /**
         * Set the total number of threads to start
         *
         * @param numThreads
         *            the number of threads.
         */
        public void setNumThreads(int numThreads) 
        {
            setProperty(new IntegerProperty(NUM_THREADS, numThreads));
        }

        /**
         * Increment the number of active threads
         */
        public void incrNumberOfThreads() 
        {
            Interlocked.Increment(ref numberOfThreads);
        }

        /**
         * Decrement the number of active threads
         */
        public void decrNumberOfThreads() 
        {
            Interlocked.Decrement(ref numberOfThreads);
        }

        /**
         * Get the number of active threads
         */
        public Int32 getNumberOfThreads()
        {
            return (Int32)Interlocked.Read(ref numberOfThreads);
        }
    
        /**
         * Get the number of threads.
         *
         * @return the number of threads.
         */
        public int getNumThreads() 
        {
            return this.getPropertyAsInt(AbstractThreadGroup.NUM_THREADS);
        }

        /**
         * Check if a sampler error should cause thread to start next loop.
         *
         * @return true if thread should start next loop
         */
        public Boolean getOnErrorStartNextLoop() 
        {
            return getPropertyAsString(AbstractThreadGroup.ON_SAMPLE_ERROR).Equals(ON_SAMPLE_ERROR_START_NEXT_LOOP);
        }

        /**
         * Check if a sampler error should cause thread to stop.
         *
         * @return true if thread should stop
         */
        public Boolean getOnErrorStopThread() 
        {
            return getPropertyAsString(AbstractThreadGroup.ON_SAMPLE_ERROR).Equals(ON_SAMPLE_ERROR_STOPTHREAD);
        }

        /**
         * Check if a sampler error should cause test to stop.
         *
         * @return true if test (all threads) should stop
         */
        public Boolean getOnErrorStopTest() 
        {
            return getPropertyAsString(AbstractThreadGroup.ON_SAMPLE_ERROR).Equals(ON_SAMPLE_ERROR_STOPTEST);
        }

        /**
         * Check if a sampler error should cause test to stop now.
         *
         * @return true if test (all threads) should stop immediately
         */
        public Boolean getOnErrorStopTestNow()
        {
            return getPropertyAsString(AbstractThreadGroup.ON_SAMPLE_ERROR).Equals(ON_SAMPLE_ERROR_STOPTEST_NOW);
        }

        public abstract Boolean stopThread(String threadName, Boolean now);

        public abstract int numberOfActiveThreads();

        public abstract void start(int groupCount, ListenerNotifier notifier, ListedHashTree threadGroupTree, StandardNetMeterEngine engine);

        public abstract Boolean verifyThreadsStopped();

        public abstract void waitThreadsStopped();

        public abstract void tellThreadsToStop();

        public abstract void stop();


    }
}
