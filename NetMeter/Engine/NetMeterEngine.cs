using System;

namespace NetMeter.Engine
{
    public interface NetMeterEngine
    {
        void configure();
        void runTest();
        void stopTest(Boolean now);
        void reset();
        void setProperties();
        void exit();
        bool isActive();
    }
}
