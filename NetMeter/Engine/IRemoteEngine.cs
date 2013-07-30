using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Valkyrie.Collections;

namespace NetMeter.Engine
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IRemoteEngine" in both code and config file together.
    [ServiceContract]
    public interface IRemoteEngine
    {
        [OperationContract]
        void Configure(HashTree testtree, String host);

        [OperationContract]
        void RunTest();

        [OperationContract]
        void StopTest(Boolean now);

        [OperationContract]
        void Reset();

        [OperationContract]
        void SetProperty();

        [OperationContract]
        void Exit();
    }
}
