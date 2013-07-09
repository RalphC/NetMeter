using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;

namespace NetMeter.Engine
{
    public interface NetMeterEngine
    {
        void configure();
        void runTest();
        void stopTest(bool now);
        void reset();
        void setProperties();
        void exit();
        bool isActive();
    }
}
