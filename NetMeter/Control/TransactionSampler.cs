using log4net;
using NetMeter.Samplers;
using NetMeter.TestElements;
using System;
using System.Collections.Generic;
using Valkyrie.Logging;

namespace NetMeter.Control
{
    class TransactionSampler : TestAgent, AbstractTestElement
    {
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

        private static sealed HashSet<String> APPLIABLE_CONFIG_CLASSES = new HashSet<String>(new String[]{"org.apache.jmeter.config.gui.SimpleConfigGui"});

        private Boolean transactionDone {get; set; }

        private TransactionController transactionController;

        private TestAgent subSampler;

        private ExecuteResult transactionSampleResult;

        private int calls = 0;

        private int noFailingSamples = 0;

        private int totalTime = 0;

        /**
         * @deprecated only for use by test code
         */
        public TransactionSampler()
        {
            //log.warn("Constructor only intended for use in testing");
        }

        public TransactionSampler(TransactionController controller, String name) 
        {
            transactionController = controller;
            SetName(name); // ensure name is available for debugging
            transactionSampleResult = new ExecuteResult();
            transactionSampleResult.setSampleLabel(name);
            // Assume success
            transactionSampleResult.Success = true;
            transactionSampleResult.sampleStart();
        }

        /**
         * One cannot sample the TransactionSampler directly.
         */
        public ExecuteResult Execute(Entry e) 
        {
            throw new Exception("Cannot sample TransactionSampler directly");
            // It is the JMeterThread which knows how to sample a real sampler
        }

        public TestAgent GetSubSampler() 
        {
            return subSampler;
        }

        public ExecuteResult GetTransactionResult() 
        {
            return transactionSampleResult;
        }

        public TransactionController getTransactionController() 
        {
            return transactionController;
        }

        public void addSubSamplerResult(ExecuteResult res) 
        {
            // Another subsample for the transaction
            calls++;
            // The transaction fails if any sub sample fails
            if (!res.Success) 
            {
                transactionSampleResult.Success = false;
                noFailingSamples++;
            }
            // Add the sub result to the transaction result
            transactionSampleResult.addSubResult(res);
            // Add current time to total for later use (exclude pause time)
            totalTime += res.GetTime();
        }

        protected void setTransactionDone() 
        {
            this.transactionDone = true;
            // Set the overall status for the transaction sample
            // TODO: improve, e.g. by adding counts to the SampleResult class
            transactionSampleResult.setResponseMessage("Number of samples in transaction : "
                            + calls + ", number of failing samples : "
                            + noFailingSamples);
            if (transactionSampleResult.isSuccessful())
            {
                transactionSampleResult.setResponseCodeOK();
            }
            // Bug 50080 (not include pause time when generate parent)
            if (!transactionController.isIncludeTimers()) 
            {
                long end = transactionSampleResult.currentTimeInMillis();
                transactionSampleResult.setIdleTime(end
                        - transactionSampleResult.getStartTime() - totalTime);
                transactionSampleResult.setEndTime(end);
            }
        }

        protected void setSubSampler(TestAgent subSampler)
        {
            this.subSampler = subSampler;
        }

        /**
         * @see org.apache.jmeter.samplers.AbstractSampler#applies(org.apache.jmeter.config.ConfigTestElement)
         */
        public Boolean Applies(ConfigTestElement configElement) 
        {
            String guiClass = configElement.getProperty(TestElement.GUI_CLASS).getStringValue();
            return APPLIABLE_CONFIG_CLASSES.Contains(guiClass);
        }
    }
}
