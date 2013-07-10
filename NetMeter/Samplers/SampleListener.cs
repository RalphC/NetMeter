using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMeter.Samplers
{
    public interface SampleListener
    {
        /**
         * A sample has started and stopped.
         */
        void sampleOccurred(SampleEvent e);

        /**
         * A sample has started.
         */
        void sampleStarted(SampleEvent e);

        /**
         * A sample has stopped.
         */
        void sampleStopped(SampleEvent e);
    }
}
