using System;
using NetMeter.Engine.Event;

namespace NetMeter.TestElements
{
    interface TestIterationListener
    {
        /**
         * Each time through a Thread Group's test script, an iteration event is
         * fired for each thread.
         *
         * This will be after the test elements have been cloned, so in general
         * the instance will not be the same as the ones the start/end methods call.
         *
         * @param event
         */
        void testIterationStart(LoopIterationEvent iterEvent);
    }
}
