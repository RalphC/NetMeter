using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMeter.Engine
{
    class NetMeterEngineException : Exception
    {
        public NetMeterEngineException()
        {
            new Exception();
        }

        public NetMeterEngineException(String msg)
        {
            new Exception(msg);
        }

        public NetMeterEngineException(String msg, NetMeterEngineException inner)
        {
            new Exception(msg, (Exception)inner);
        }
    }
}
