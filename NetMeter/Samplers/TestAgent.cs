using System;
using System.Runtime.Serialization;

namespace NetMeter.Samplers
{
    /**
     * Classes which are able to generate information about an entry should
     * implement this interface.
     */
    public interface TestAgent : TestElements.TestElement, ISerializable
    {
        /**
         * Obtains statistics about the given Entry, and packages the information
         * into a SampleResult.
         */
        ExecuteResult Execute(Entry e);
    }
}
