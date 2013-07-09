using System;

namespace NetMeter.Engine.Event
{
    public interface LoopIterationListener
    {
        /**
         * Called when a loop iteration is about to start.
         * 
         * @param iterEvent the event
         */
        void iterationStart(LoopIterationEvent iterEvent);
    }
}
