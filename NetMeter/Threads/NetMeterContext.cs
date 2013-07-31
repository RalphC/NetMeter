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

        private StandardEngine engine;

        private NetMeterThread nThread;

        private AbstractThreadGroup threadGroup;

        private int threadNum;

        private bool isReinitSubControllers = false;

        public bool restartNextLoop { get; set; }

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
            nThread = null;
            isReinitSubControllers = false;
            samplerContext.Clear();
            restartNextLoop = false;
        }

        /**
         * Gives access to the JMeter variables for the current thread.
         * 
         * @return a pointer to the JMeter variables.
         */
        public NetMeterVariables GetVariables()
        {
            return variables;
        }

        public void SetVariables(NetMeterVariables vars)
        {
            this.variables = vars;
        }

        public SampleResult getPreviousResult() 
        {
            return previousResult;
        }

        public void SetPreviousResult(SampleResult result) 
        {
            this.previousResult = result;
        }

        public Sampler GetCurrentSampler() 
        {
            return currentSampler;
        }

        public void SetCurrentSampler(Sampler sampler)
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
        public void SetThreadNum(int threadNum)
        {
            this.threadNum = threadNum;
        }

        public NetMeterThread getThread()
        {
            return this.nThread;
        }

        public void SetThread(NetMeterThread nThread)
        {
            this.nThread = nThread;
        }

        public AbstractThreadGroup getThreadGroup()
        {
            return this.threadGroup;
        }

        public void SetThreadGroup(AbstractThreadGroup threadgrp)
        {
            this.threadGroup = threadgrp;
        }

        public StandardEngine GetEngine()
        {
            return engine;
        }

        public void SetEngine(StandardEngine engine)
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
        public void unsetIsReinitializingSubControllers() 
        {
            if (isReinitSubControllers) 
            {
                isReinitSubControllers = false;
            }
        }

        /**
         * Set flag indicating listeners should not be notified since reinit of sub 
         * controllers is being done. See bug 50032 
         * @return true if it is the first one to set
         */
        public bool setIsReinitializingSubControllers() 
        {
            if (!isReinitSubControllers) 
            {
                isReinitSubControllers = true;
                return true;
            }
            return false;
        }

        /**
         * @return true if within reinit of Sub Controllers
         */
        public bool isReinitializingSubControllers() 
        {
            return isReinitSubControllers;
        }

        /**
         * Clean cached data after sample
         */
        public void CleanAfterSample() 
        {
            if(previousResult != null) 
            {
                previousResult.CleanAfterSample();
            }
            samplerContext.Clear();
        }

        /**
         * Sampler context is cleaned up as soon as Post-Processor have ended
         * @return Context to use within PostProcessors to cache data
         */
        public ConcurrentDictionary<String, Object> GetSamplerContext() 
        {
            return samplerContext;
        }
    }
}
