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

namespace NetMeter.Threads
{
    public class NetMeterThread
    {
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
        private sealed List<TestIterationListener> testIterationStartListeners;

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

        private StandardNetMeterEngine engine = null; // For access to stop methods.

        /*
         * The following variables may be set/read from multiple threads.
         */
        private volatile Boolean running; // may be set from a different thread

        private volatile Boolean onErrorStopTest;

        private volatile Boolean onErrorStopTestNow;

        private volatile Boolean onErrorStopThread;

        private volatile Boolean onErrorStartNextLoop;

        private volatile Sampler currentSampler;

        private sealed object interruptLock = new object(); // ensure that interrupt cannot overlap with shutdown

        public NetMeterThread(HashTree test, NetMeterThreadMonitor monitor, ListenerNotifier note) 
        {
            this.monitor = monitor;
            threadVars = new NetMeterVariables();
            testTree = test;
            compiler = new TestCompiler(testTree);
            controller = (Controller) testTree.getArray()[0];
            SearchByType<TestIterationListener> threadListenerSearcher = new SearchByType<TestIterationListener>();
            test.traverse(threadListenerSearcher);
            testIterationStartListeners = threadListenerSearcher.getSearchResults();
            notifier = note;
            running = true;
        }

        public void setInitialContext(NetMeterContext context) 
        {
            threadVars.putAll(context.getVariables());
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
        public void setStartTime(long stime) 
        {
            startTime = stime;
        }

        /**
         * Get the start time value.
         *
         * @return the start time value.
         */
        public Int64 getStartTime() 
        {
            return startTime;
        }

        /**
         * Set the EndTime for this Thread.
         *
         * @param etime
         *            the EndTime value.
         */
        public void setEndTime(long etime) 
        {
            endTime = etime;
        }

        /**
         * Get the end time value.
         *
         * @return the end time value.
         */
        public Int64 getEndTime() 
        {
            return endTime;
        }

        /**
         * Check the scheduled time is completed.
         *
         */
        private void stopScheduler() 
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
        private void startScheduler() 
        {
            Int64 delay = (startTime - (Int64)DateTime.Now.TimeOfDay.TotalMilliseconds);
            // delayBy(delay, "startScheduler");
        }

        public void setThreadName(String threadName) 
        {
            this.threadName = threadName;
        }

        /*
         * See below for reason for this change. Just in case this causes problems,
         * allow the change to be backed out
         */
//        private static sealed Boolean startEarlier = JMeterUtils.getPropDefault("jmeterthread.startearlier", true); // $NON-NLS-1$

//        private static sealed Boolean reversePostProcessors = JMeterUtils.getPropDefault("jmeterthread.reversePostProcessors",false); // $NON-NLS-1$

        public void run() 
        {
            // threadContext is not thread-safe, so keep within thread
            NetMeterContext threadContext = NetMeterContextManager.getContext();
            LoopIterationListener iterationListener = null;

            try 
            {
                iterationListener = initRun(threadContext);
                while (running)
                {
                    Sampler sam = (Sampler)controller.next();
                    while (running && sam != null) 
                    {
                	    process_sampler(sam, null, threadContext);
                	    threadContext.cleanAfterSample();
                	    if(onErrorStartNextLoop || threadContext.isRestartNextLoop()) 
                        {
                	        if(threadContext.isRestartNextLoop())
                            {
                                triggerEndOfLoopOnParentControllers(sam, threadContext);
                                sam = null;
                                threadContext.getVariables().Add(LAST_SAMPLE_OK, TRUE);
                                threadContext.setRestartNextLoop(false);
                	        } 
                            else 
                            {
                    		    Boolean lastSampleFailed = !TRUE.Equals(threadContext.getVariables().get(LAST_SAMPLE_OK));
                    		    if(lastSampleFailed) 
                                {
//    	                		    if(log.isDebugEnabled()) 
//                                    {
//    	                    		    log.debug("StartNextLoop option is on, Last sample failed, starting next loop");
//    	                    	    }
    	                    	    triggerEndOfLoopOnParentControllers(sam, threadContext);
    	                            sam = null;
    	                            threadContext.getVariables().Add(LAST_SAMPLE_OK, TRUE);
                    		    } 
                                else
                                {
                                    sam = (Sampler)controller.next();
                    		    }
                	        }
                	    } 
                	    else 
                        {
                		    sam = (Sampler)controller.next();
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
            //catch (Exception e) 
            //{
            //    log.error("Test failed!", e);
            //} 
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
                    threadContext.clear();
//                    log.info("Thread finished: " + threadName);
                    threadFinished(iterationListener);
                    monitor.threadFinished(this); // Tell the monitor we are done
                    NetMeterContextManager.removeContext(); // Remove the ThreadLocal entry
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
        private void triggerEndOfLoopOnParentControllers(Sampler sam, NetMeterContext threadContext) 
        {
            // Find parent controllers of current sampler
            FindTestElementsUpToRootTraverser pathToRootTraverser=null;
            TransactionSampler transactionSampler = null;
            if(sam is TransactionSampler) 
            {
                transactionSampler = (TransactionSampler) sam;
                pathToRootTraverser = new FindTestElementsUpToRootTraverser((transactionSampler).getTransactionController());
            } 
            else 
            {
                pathToRootTraverser = new FindTestElementsUpToRootTraverser(sam);
            }
            testTree.traverse(pathToRootTraverser);
            List<Controller> controllersToReinit = pathToRootTraverser.getControllersToRoot();
  	
            // Trigger end of loop condition on all parent controllers of current sampler
            foreach (Controller cont in controllersToReinit)
            {
                Controller parentController = cont;
                if (parentController is AbstractThreadGroup)
                {
                    AbstractThreadGroup tg = (AbstractThreadGroup)parentController;
                    tg.startNextLoop();
                }
                else
                {
                    parentController.triggerEndOfLoop();
                }
            }
            if(transactionSampler!=null) 
            {
                process_sampler(transactionSampler, null, threadContext);
            }
        }

        /**
         * Process the current sampler, handling transaction samplers.
         *
         * @param current sampler
         * @param parent sampler
         * @param threadContext
         * @return SampleResult if a transaction was processed
         */
        private SampleResult process_sampler(Sampler current, Sampler parent, NetMeterContext threadContext) 
        {
            SampleResult transactionResult = null;
            try 
            {
                // Check if we have a sampler to sample
                if(current != null)
                {
                    threadContext.setCurrentSampler(current);
                    // Get the sampler ready to sample
                    SamplePackage pack = compiler.configureSampler(current);
                    // runPreProcessors(pack.getPreProcessors());

                    // Hack: save the package for any transaction controllers
                    threadVars.putObject(PACKAGE_OBJECT, pack);

                    //delay(pack.getTimers());
                    Sampler sampler = pack.getSampler();
                    sampler.setThreadContext(threadContext);
                    // TODO should this set the thread names for all the subsamples?
                    // might be more efficient than fetching the name elsewehere
                    sampler.setThreadName(threadName);
                    // TestBeanHelper.prepare(sampler);

                    // Perform the actual sample
                    currentSampler = sampler;
                    SampleResult result = sampler.sample(null);
                    currentSampler = null;
                    // TODO: remove this useless Entry parameter

                    // If we got any results, then perform processing on the result
                    if (result != null) 
                    {
                        result.setGroupThreads(threadGroup.getNumberOfThreads());
                        result.setAllThreads(NetMeterContextManager.getNumberOfThreads());
                        result.setThreadName(threadName);
                        threadContext.setPreviousResult(result);
                        runPostProcessors(pack.getPostProcessors());
                        checkAssertions(pack.getAssertions(), result, threadContext);
                        // Do not send subsamples to listeners which receive the transaction sample
                        List<SampleListener> sampleListeners = getSampleListeners(pack, transactionPack, transactionSampler);
                        notifyListeners(sampleListeners, result);
                        compiler.done(pack);

                        // Check if thread or test should be stopped
                        if (result.isStopThread() || (!result.isSuccessful() && onErrorStopThread)) 
                        {
                            stopThread();
                        }
                        if (result.isStopTest() || (!result.isSuccessful() && onErrorStopTest)) 
                        {
                            stopTest();
                        }
                        if (result.isStopTestNow() || (!result.isSuccessful() && onErrorStopTestNow)) 
                        {
                            stopTestNow();
                        }
                        if(result.isStartNextThreadLoop()) 
                        {
                            threadContext.setRestartNextLoop(true);
                        }
                    } 
                    else 
                    {
                        compiler.done(pack); // Finish up
                    }
                }
                if (scheduler) 
                {
                    // checks the scheduler to stop the iteration
                    stopScheduler();
                }
            } 
            catch (JMeterStopTestException e) 
            {
                // log.info("Stopping Test: " + e.toString());
                stopTest();
            }
            catch (JMeterStopThreadException e)
            {
                // log.info("Stopping Thread: " + e.toString());
                stopThread();
            }
            catch (Exception e) 
            {
                if (current != null) 
                {
                    // log.error("Error while processing sampler '"+current.getName()+"' :", e);
                } 
                else 
                {
                    // log.error("", e);
                }
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
        private List<SampleListener> getSampleListeners(SamplePackage samplePack, SamplePackage transactionPack, TransactionSampler transactionSampler) 
        {
            List<SampleListener> sampleListeners = samplePack.getSampleListeners();
            // Do not send subsamples to listeners which receive the transaction sample
            if(transactionSampler != null) 
            {
                List<SampleListener> onlySubSamplerListeners = new List<SampleListener>();
                List<SampleListener> transListeners = transactionPack.getSampleListeners();
                foreach(SampleListener listener in sampleListeners) {
                    // Check if this instance is present in transaction listener list
                    Boolean found = false;
                    foreach(SampleListener trans in transListeners) 
                    {
                        // Check for the same instance
                        if(trans == listener) 
                        {
                            found = true;
                            break;
                        }
                    }
                    if(!found) {
                        onlySubSamplerListeners.Add(listener);
                    }
                }
                sampleListeners = onlySubSamplerListeners;
            }
            return sampleListeners;
        }

        /**
         * @param threadContext
         * @return 
         *
         */
        private IterationListener initRun(NetMeterContext threadContext) 
        {
            threadContext.setVariables(threadVars);
            threadContext.setThreadNum(getThreadNum());
            threadContext.getVariables().Add(LAST_SAMPLE_OK, TRUE);
            threadContext.setThread(this);
            threadContext.setThreadGroup(threadGroup);
            threadContext.setEngine(engine);
            testTree.traverse(compiler);
            // listeners = controller.getListeners();
            if (scheduler) 
            {
                // set the scheduler to start
                startScheduler();
            }
            rampUpDelay(); // TODO - how to handle thread stopped here
            // log.info("Thread started: " + Thread.currentThread().getName());
            /*
             * Setting SamplingStarted before the contollers are initialised allows
             * them to access the running values of functions and variables (however
             * it does not seem to help with the listeners)
             */
            if (startEarlier) 
            {
                threadContext.setSamplingStarted(true);
            }
            controller.initialize();
            IterationListener iterationListener = new IterationListener();
            controller.addIterationListener(iterationListener);
            if (!startEarlier) 
            {
                threadContext.setSamplingStarted(true);
            }
            threadStarted();
            return iterationListener;
        }

        private void threadStarted() 
        {
            NetMeterContextManager.incrNumberOfThreads();
            threadGroup.incrNumberOfThreads();
            GuiPackage gp =GuiPackage.getInstance();
            if (gp != null) 
            {// check there is a GUI
                gp.getMainFrame().updateCounts();
            }
            ThreadListenerTraverser startup = new ThreadListenerTraverser(true);
            testTree.traverse(startup); // call ThreadListener.threadStarted()
        }

        private void threadFinished(LoopIterationListener iterationListener) 
        {
            ThreadListenerTraverser shut = new ThreadListenerTraverser(false);
            testTree.traverse(shut); // call ThreadListener.threadFinished()
            NetMeterContextManager.decrNumberOfThreads();
            threadGroup.decrNumberOfThreads();
            GuiPackage gp = GuiPackage.getInstance();
            if (gp != null)
            {// check there is a GUI
                gp.getMainFrame().updateCounts();
            }
            if (iterationListener != null)
            { // probably not possible, but check anyway
                controller.removeIterationListener(iterationListener);
            }
        }

        // N.B. This is only called at the start and end of a thread, so there is not
        // necessary to cache the search results, thus saving memory
        private static class ThreadListenerTraverser : HashTreeTraverser 
        {
            private sealed Boolean isStart;

            private ThreadListenerTraverser(Boolean start) 
            {
                isStart = start;
            }

            public void addNode(Object node, HashTree subTree)
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

        public String getThreadName()
        {
            return threadName;
        }

        public void stop() 
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
                Sampler samp = currentSampler; // fetch once; must be done under lock
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

        private void stopTest()
        {
            running = false;
//            log.info("Stop Test detected by thread: " + threadName);
            if (engine != null) 
            {
                engine.askThreadsToStop();
            }
        }

        private void stopTestNow()
        {
            running = false;
            // log.info("Stop Test Now detected by thread: " + threadName);
            if (engine != null) 
            {
                engine.stopTest();
            }
        }

        private void stopThread() 
        {
            running = false;
            // log.info("Stop Thread detected by thread: " + threadName);
        }

        private void checkAssertions(List<Assertion> assertions, SampleResult parent, NetMeterContext threadContext) 
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
                        processAssertion(parent, assertion);
                    }
                    if (scopedAssertion.isScopeChildren(scope) || scopedAssertion.isScopeAll(scope))
                    {
                        SampleResult[] children = parent.getSubResults();
                        Boolean childError = false;
                        foreach (SampleResult child in children)
                        {
                            processAssertion(child, assertion);
                            if (!child.isSuccessful())
                            {
                                childError = true;
                            }
                        }
                        // If parent is OK, but child failed, add a message and flag the parent as failed
                        if (childError && parent.isSuccessful()) 
                        {
                            AssertionResult assertionResult = new AssertionResult(((AbstractTestElement)assertion).getName());
                            assertionResult.setResultForFailure("One or more sub-samples failed");
                            parent.addAssertionResult(assertionResult);
                            parent.setSuccessful(false);
                        }
                    }
                } 
                else 
                {
                    processAssertion(parent, assertion);
                }
            }
            threadContext.getVariables().put(LAST_SAMPLE_OK, Boolean.toString(parent.isSuccessful()));
        }

        private void processAssertion(SampleResult result, Assertion assertion) 
        {
            AssertionResult assertionResult;
            try
            {
                assertionResult = assertion.getResult(result);
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
                log.error("Exception processing Assertion ",ex);
                assertionResult = new AssertionResult("Assertion failed! See log file.");
                assertionResult.setError(true);
                assertionResult.setFailureMessage(ex.Message);
            }
            result.setSuccessful(result.isSuccessful() && !(assertionResult.isError() || assertionResult.isFailure()));
            result.addAssertionResult(assertionResult);
        }

        private void runPostProcessors(List<PostProcessor> extractors) 
        {
            ListIterator<PostProcessor> iter;
            if (reversePostProcessors) 
            {// Original (rather odd) behaviour
                iter = extractors.listIterator(extractors.size());// start at the end
                while (iter.hasPrevious()) {
                    PostProcessor ex = iter.previous();
                    TestBeanHelper.prepare((TestElement) ex);
                    ex.process();
                }
            } 
            else 
            {
                foreach (PostProcessor ex in extractors) 
                {
                    TestBeanHelper.prepare((TestElement) ex);
                    ex.process();
                }
            }
        }

        //private void runPreProcessors(List<PreProcessor> preProcessors)
        //{
        //    foreach (PreProcessor ex in preProcessors)
        //    {
        //        if (log.isDebugEnabled())
        //        {
        //            // log.debug("Running preprocessor: " + ((AbstractTestElement) ex).getName());
        //        }
        //        TestBeanHelper.prepare((TestElement)ex);
        //        ex.process();
        //    }
        //}

        //private void delay(List<Timer> timers) 
        //{
        //    long sum = 0;
        //    foreach (Timer timer in timers) 
        //    {
        //        TestBeanHelper.prepare((TestElement) timer);
        //        sum += timer.delay();
        //    }
        //    if (sum > 0) 
        //    {
        //        try 
        //        {
        //            Thread.sleep(sum);
        //        } 
        //        catch (InterruptedException e) 
        //        {
        //            log.warn("The delay timer was interrupted - probably did not wait as long as intended.");
        //        }
        //    }
        //}

        void notifyTestListeners() 
        {
            threadVars.incIteration();
            foreach (TestIterationListener listener in testIterationStartListeners) 
            {
                if (listener is TestElement) 
                {
                    listener.testIterationStart(new LoopIterationEvent(controller, threadVars.getIteration()));
                    ((TestElement) listener).recoverRunningVersion();
                }
                else 
                {
                    listener.testIterationStart(new LoopIterationEvent(controller, threadVars.getIteration()));
                }
            }
        }

        private void notifyListeners(List<SampleListener> listeners, SampleResult result) 
        {
//            SampleEvent event = new SampleEvent(result, threadGroup.getName(), threadVars);
//            notifier.notifyListeners(event, listeners);
        }

        /**
         * Set rampup delay for JMeterThread Thread
         * @param delay Rampup delay for JMeterThread
         */
        public void setInitialDelay(int delay) {
            initialDelay = delay;
        }

        /**
         * Initial delay if ramp-up period is active for this threadGroup.
         */
        private void rampUpDelay() {
            delayBy(initialDelay, "RampUp");
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
        public void setThreadNum(int threadNum) 
        {
            this.threadNum = threadNum;
        }

        private class IterationListener : LoopIterationListener 
        {
            /**
             * {@inheritDoc}
             */
            public void iterationStart(LoopIterationEvent iterEvent)
            {
                notifyTestListeners();
            }
        }

        /**
         * Save the engine instance for access to the stop methods
         *
         * @param engine
         */
        public void setEngine(StandardNetMeterEngine engine)
        {
            this.engine = engine;
        }

        /**
         * Should Test stop on sampler error?
         *
         * @param b -
         *            true or false
         */
        public void setOnErrorStopTest(Boolean b) 
        {
            onErrorStopTest = b;
        }

        /**
         * Should Test stop abruptly on sampler error?
         *
         * @param b -
         *            true or false
         */
        public void setOnErrorStopTestNow(Boolean b)
        {
            onErrorStopTestNow = b;
        }

        /**
         * Should Thread stop on Sampler error?
         *
         * @param b -
         *            true or false
         */
        public void setOnErrorStopThread(Boolean b)
        {
            onErrorStopThread = b;
        }

        /**
         * Should Thread start next loop on Sampler error?
         *
         * @param b -
         *            true or false
         */
        public void setOnErrorStartNextLoop(Boolean b)
        {
            onErrorStartNextLoop = b;
        }

        public void setThreadGroup(AbstractThreadGroup group)
        {
            this.threadGroup = group;
        }
    }
}
