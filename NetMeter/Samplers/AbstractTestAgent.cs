using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.TestElements;
using NetMeter.Samplers;

namespace NetMeter.Samplers
{
    public abstract class AbstractTestAgent : AbstractTestElement, TestAgent
    {
        public Boolean Apply()
        {
            return true;
        }
    }
}
