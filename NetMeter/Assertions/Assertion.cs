using NetMeter.Samplers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.TestElements;

namespace NetMeter.Assertions 
{
    /**
     * An Assertion checks a SampleResult to determine whether or not it is
     * successful. The resulting success status can be obtained from a corresponding
     * Assertion Result. For example, if a web response doesn't contain an expected
     * expression, it would be considered a failure.
     *
     * @version $Revision: 674351 $
     */
    interface Assertion : TestElement
    {
        /**
         * Returns the AssertionResult object encapsulating information about the
         * success or failure of the assertion.
         *
         * @param response
         *            the SampleResult containing information about the Sample
         *            (duration, success, etc)
         *
         * @return the AssertionResult containing the information about whether the
         *         assertion passed or failed.
         */
        AssertionResult getResult(SampleResult response);
    }
}
