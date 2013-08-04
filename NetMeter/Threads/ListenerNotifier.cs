using NetMeter.Samplers;
using NetMeter.TestElements;
using System;
using System.Collections.Generic;

namespace NetMeter.Threads
{
    public class ListenerNotifier
    {
       /**
         * Notify a list of listeners that a sample has occurred.
         *
         * @param res
         *            the sample event that has occurred. Must be non-null.
         * @param listeners
         *            a list of the listeners which should be notified. This list
         *            must not be null and must contain only SampleListener
         *            elements.
         */
        public void notifyListeners(ExecutionEvent res, List<ExecutionListener> listeners)
        {
            foreach (ExecutionListener sampleListener in listeners) 
            {
                try 
                {
                    TestBeanHelper.prepare((TestElement) sampleListener);
                    sampleListener.ExecutionOccurred(res);
                } 
                catch (Exception e) 
                {
                    //log.error("Detected problem in Listener: ", e);
                    //log.info("Continuing to process further listeners");
                }
            }
        }
    }
}
