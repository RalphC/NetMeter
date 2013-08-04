using log4net;
using System;
using Valkyrie.Logging;

namespace UPMP.NetMeterPlugin
{
    public abstract class AbstractTestAgentImpl
    {
        protected TestAgentBase testElement;

        private static ILog log = LoggingManager.GetLoggerForClass();

        public class HttpClientKey
        {

        }

        protected HttpClientKey setupClient(Uri url)
        {
            return null;
        }

        protected AbstractTestAgentImpl(TestAgentBase testElement)
        {
            this.testElement = testElement;
        }

        protected abstract TestExecuteResult Execute(Uri url, String methond);

        protected TestExecuteResult ErrorResult(TestExecuteResult res)
        {
            return null;
        }

        protected Byte[] ReadResponse(TestExecuteResult res)
        {
            return null;
        }

        protected TestExecuteResult ResultProcessing(TestExecuteResult res)
        {
            return res;
        }

        protected String GetResponseHeaders(TestExecuteResult response)
        {
            return "";
        }

        protected String SetConnectionCookie(Uri url)
        {
            return "";
        }

        protected void saveConnectionCookies(Uri url)
        {

        }

        protected void SetupRequest(Uri url)
        {

        }

        internal TestAgentBase TestAgentBase
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }
    }
}
