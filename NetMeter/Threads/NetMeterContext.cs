using System;
using System.Collections.Concurrent;
using NetMeter.Engine;
using NetMeter.Samplers;

namespace NetMeter.Threads
{
    public class NetMeterContext
    {
        private NetMeterVariables variables;

        private SampleResult previousResult;

        private Sampler currentSampler;

        private Sampler previousSampler;

        private bool samplingStarted;

        private StandardNetMeterEngine engine;

        private NetMeterThread thread;

        private AbstractThreadGroup threadGroup;

        private int threadNum;

        private bool isReinitSubControllers = false;

        private bool restartNextLoop = false;

        private ConcurrentDictionary<String, Object> samplerContext = new ConcurrentDictionary<String, Object>();

        public NetMeterContext()
        {
            init();
        }

        public void clear() 
        {
            init();
        }

        private void init()
        {
            variables = null;
            previousResult = null;
            currentSampler = null;
            previousSampler = null;
            samplingStarted = false;
            threadNum = 0;
            thread = null;
            isReinitSubControllers = false;
            samplerContext.Clear();
        }

        /**
         * Gives access to the JMeter variables for the current thread.
         * 
         * @return a pointer to the JMeter variables.
         */
        public NetMeterVariables getVariables()
        {
            return variables;
        }

        public void setVariables(NetMeterVariables vars)
        {
            this.variables = vars;
        }

        public SampleResult getPreviousResult() 
        {
            return previousResult;
        }

        public void setPreviousResult(SampleResult result) 
        {
            this.previousResult = result;
        }

        public Sampler getCurrentSampler() 
        {
            return currentSampler;
        }

        public void setCurrentSampler(Sampler sampler)
        {
            this.previousSampler = currentSampler;
            this.currentSampler = sampler;
        }

        /**
         * Returns the previousSampler.
         *
         * @return Sampler
         */
        public Sampler getPreviousSampler() 
        {
            return previousSampler;
        }

        /**
         * Returns the threadNum.
         *
         * @return int
         */
        public int getThreadNum() 
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

        public NetMeterThread getThread()
        {
            return this.thread;
        }

        public void setThread(NetMeterThread thread)
        {
            this.thread = thread;
        }

        public AbstractThreadGroup getThreadGroup()
        {
            return this.threadGroup;
        }

        public void setThreadGroup(AbstractThreadGroup threadgrp)
        {
            this.threadGroup = threadgrp;
        }

        public StandardNetMeterEngine getEngine()
        {
            return engine;
        }

        public void setEngine(StandardNetMeterEngine engine)
        {
            this.engine = engine;
        }

        public bool isSamplingStarted()
        {
            return samplingStarted;
        }

        public void setSamplingStarted(bool b)
        {
            samplingStarted = b;
        }

        /**
         * Reset flag indicating listeners should not be notified since reinit of sub 
         * controllers is being done. See bug 50032 
         */
        public void unsetIsReinitializingSubControllers() {
            if (isReinitSubControllers) {
                isReinitSubControllers = false;
            }
        }

        /**
         * Set flag indicating listeners should not be notified since reinit of sub 
         * controllers is being done. See bug 50032 
         * @return true if it is the first one to set
         */
        public bool setIsReinitializingSubControllers() {
            if (!isReinitSubControllers) {
                isReinitSubControllers = true;
                return true;
            }
            return false;
        }

        /**
         * @return true if within reinit of Sub Controllers
         */
        public bool isReinitializingSubControllers() {
            return isReinitSubControllers;
        }

        /**
         * if set to true a restart of the loop will occurs
         * @param restartNextLoop
         */
        public void setRestartNextLoop(bool restartNextLoop) {
            this.restartNextLoop = restartNextLoop;
        }

        /**
         * a restart of the loop was required ?
         * @return the restartNextLoop
         */
        public bool isRestartNextLoop() {
            return restartNextLoop;
        }

        /**
         * Clean cached data after sample
         */
        public void cleanAfterSample() {
            if(previousResult != null) {
                previousResult.cleanAfterSample();
            }
            samplerContext.clear();
        }

        /**
         * Sampler context is cleaned up as soon as Post-Processor have ended
         * @return Context to use within PostProcessors to cache data
         */
        public ConcurrentDictionary<String, Object> getSamplerContext() {
            return samplerContext;
        }
        }
}
