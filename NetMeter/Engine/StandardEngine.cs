using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMeter.Threads;
using Valkyrie.Logging;
using log4net;
using Valkyrie.Collections;
using NetMeter.TestElements;
using System.Threading;
using NetMeter.Util;

namespace NetMeter.Engine
{
    public class StandardEngine : NetMeterEngine
    {
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

        // Should we exit at end of the test? (only applies to server, because host is non-null)
        private static sealed Boolean exitAfterTest = false;

        private static sealed Boolean startListenersLater = true;

        /*
         * Allow functions etc to register for testStopped notification.
         * Only used by the function parser so far.
         * The list is merged with the testListeners and then cleared.
         */
        private static sealed List<TestStateListener> testList = new List<TestStateListener>();

        /** Whether to call System.exit(0) in exit after stopping RMI */
        private static sealed Boolean REMOTE_SYSTEM_EXIT = false;

        /** Whether to call System.exit(1) if threads won't stop */
        private static sealed Boolean SYSTEM_EXIT_ON_STOP_FAIL = true;

        /** Flag to show whether test is running. Set to false to stop creating more threads. */
        private static volatile Boolean running = false;

        /** Flag to show whether test was shutdown gracefully. */
        private volatile Boolean shutdown = false;

        /** Flag to show whether engine is active. Set to false at end of test. */
        private volatile Boolean active = false;

        /** Thread Groups run sequentially */
        private volatile Boolean serialized = false;

        /** tearDown Thread Groups run after shutdown of main threads */
        private volatile Boolean tearDownOnShutdown = false;

        private volatile static StandardEngine engine;

        private String host;

        private HashTree test;

        private List<AbstractThreadGroup> groups = new List<AbstractThreadGroup>();

        public static void StopEngineNow()
        {
            if (engine != null)
            {// May be null if called from Unit test
                engine.StopTest(true);
            }
        }

        public static void StopEngine()
        {
            if (engine != null)
            { // May be null if called from Unit test
                engine.StopTest(false);
            }
        }

        public static bool StopThread(String threadName)
        {
            return StopThread(threadName, false);
        }

        public static bool StopThreadNow(String threadName)
        {
            return StopThread(threadName, true);
        }

        private static bool StopThread(String threadName, bool now) 
        {
            if (engine == null) 
            {
                return false;// e.g. not yet started
            }
            bool wasStopped = false;
            // ConcurrentHashMap does not need sync. here
            foreach ( AbstractThreadGroup threadGroup in engine.groups)
            {
                wasStopped = wasStopped || threadGroup.StopThread(threadName, now);
            }
            return wasStopped;
        }

        public StandardEngine() : this(null)
        {
        }

        public StandardEngine(String host) 
        {
            this.host = host;
            // Hack to allow external control
            engine = this;
        }


        public void Configure(HashTree testTree) 
        {
            // Is testplan serialised?
            SearchByType<TestPlan> testPlan = new SearchByType<TestPlan>();
            testTree.Traverse(testPlan);
            List<TestPlan> plan = testPlan.GetSearchResults();
            if (0 == plan.Count)
            {
                throw new Exception("Could not find the TestPlan class!");
            }
            TestPlan tp = (TestPlan) plan[0];
            serialized = tp.isSerialized();
            tearDownOnShutdown = tp.isTearDownOnShutdown();
            active = true;
            test = testTree;
        }

        public void RunTest()
        {
            if (host != null)
            {
                Int64 now = DateTime.Now.Ticks;
                System.Console.WriteLine("Starting the test on host {0}@{1} ({2})", host, DateTime.Now, now);
            }
            try
            {
                Thread runningThread = new Thread(this.Run);
                runningThread.Start();
            } 
            catch (Exception err) 
            {
                StopTest();
                throw new NetMeterEngineException(err.Message);
            }
        }

        private void RemoveThreadGroups(LinkedList<Object> elements) 
        {
            foreach (Object obj in elements)
            {
                if (typeof(AbstractThreadGroup).IsAssignableFrom(obj.GetType())) 
                {
                    elements.Remove(obj);
                } 
                else if (!typeof(TestElement).IsAssignableFrom(obj.GetType())) 
                {
                    elements.Remove(obj);
                }
            }
        }


        private void NotifyTestListenersOfStart(SearchByType<TestStateListener> testListeners) 
        {
            foreach (TestStateListener tl in testListeners.GetSearchResults()) 
            {
                if (host == null) 
                {
                    tl.TestStarted();
                }
                else
                {
                    tl.TestStarted(host);
                }
            }
        }

        private void NotifyTestListenersOfEnd(SearchByType<TestStateListener> testListeners)
        {
            log.Info("Notifying test listeners of end of test");
            foreach (TestStateListener tl in testListeners.GetSearchResults())
            {
                try 
                {
                    if (host == null) 
                    {
                        tl.TestEnded();
                    } 
                    else
                    {
                        tl.TestEnded(host);
                    }
                } 
                catch (Exception e) 
                {
                    log.Warn(String.Format("Error encountered during shutdown of {0}, {1} {2}", tl.ToString(), e.Message));
                }
            }

            if (host != null) 
            {
                log.Info("Test has ended on host "+host);
                Int64 now = DateTime.Now.Ticks;
                System.Console.WriteLine("Finished the test on host {0}@{1} ({2}), {3}", host, DateTime.Now, now, (exitAfterTest ? " - exit requested." : ""));
                if (exitAfterTest)
                {
                    Exit();
                }
            }
            active=false;
        }

        public void Reset() 
        {
            if (running) 
            {
                StopTest();
            }
        }

        public void StopTest() 
        {
            StopTest(true);
        }

        public void StopTest(Boolean now) 
        {
            shutdown = !now;
            StopTestClass stc = new StopTestClass(now);
            Thread stopThread = new Thread(stc.Run);
            stopThread.Start(this);
        }

        protected class StopTestClass
        {
            private Boolean now;

            public StopTestClass(Boolean b) 
            {
                now = b;
            }

            public void Run(Object obj) 
            {
                running = false;
                if (now) 
                {
                    StandardEngine engine = (StandardEngine)obj;
                    engine.TellThreadGroupsToStop();
                    Thread.Sleep(10 * engine.CountActiveThreads());
                    Boolean stopped = engine.VerifyThreadsStopped();
                    if (!stopped) 
                    {  // we totally failed to stop the test
                        // TODO should we call test listeners? That might hang too ...
                        log.Fatal(NetMeterUtils.getResString("stopping_test_failed"));
                        if (SYSTEM_EXIT_ON_STOP_FAIL) 
                        { // default is true
                            log.Fatal("Exitting");
                            System.Console.WriteLine("Fatal error, could not stop test, exitting");

                        } 
                        else
                        {
                            System.Console.WriteLine("Fatal error, could not stop test");                            
                        }
                    } // else will be done by threadFinished()
                } 
                else
                {
                    engine.StopAllThreadGroups();
                }
            }
        }

        public void Run() 
        {
            log.Info("Running the test!");
            running = true;

            NetMeterContextManager.StartTest();
            //try 
            //{
            //    PreCompiler compiler = new PreCompiler();
            //    test.Traverse(compiler);
            //} 
            //catch (Exception ex) 
            //{
            //    log.Error(String.Format("Error occurred compiling the tree: {0}", ex.Message));
            //    return; // no point continuing
            //}
            /**
             * Notification of test listeners needs to happen after function
             * replacement, but before setting RunningVersion to true.
             */
            SearchByType<TestStateListener> testListeners = new SearchByType<TestStateListener>(); // TL - S&E
            test.Traverse(testListeners);

            // Merge in any additional test listeners
            // currently only used by the function parser
            testListeners.GetSearchResults().AddRange(testList);
            testList.Clear(); // no longer needed

            //if (!startListenersLater ) 
            //{ 
            //    NotifyTestListenersOfStart(testListeners); 
            //}
            test.Traverse(new TurnElementsOn());
            //if (startListenersLater) 
            //{ 
            //    NotifyTestListenersOfStart(testListeners); 
            //}

            LinkedList<Object> testLevelElements = new LinkedList<Object>(test.list(test.GetArray()[0]));
            RemoveThreadGroups(testLevelElements);

            SearchByType<AbstractThreadGroup> searcher = new SearchByType<AbstractThreadGroup>();

            test.Traverse(searcher);
        
            TestCompiler.Initialize();

            ListenerNotifier notifier = new ListenerNotifier();

            int groupCount = 0;
            NetMeterContextManager.ClearTotalThreads();        

            /*
             * Here's where the test really starts. Run a Full GC now: it's no harm
             * at all (just delays test start by a tiny amount) and hitting one too
             * early in the test can impair results for short tests.
             */
            NetMeterUtils.helpGC();
        
            NetMeterContextManager.GetContext().SetSamplingStarted(true);
            Boolean mainGroups = running; // still running at this point, i.e. setUp was not cancelled

            while (running)
            {
                foreach (AbstractThreadGroup group in searcher.GetSearchResults())
                {
                    groupCount++;
                    String groupName = group.GetName();
                    log.Info(String.Format("Starting ThreadGroup: {0} : {1}", groupCount, groupName));
                    StartThreadGroup(group, groupCount, searcher, testLevelElements, notifier);
                    if (serialized) 
                    {
                        log.Info(String.Format("Waiting for thread group: {0} to finish before starting next group", groupName));
                        group.WaitThreadsStopped();
                    }
                }
            }

            if (groupCount == 0)
            { // No TGs found
                log.Info("No enabled thread groups found");
            } 
            else
            {
                if (running) 
                {
                    log.Info("All thread groups have been started");
                } 
                else
                {
                    log.Info("Test stopped - no more thread groups will be started");
                }
            }

            //wait for all Test Threads To Exit
            WaitThreadsStopped();
            NotifyTestListenersOfEnd(testListeners);
        }

        /**
         * @return total of active threads in all Thread Groups
         */
        private int CountActiveThreads()
        {
            int reminingThreads= 0;
            foreach (AbstractThreadGroup threadGroup in groups) 
            {
                reminingThreads += threadGroup.NumberOfActiveThreads();
            }            
            return reminingThreads; 
        }
    
        private void StartThreadGroup(AbstractThreadGroup group, int groupCount, SearchByType<AbstractThreadGroup> searcher, LinkedList<Object> testLevelElements, ListenerNotifier notifier)
        {
            int numThreads = group.GetNumThreads();
            NetMeterContextManager.AddTotalThreads(numThreads);
            Boolean onErrorStopTest = group.GetOnErrorStopTest();
            Boolean onErrorStopTestNow = group.GetOnErrorStopTestNow();
            Boolean onErrorStopThread = group.GetOnErrorStopThread();
            Boolean onErrorStartNextLoop = group.GetOnErrorStartNextLoop();
            String groupName = group.GetName();
            log.Info("Starting " + numThreads + " threads for group " + groupName + ".");

            if (onErrorStopTest) {
                log.Info("Test will stop on error");
            } else if (onErrorStopTestNow) {
                log.Info("Test will stop abruptly on error");
            } else if (onErrorStopThread) {
                log.Info("Thread will stop on error");
            } else if (onErrorStartNextLoop) {
                log.Info("Thread will start next loop on error");
            } else {
                log.Info("Thread will continue on error");
            }
            OrderedHashTree threadGroupTree = (OrderedHashTree) searcher.GetSubTree(group);
            threadGroupTree.Put(group, testLevelElements);

            groups.Add(group);
            group.Start(groupCount, notifier, threadGroupTree, this);
        }

        /**
         * @return boolean true if all threads of all Threead Groups stopped
         */
        private Boolean VerifyThreadsStopped()
        {
            Boolean stoppedAll = true;
            // ConcurrentHashMap does not need synch. here
            foreach (AbstractThreadGroup threadGroup in groups)
            {
                stoppedAll = stoppedAll && threadGroup.VerifyThreadsStopped();
            }
            return stoppedAll;
        }

        /**
         * Wait for Group Threads to stop
         */
        private void WaitThreadsStopped() 
        {
            // ConcurrentHashMap does not need synch. here
            foreach (AbstractThreadGroup threadGroup in groups) 
            {
                threadGroup.WaitThreadsStopped();
            }
        }

        /**
         * For each thread group, invoke {@link AbstractThreadGroup#tellThreadsToStop()}
         */
        private void TellThreadGroupsToStop() 
        {
            // ConcurrentHashMap does not need protecting
            foreach (AbstractThreadGroup threadGroup in groups)
            {
                threadGroup.TellThreadsToStop();
            }
        }

        public void AskThreadsToStop() 
        {
            if (engine != null) { // Will be null if StopTest thread has started
                engine.StopTest(false);
            }
        }

        /**
         * For each current thread group, invoke:
         * <ul> 
         * <li>{@link AbstractThreadGroup#stop()} - set stop flag</li>
         * </ul> 
         */
        private void StopAllThreadGroups() 
        {
            // ConcurrentHashMap does not need synch. here
            foreach (AbstractThreadGroup threadGroup in groups)
            {
                threadGroup.Stop();
            }
        }

        // Remote exit
        // Called by RemoteJMeterEngineImpl.rexit()
        // and by notifyTestListenersOfEnd() iff exitAfterTest is true;
        // in turn that is called by the run() method and the StopTest class
        // also called
        public void Exit() 
        {
            //ClientNetMeterEngine.tidyRMI(log); // This should be enough to allow server to exit.
            if (REMOTE_SYSTEM_EXIT) 
            { // default is false
                log.Warn("About to run System.exit(0) on "+host);
                // Needs to be run in a separate thread to allow RMI call to return OK
                Thread t = new Thread
                    ( () => 
                        {
                            Thread.Sleep(1000);
                            log.Info("Bye from " + host);
                            System.Console.WriteLine("Bye from {0}", host);
                        }
                    ); 
                t.Start();
            }
        }

        private void Pause(Int32 ms)
        {
            try 
            {
                Thread.Sleep(ms);
            } 
            catch (Exception e)
            {
            }
        }

        //public void SetProperties(Properties p) 
        //{
        //    log.Info("Applying properties "+p);
        //    NetMeterUtils.getJMeterProperties().putAll(p);
        //}
    
        public Boolean isActive() 
        {
            return active;
        }
    }
}
