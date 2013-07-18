using System;
using Valkyrie.Collections;

namespace NetMeter.Engine
{
    public interface NetMeterEngine
    {
        void Configure(HashTree tree);
        void RunTest();
        void StopTest(Boolean now);
        void Reset();
        void SetProperties();
        void Exit();
        bool isActive();
    }
}
