using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valkyrie.Collections;
using Valkyrie.Logging;
using System.ServiceModel;

namespace NetMeter.Engine
{
    public class ServerEngine : NetMeterEngine
    {
        private static ILog log = LoggingManager.GetLoggerForClass();

        private IRemoteEngine clientEngine;

        private HashTree test;

        private String host;

        private static ChannelFactory<IRemoteEngine> EngineChannelFactory;

        private static IRemoteEngine GetEngine(String host)
        {
            NetTcpBinding clientBinding = new NetTcpBinding();
            EndpointAddress clientEndpoint = new EndpointAddress(host);
            EngineChannelFactory = new ChannelFactory<IRemoteEngine>(clientBinding, clientEndpoint);
            return EngineChannelFactory.CreateChannel();
        }

        public ServerEngine(String host)
        {
            this.clientEngine = GetEngine(host);
            this.host = host;
        }

        public void Configure(HashTree testtree)
        {

        }

        public void StopTest(Boolean now)
        {

        }

        public void Reset()
        {

        }

        public void RunTest()
        {

        }

        public void Exit()
        {
            log.Info("about to exit remote server on " + host);
            try
            {
                clientEngine.Exit();
            }
            catch (Exception ex)
            {
                log.Warn("Could not perform remote exit: " + ex.Message);
            }
        }

        public void SetProperties()
        {

        }

        public Boolean isActive()
        {
            return true;
        }

    }
}
