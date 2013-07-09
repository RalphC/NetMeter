using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMeter.TestElements.Property
{
    public interface PropertyIterator
    {
        bool hasNext();

        NetMeterProperty next();

        void remove();
    }
}
