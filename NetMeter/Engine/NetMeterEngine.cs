using System;

namespace NetMeter.Engine
{
    public interface NetMeterEngine
    {
        void Configure();
        void RunTest();
        void StopTest(Boolean now);
        void Reset();
        void SetProperties();
        void Exit();
        bool isActive();
    }
}
