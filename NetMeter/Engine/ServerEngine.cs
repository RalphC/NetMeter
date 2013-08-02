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

        private static List<IRemoteEngine> activeClientEngines = new List<IRemoteEngine>();

        private static List<IRemoteEngine> failedClientEngines = new List<IRemoteEngine>();

        private HashTree test;

        private String host;

        private static ChannelFactory<IRemoteEngine> EngineChannelFactory;

        private static void GetEngine(String host, HashTree testTree)
        {
            List<IRemoteEngine> engineList = new List<IRemoteEngine>();
            IRemoteEngine engine = null;
            foreach (String client in host.Split(','))
            {
                NetTcpBinding clientBinding = new NetTcpBinding();
                EndpointAddress clientEndpoint = new EndpointAddress(host);
                EngineChannelFactory = new ChannelFactory<IRemoteEngine>(clientBinding, clientEndpoint);
                try
                {
                    engine = EngineChannelFactory.CreateChannel();
                    activeClientEngines.Add(engine);
                }
                catch (Exception ex)
                {
                    log.Warn(ex.Message);
                    System.Console.WriteLine(ex.Message);
                    if (engine != null)
                    {
                        failedClientEngines.Add(engine);
                    }
                }
            }
        }

        public ServerEngine(String host, HashTree testTree)
        {
            this.test = testTree;
            GetEngine(host, test);
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
                foreach (IRemoteEngine engine in activeClientEngines)
                {
                    engine.Exit();
                }
                
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
