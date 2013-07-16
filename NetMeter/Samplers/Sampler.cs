using System;
using System.Runtime.Serialization;

namespace NetMeter.Samplers
{
    /**
     * Classes which are able to generate information about an entry should
     * implement this interface.
     */
    public interface Sampler : TestElements.TestElement, ISerializable
    {
        /**
         * Obtains statistics about the given Entry, and packages the information
         * into a SampleResult.
         */
        SampleResult Sample(Entry e);
    }
}
