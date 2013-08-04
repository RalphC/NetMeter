using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.Assertions;
using log4net;
using Valkyrie.Logging;
using NetMeter.Samplers;

namespace NetMeterComponent
{
    public class ResponseAssertion : Assertion
    {
        private static ILog log = LoggingManager.GetLoggerForClass();

        public ResponseAssertion()
        {

        }

        public AssertionResult GetResult(ExecuteResult response)
        {
            AssertionResult result;
            result = EvaluateResponse(response);
            return result;
        }

        private AssertionResult EvaluateResponse(ExecuteResult response)
        {
            AssertionResult result = new AssertionResult();

            return result;
        }

    }
}
