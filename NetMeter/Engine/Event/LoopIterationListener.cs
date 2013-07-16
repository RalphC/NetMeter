using System;

namespace NetMeter.Engine.Event
{
    /**
     * Allows a class to receive loop iteration start events.
     */
    public interface LoopIterationListener
    {
        /**
         * Called when a loop iteration is about to start.
         * 
         * @param iterEvent the event
         */
        void IterationStart(LoopIterationEvent iterEvent);
    }
}
