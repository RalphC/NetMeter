using System;
using NetMeter.TestElements;
using NetMeter.Samplers;
using NetMeter.Engine.Event;

namespace NetMeter.Control
{
    public interface Controller : TestElement
    {
        /**
         * Delivers the next Sampler or null
         *
         * @return org.apache.jmeter.samplers.Sampler or null
         */
        TestAgent next();

        /**
         * Indicates whether the Controller is done delivering Samplers for the rest
         * of the test.
         *
         * When the top-level controller returns true to JMeterThread,
         * the thread is complete.
         *
         * @return boolean
         */
        Boolean isDone();

        /**
         * Controllers have to notify listeners of when they begin an iteration
         * through their sub-elements.
         */
        void addIterationListener(LoopIterationListener listener);

        /**
         * Called to initialize a controller at the beginning of a test iteration.
         */
        void Initialize();

        /**
         * Unregister IterationListener
         * @param iterationListener {@link LoopIterationListener}
         */
        void RemoveIterationListener(LoopIterationListener iterationListener);

        /**
         * Trigger end of loop condition on controller (used by Start Next Loop feature)
         */
        void triggerEndOfLoop();
    }
}
