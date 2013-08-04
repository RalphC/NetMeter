using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMeter.Samplers
{
    public interface ExecutionListener
    {
        /**
         * A sample has started and stopped.
         */
        void ExecutionOccurred(ExecutionEvent e);

        /**
         * A sample has started.
         */
        void ExecutionStarted(ExecutionEvent e);

        /**
         * A sample has stopped.
         */
        void ExecutionStopped(ExecutionEvent e);
    }
}
