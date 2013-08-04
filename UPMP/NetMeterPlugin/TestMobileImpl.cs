using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valkyrie.Logging;

namespace UPMP.NetMeterPlugin
{
    public class TestMobileImpl : AbstractTestAgentImpl
    {
        private static ILog log = LoggingManager.GetLoggerForClass();

        protected TestMobileImpl(TestAgentBase element)
            : base(element)
        {
            
        }

        protected TestExecuteResult Execute(Uri url)
        {
            return null;
        }

        private String SendPostData(Uri url)
        {
            return "";
        }
    }
}
