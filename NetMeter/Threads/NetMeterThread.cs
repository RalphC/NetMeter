using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using log4net;
using NetMeter.Engine;
using NetMeter.Engine.Event;
using NetMeter.TestElements;
using NetMeter.Control;
using NetMeter.Samplers;
using Valkyrie.Collections;
using NetMeter.Assertions;
using NetMeter.Processor;
using Valkyrie.Logging;
using log4net;

namespace NetMeter.Threads
{
    public class NetMeterThread
    {
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

        public static sealed String PACKAGE_OBJECT = "NetMeterThread.pack"; // $NON-NLS-1$

        public static sealed String LAST_SAMPLE_OK = "NetMeterThread.last_sample_ok"; // $NON-NLS-1$

        private static sealed String TRUE = Boolean.TrueString; // i.e. "true"

        private sealed Controller controller;

        private sealed HashTree testTree;

        private sealed TestCompiler compiler;

        private sealed NetMeterThreadMonitor monitor;

        private sealed NetMeterVariables threadVars;

        // Note: this is only used to implement TestIterationListener#testIterationStart
        // Since this is a frequent event, it makes sense to create the list once rather than scanning each time
        // The memory used will be released when the thread finishes
        protected sealed List<TestIterationListener> testIterationStartListeners;

        private sealed ListenerNotifier notifier;

        /*
         * The following variables are set by StandardJMeterEngine.
         * This is done before start() is called, so the values will be published to the thread safely
         * TODO - consider passing them to the constructor, so that they can be made final
         * (to avoid adding lots of parameters, perhaps have a parameter wrapper object.
         */
        private String threadName;

        private int initialDelay = 0;

        private int threadNum = 0;

        private long startTime = 0;

        private long endTime = 0;

        private Boolean scheduler = false;
        // based on this scheduler is enabled or disabled

        // Gives access to parent thread threadGroup
        private AbstractThreadGroup threadGroup;

        private StandardEngine engine = null; // For access to stop methods.

        /*
         * The following variables may be set/read from multiple threads.
         */
        private volatile Boolean running; // may be set from a different thread

        private volatile Boolean onErrorStopTest;

        private volatile Boolean onErrorStopTestNow;

        private volatile Boolean onErrorStopThread;

        private volatile Boolean onErrorStartNextLoop;

        private volatile TestAgent currentSampler;

        private sealed object interruptLock = new object(); // ensure that interrupt cannot overlap with shutdown

        public NetMeterThread(HashTree test, NetMeterThreadMonitor monitor, ListenerNotifier note) 
        {
            this.monitor = monitor;
            threadVars = new NetMeterVariables();
            testTree = test;
            compiler = new TestCompiler(testTree);
            controller = (Controller) testTree.GetArray()[0];
            SearchByType<TestIterationListener> threadListenerSearcher = new SearchByType<TestIterationListener>();
            test.Traverse(threadListenerSearcher);
            testIterationStartListeners = threadListenerSearcher.GetSearchResults();
            notifier = note;
            running = true;
        }

        public void SetInitialContext(NetMeterContext context) 
        {
            threadVars.PutAll(context.GetVariables());
        }

        /**
         * Enable the scheduler for this JMeterThread.
         */
        public void setScheduled(Boolean sche) 
        {
            this.scheduler = sche;
        }

        /**
         * Set the StartTime for this Thread.
         *
         * @param stime the StartTime value.
         */
        public void SetStartTime(long stime) 
        {
            startTime = stime;
        }

        /**
         * Get the start time value.
         *
         * @return the start time value.
         */
        public Int64 GetStartTime() 
        {
            return startTime;
        }

        /**
         * Set the EndTime for this Thread.
         *
         * @param etime
         *            the EndTime value.
         */
        public void SetEndTime(long etime) 
        {
            endTime = etime;
        }

        /**
         * Get the end time value.
         *
         * @return the end time value.
         */
        public Int64 GetEndTime() 
        {
            return endTime;
        }

        /**
         * Check the scheduled time is completed.
         *
         */
        private void StopScheduler() 
        {
            Int64 now = (Int64)DateTime.Now.TimeOfDay.TotalMilliseconds;
            Int64 delay = now - endTime;
            if ((delay >= 0)) 
            {
                running = false;
            }
        }

        /**
         * Wait until the scheduled start time if necessary
         *
         */
        private void StartScheduler() 
        {
            Int64 delay = (startTime - (Int64)DateTime.Now.TimeOfDay.TotalMilliseconds);
            // delayBy(delay, "startScheduler");
        }

        public void SetThreadName(String threadName) 
        {
            this.threadName = threadName;
        }

        /*
         * See below for reason for this change. Just in case this causes problems,
         * allow the change to be backed out
         */
        private static sealed Boolean startEarlier = true; // $NON-NLS-1$

        private static sealed Boolean reversePostProcessors = false; // $NON-NLS-1$

        public void Run() 
        {
            // threadContext is not thread-safe, so keep within thread
            NetMeterContext threadContext = NetMeterContextManager.GetContext();
            LoopIterationListener iterationListener = null;

            try 
            {
                iterationListener = InitRun(threadContext);
                while (running)
                {
                    TestAgent sam = (TestAgent)controller.next();
                    while (running && sam != null) 
                    {
                	    ProcessTestAgent(sam, null, threadContext);
                	    threadContext.CleanAfterExecute();
                        if (onErrorStartNextLoop || threadContext.restartNextLoop) 
                        {
                            if (threadContext.restartNextLoop)
                            {
                                TriggerEndOfLoopOnParentControllers(sam, threadContext);
                                sam = null;
                                threadContext.GetVariables().Add(LAST_SAMPLE_OK, TRUE);
                                threadContext.restartNextLoop = false;
                	        } 
                            else 
                            {
                    		    Boolean lastSampleFailed = !TRUE.Equals(threadContext.GetVariables().Get(LAST_SAMPLE_OK));
                    		    if(lastSampleFailed) 
                                {
//    	                		    if(log.isDebugEnabled()) 
//                                    {
//    	                    		    log.debug("StartNextLoop option is on, Last sample failed, starting next loop");
//    	                    	    }
    	                    	    TriggerEndOfLoopOnParentControllers(sam, threadContext);
    	                            sam = null;
    	                            threadContext.GetVariables().Add(LAST_SAMPLE_OK, TRUE);
                    		    } 
                                else
                                {
                                    sam = (TestAgent)controller.next();
                    		    }
                	        }
                	    } 
                	    else 
                        {
                		    sam = (TestAgent)controller.next();
                	    }
                    }
                    if (controller.isDone())
                    {
                        running = false;
                    }
                }
            }
            // Might be found by contoller.next()
            //catch (NetMeterStopTestException e) 
            //{
            //    log.info("Stopping Test: " + e.toString());
            //    stopTest();
            //}
            //catch (JMeterStopTestNowException e)
            //{
            //    log.info("Stopping Test Now: " + e.toString());
            //    stopTestNow();
            //} 
            //catch (JMeterStopThreadException e)
            //{
            //    log.info("Stop Thread seen: " + e.toString());
            //} 
            catch (Exception ex)
            {
                log.Error("Test failed!", ex);
            } 
            //catch (ThreadDeath e) 
            //{
            //    throw e; // Must not ignore this one
            //} 
            finally 
            {
                currentSampler = null; // prevent any further interrupts
                try 
                {
                    Monitor.Enter(interruptLock);  // make sure current interrupt is finished, prevent another starting yet
                    threadContext.Clear();
//                    log.info("Thread finished: " + threadName);
                    ThreadFinished(iterationListener);
                    monitor.ThreadFinished(this); // Tell the monitor we are done
                    NetMeterContextManager.RemoveContext(); // Remove the ThreadLocal entry
                }
                finally 
                {
                    Monitor.Exit(interruptLock); // Allow any pending interrupt to complete (OK because currentSampler == null)
                }
            }
        }

        /**
         * Trigger end of loop on parent controllers up to Thread Group
         * @param sam Sampler Base sampler
         * @param threadContext 
         */
        private void TriggerEndOfLoopOnParentControllers(TestAgent sam, NetMeterContext threadContext) 
        {
            // Find parent controllers of current sampler
            //FindTestElementsUpToRootTraverser pathToRootTraverser=null;
            //TransactionSampler transactionSampler = null;
            //if(sam is TransactionSampler) 
            //{
            //    transactionSampler = (TransactionSampler) sam;
            //    pathToRootTraverser = new FindTestElementsUpToRootTraverser((transactionSampler).getTransactionController());
            //} 
            //else 
            //{
            //    pathToRootTraverser = new FindTestElementsUpToRootTraverser(sam);
            //}
            //testTree.Traverse(pathToRootTraverser);
            //List<Controller> controllersToReinit = pathToRootTraverser.getControllersToRoot();
  	
            //// Trigger end of loop condition on all parent controllers of current sampler
            //foreach (Controller cont in controllersToReinit)
            //{
            //    Controller parentController = cont;
            //    if (parentController is AbstractThreadGroup)
            //    {
            //        AbstractThreadGroup tg = (AbstractThreadGroup)parentController;
            //        tg.StartNextLoop();
            //    }
            //    else
            //    {
            //        parentController.triggerEndOfLoop();
            //    }
            //}
            //if(transactionSampler!=null) 
            //{
            //    Process_sampler(transactionSampler, null, threadContext);
            //}
        }

        /**
         * Process the current sampler, handling transaction samplers.
         *
         * @param current sampler
         * @param parent sampler
         * @param threadContext
         * @return SampleResult if a transaction was processed
         */
        private ExecuteResult ProcessTestAgent(TestAgent current, TestAgent parent, NetMeterContext threadContext) 
        {
            ExecuteResult transactionResult = null;
            try 
            {
                // Check if we have a sampler to sample
                if(current != null)
                {
                    threadContext.SetCurrentSampler(current);
                    // Get the sampler ready to sample
                    ExecutionPackage pack = compiler.ConfigureSampler(current);
                    // runPreProcessors(pack.getPreProcessors());

                    // Hack: save the package for any transaction controllers
                    threadVars.PutObject(PACKAGE_OBJECT, pack);

                    //delay(pack.getTimers());
                    TestAgent sampler = pack.GetSampler();
                    sampler.SetThreadContext(threadContext);
                    // TODO should this set the thread names for all the subsamples?
                    // might be more efficient than fetching the name elsewehere
                    sampler.SetThreadName(threadName);
                    // TestBeanHelper.prepare(sampler);

                    // Perform the actual sample
                    currentSampler = sampler;
                    ExecuteResult result = sampler.Execute(null);
                    currentSampler = null;
                    // TODO: remove this useless Entry parameter

                    // If we got any results, then perform processing on the result
                    if (result != null) 
                    {
                        result.SetGroupThreads(threadGroup.GetNumberOfThreads());
                        result.SetAllThreads(NetMeterContextManager.GetNumberOfThreads());
                        result.SetThreadName(threadName);
                        threadContext.SetPreviousResult(result);
                        RunPostProcessors(pack.GetPostProcessors());
                        CheckTestAssertions(pack.GetAssertions(), result, threadContext);
                        // Do not send subsamples to listeners which receive the transaction sample
                        List<ExecutionListener> sampleListeners = GetSampleListeners(pack);
                        NotifyListeners(sampleListeners, result);
                        compiler.Done(pack);

                        // Check if thread or test should be stopped
                        if (result.isStopThread() || (!result.Success && onErrorStopThread)) 
                        {
                            StopThread();
                        }
                        if (result.isStopTest() || (!result.Success && onErrorStopTest)) 
                        {
                            StopTest();
                        }
                        if (result.isStopTestNow() || (!result.Success && onErrorStopTestNow)) 
                        {
                            StopTestNow();
                        }
                        if(result.isStartNextThreadLoop()) 
                        {
                            threadContext.restartNextLoop = true;
                        }
                    } 
                    else 
                    {
                        compiler.Done(pack); // Finish up
                    }
                }
                if (scheduler) 
                {
                    // checks the scheduler to stop the iteration
                    StopScheduler();
                }
            } 
            catch (Exception e) 
            {
                if (current != null) 
                {
                     log.Error("Error while processing sampler '"+current.GetName()+"' :", e);
                } 
                else 
                {
                     log.Error("", e);
                }
                StopThread();
            }
            return transactionResult;
        }

        /**
         * Get the SampleListeners for the sampler. Listeners who receive transaction sample
         * will not be in this list.
         *
         * @param samplePack
         * @param transactionPack
         * @param transactionSampler
         * @return the listeners who should receive the sample result
         */
        private List<ExecutionListener> GetSampleListeners(ExecutionPackage samplePack) 
        {
            return samplePack.GetSampleListeners();
            //List<SampleListener> sampleListeners = samplePack.GetSampleListeners();
            //// Do not send subsamples to listeners which receive the transaction sample
            //if(transactionSampler != null) 
            //{
            //    List<SampleListener> onlySubSamplerListeners = new List<SampleListener>();
            //    List<SampleListener> transListeners = transactionPack.GetSampleListeners();
            //    foreach(SampleListener listener in sampleListeners) {
            //        // Check if this instance is present in transaction listener list
            //        Boolean found = false;
            //        foreach(SampleListener trans in transListeners) 
            //        {
            //            // Check for the same instance
            //            if(trans == listener) 
            //            {
            //                found = true;
            //                break;
            //            }
            //        }
            //        if(!found) {
            //            onlySubSamplerListeners.Add(listener);
            //        }
            //    }
            //    sampleListeners = onlySubSamplerListeners;
            //}
            //return sampleListeners;
        }

        /**
         * @param threadContext
         * @return 
         *
         */
        private IterationListener InitRun(NetMeterContext threadContext) 
        {
            threadContext.SetVariables(threadVars);
            threadContext.SetThreadNum(getThreadNum());
            threadContext.GetVariables().Add(LAST_SAMPLE_OK, TRUE);
            threadContext.SetThread(this);
            threadContext.SetThreadGroup(threadGroup);
            threadContext.SetEngine(engine);
            testTree.Traverse(compiler);
            // log.info("Thread started: " + Thread.currentThread().getName());
            /*
             * Setting SamplingStarted before the contollers are initialised allows
             * them to access the running values of functions and variables (however
             * it does not seem to help with the listeners)
             */
            if (startEarlier) 
            {
                threadContext.SetSamplingStarted(true);
            }
            controller.Initialize();
            IterationListener iterationListener = new IterationListener();
            controller.addIterationListener(iterationListener);
            if (!startEarlier) 
            {
                threadContext.SetSamplingStarted(true);
            }
            ThreadStarted();
            return iterationListener;
        }

        private void ThreadStarted() 
        {
            NetMeterContextManager.IncrNumberOfThreads();
            threadGroup.IncrNumberOfThreads();
            ThreadListenerTraverser startup = new ThreadListenerTraverser(true);
            testTree.Traverse(startup); // call ThreadListener.threadStarted()
        }

        private void ThreadFinished(LoopIterationListener iterationListener) 
        {
            ThreadListenerTraverser shut = new ThreadListenerTraverser(false);
            testTree.Traverse(shut); // call ThreadListener.threadFinished()
            NetMeterContextManager.DecrNumberOfThreads();
            threadGroup.DecrNumberOfThreads();
            if (iterationListener != null)
            { // probably not possible, but check anyway
                controller.RemoveIterationListener(iterationListener);
            }
        }

        // N.B. This is only called at the start and end of a thread, so there is not
        // necessary to cache the search results, thus saving memory
        private class ThreadListenerTraverser : HashTreeTraverser 
        {
            private sealed Boolean isStart;

            public ThreadListenerTraverser(Boolean start) 
            {
                isStart = start;
            }

            public void AddNode(Object node, HashTree subTree)
            {
                if (node is ThreadListener) 
                {
                    ThreadListener tl = (ThreadListener) node;
                    if (isStart) 
                    {
                        tl.threadStarted();
                    } 
                    else 
                    {
                        tl.threadFinished();
                    }
                }
            }

            public void subtractNode() 
            {
            }

            public void processPath() 
            {
            }
        }

        public String GetThreadName()
        {
            return threadName;
        }

        public void Stop() 
        { // Called by StandardJMeterEngine, TestAction and AccessLogSampler
            running = false;
            // log.info("Stopping: " + threadName);
        }

        /** {@inheritDoc} */
        public Boolean interrupt()
        {
            try 
            {
                Monitor.Enter(interruptLock);
                TestAgent samp = currentSampler; // fetch once; must be done under lock
                if (samp is Interruptible)
                { // (also protects against null)
//                   log.warn("Interrupting: " + threadName + " sampler: " +samp.getName());
                    try 
                    {
                        Boolean found = ((Interruptible)samp).interrupt();
                        if (!found) 
                        {
//                            log.warn("No operation pending");
                        }
                        return found;
                    } 
                    catch (Exception e) 
                    {
//                        log.warn("Caught Exception interrupting sampler: "+e.toString());
                    }
                } 
                else if (samp != null)
                {
//                    log.warn("Sampler is not Interruptible: "+samp.getName());
                }
            } 
            finally 
            {
                Monitor.Exit(interruptLock);            
            }
            return false;
        }

        private void StopTest()
        {
            running = false;
//            log.info("Stop Test detected by thread: " + threadName);
            if (engine != null) 
            {
                engine.AskThreadsToStop();
            }
        }

        private void StopTestNow()
        {
            running = false;
            // log.info("Stop Test Now detected by thread: " + threadName);
            if (engine != null) 
            {
                engine.StopTest();
            }
        }

        private void StopThread() 
        {
            running = false;
            // log.info("Stop Thread detected by thread: " + threadName);
        }

        private void CheckTestAssertions(List<Assertion> assertions, ExecuteResult parent, NetMeterContext threadContext) 
        {
            foreach (Assertion assertion in assertions) 
            {
                //TestBeanHelper.prepare((TestElement) assertion);
                if (assertion is AbstractScopedAssertion)
                {
                    AbstractScopedAssertion scopedAssertion = (AbstractScopedAssertion) assertion;
                    String scope = scopedAssertion.fetchScope();
                    if (scopedAssertion.isScopeParent(scope) || scopedAssertion.isScopeAll(scope) || scopedAssertion.isScopeVariable(scope))
                    {
                        ProcessTestAssertion(parent, assertion);
                    }
                    if (scopedAssertion.isScopeChildren(scope) || scopedAssertion.isScopeAll(scope))
                    {
                        ExecuteResult[] children = parent.getSubResults();
                        Boolean childError = false;
                        foreach (ExecuteResult child in children)
                        {
                            ProcessTestAssertion(child, assertion);
                            if (!child.Success)
                            {
                                childError = true;
                            }
                        }
                        // If parent is OK, but child failed, add a message and flag the parent as failed
                        if (childError && parent.Success) 
                        {
                            AssertionResult assertionResult = new AssertionResult(((AbstractTestElement)assertion).GetName());
                            assertionResult.setResultForFailure("One or more sub-samples failed");
                            parent.addAssertionResult(assertionResult);
                            parent.Success = false;
                        }
                    }
                } 
                else 
                {
                    ProcessTestAssertion(parent, assertion);
                }
            }
            threadContext.GetVariables().Add(LAST_SAMPLE_OK, parent.Success.ToString());
        }

        private void ProcessTestAssertion(ExecuteResult result, Assertion assertion) 
        {
            AssertionResult assertionResult;
            try
            {
                assertionResult = assertion.GetResult(result);
            } 
            //catch (ThreadDeath e) 
            //{
            //    throw e;
            //} 
            //catch (Error e) 
            //{
            //    log.error("Error processing Assertion ",e);
            //    assertionResult = new AssertionResult("Assertion failed! See log file.");
            //    assertionResult.setError(true);
            //    assertionResult.setFailureMessage(e.toString());
            //} 
            catch (Exception ex) 
            {
                //log.error("Exception processing Assertion ",ex);
                assertionResult = new AssertionResult("Assertion failed! See log file.");
                assertionResult.setError(true);
                assertionResult.setFailureMessage(ex.Message);
            }
            result.Success = result.Success && !(assertionResult.isError() || assertionResult.isFailure());
            result.addAssertionResult(assertionResult);
        }

        private void RunPostProcessors(List<PostProcessor> extractors) 
        {
            foreach (PostProcessor ex in extractors) 
            {
                //TestBeanHelper.prepare((TestElement) ex);
                ex.process();
            }
        }

        public void NotifyTestListeners() 
        {
            threadVars.IncIteration();
            foreach (TestIterationListener listener in testIterationStartListeners) 
            {
                if (listener is TestElement) 
                {
                    listener.testIterationStart(new LoopIterationEvent((TestElement)controller, threadVars.GetIteration()));
                    ((TestElement) listener).RecoverRunningVersion();
                }
                else 
                {
                    listener.testIterationStart(new LoopIterationEvent((TestElement)controller, threadVars.GetIteration()));
                }
            }
        }

        private void NotifyListeners(List<ExecutionListener> listeners, ExecuteResult result) 
        {
            ExecutionEvent sampleEvent = new ExecutionEvent(result, threadGroup.GetName(), threadVars);
            notifier.notifyListeners(sampleEvent, listeners);
        }

        /**
         * Set rampup delay for JMeterThread Thread
         * @param delay Rampup delay for JMeterThread
         */
        public void SetInitialDelay(int delay)
        {
            initialDelay = delay;
        }

        /**
         * Initial delay if ramp-up period is active for this threadGroup.
         */
        private void rampUpDelay() 
        {
            //delayBy(initialDelay, "RampUp");
        }

        /**
         * Wait for delay with RAMPUP_GRANULARITY
         * @param delay delay in ms
         * @param type Delay type
         */
        //protected sealed void delayBy(long delay, String type) {
        //    if (delay > 0) 
        //    {
        //        long start = System.currentTimeMillis();
        //        long end = start + delay;
        //        long now=0;
        //        long pause = RAMPUP_GRANULARITY;
        //        while(running && (now = System.currentTimeMillis()) < end) 
        //        {
        //            long togo = end - now;
        //            if (togo < pause)
        //            {
        //                pause = togo;
        //            }
        //            try 
        //            {
        //                Thread.sleep(pause); // delay between checks
        //            } 
        //            catch (InterruptedException e)
        //            {
        //                if (running)
        //                { // Don't bother reporting stop test interruptions
        //                    log.warn(type+" delay for "+threadName+" was interrupted. Waited "+(now - start)+" milli-seconds out of "+delay);
        //                }
        //                break;
        //            }
        //        }
        //    }
        //}

        /**
         * Returns the threadNum.
         */
        public Int32 getThreadNum()
        {
            return threadNum;
        }

        /**
         * Sets the threadNum.
         *
         * @param threadNum
         *            the threadNum to set
         */
        public void SetThreadNum(int threadNum) 
        {
            this.threadNum = threadNum;
        }

        private class IterationListener : LoopIterationListener 
        {
            /**
             * {@inheritDoc}
             */
            public void IterationStart(LoopIterationEvent iterEvent)
            {
                NotifyTestListeners();
            }
        }

        /**
         * Save the engine instance for access to the stop methods
         *
         * @param engine
         */
        public void SetEngine(StandardEngine engine)
        {
            this.engine = engine;
        }

        /**
         * Should Test stop on sampler error?
         *
         * @param b -
         *            true or false
         */
        public void SetOnErrorStopTest(Boolean b) 
        {
            onErrorStopTest = b;
        }

        /**
         * Should Test stop abruptly on sampler error?
         *
         * @param b -
         *            true or false
         */
        public void SetOnErrorStopTestNow(Boolean b)
        {
            onErrorStopTestNow = b;
        }

        /**
         * Should Thread stop on Sampler error?
         *
         * @param b -
         *            true or false
         */
        public void SetOnErrorStopThread(Boolean b)
        {
            onErrorStopThread = b;
        }

        /**
         * Should Thread start next loop on Sampler error?
         *
         * @param b -
         *            true or false
         */
        public void SetOnErrorStartNextLoop(Boolean b)
        {
            onErrorStartNextLoop = b;
        }

        public void SetThreadGroup(AbstractThreadGroup group)
        {
            this.threadGroup = group;
        }
    }
}
