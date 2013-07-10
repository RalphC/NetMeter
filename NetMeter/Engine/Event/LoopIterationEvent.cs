using System;
using NetMeter.TestElements;

namespace NetMeter.Engine.Event
{
    /**
     * An iteration event provides information about the iteration number and the
     * source of the event.
     */
    public class LoopIterationEvent
    {
        private sealed Int32 iteration;

        private sealed TestElement source;

        public LoopIterationEvent(TestElement source, Int32 iter)
        {
            this.iteration = iter;
            this.source = source;
        }

        /**
         * Returns the iteration.
         *
         * @return int
         */
        public int getIteration()
        {
            return iteration;
        }

        /**
         * Returns the source.
         *
         * @return TestElement
         */
        public TestElement getSource()
        {
            return source;
        }
    }
}
