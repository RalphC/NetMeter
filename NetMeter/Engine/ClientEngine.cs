using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using log4net;
using Valkyrie.Logging;
using System.Threading;
using Valkyrie.Collections;

namespace NetMeter.Engine
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "IRemoteEngine" in both code and config file together.
    public class ClientEngine : IRemoteEngine
    {
        private static ILog log = LoggingManager.GetLoggerForClass();

        private NetMeterEngine StandardEngine;

        private Thread ownerThread;

        private static Int32 DEFAULT_WCF_PORT = 1099;

        private static Int32 DEFAULT_LOCAL_PORT = 0;

        public static void StartClient()
        {
            ClientEngine engine = new ClientEngine();
            engine.Init();
        }

        private ClientEngine()
        {
            NetTcpBinding portSharingBinding = new NetTcpBinding();
            portSharingBinding.PortSharingEnabled = true;

            ServiceHost server = new ServiceHost(typeof(ClientEngine));
            server.AddServiceEndpoint(typeof(IRemoteEngine), portSharingBinding, "net.tcp://localhost:1099/RemoteNetMeterEngine");
        }

        private static Boolean createServer = true;

        private Object LOCK = new Object();

        private Int32 wcfPort;

        private void Init()
        {
            log.Info("Starting Standard Engine on" + this.wcfPort);

        }

        public void Configure(HashTree testtree, String host)
        {
            log.Info("Creating NetMeterEngine on Host" + host);
            Monitor.Enter(LOCK);
            try
            {
                if (StandardEngine != null && StandardEngine.isActive())
                {
                    log.Warn("Engine is busy - cannot create new NetMeterEngine");
                    throw new Exception("Engine is busy");
                }
                ownerThread = Thread.CurrentThread;
                StandardEngine = new StandardEngine(host);
                StandardEngine.Configure(testtree);
            }
            finally
            {
                Monitor.Exit(LOCK);
            }
        }

        public void RunTest()
        {
            log.Info("Running Test");

            StandardEngine.RunTest();
        }

        public void Reset()
        {

        }

        public void StopTest(Boolean now)
        {
            if (now)
            {

            } 
            else
            {
            }
            StandardEngine.StopTest(now);
            log.Info("Test Stopped");
        }

        public void Exit()
        {

        }

        public void SetProperty()
        {

        }

        private void CheckOwner(String methodName)
        {
            if ( ownerThread != null && ownerThread != Thread.CurrentThread)
            {
                String msg = String.Format("The engine is not owned by this thread - cannot call {0}", methodName);
                log.Warn(msg);
                throw new Exception(msg);
            }
        }

    }
}
