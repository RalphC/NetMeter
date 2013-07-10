using System;

namespace NetMeter.Config
{
    public interface ConfigElement : ICloneable
    {

        /**
         * Add a configuration element to this one. This allows config elements to
         * combine and give a &quot;layered&quot; effect. For example,
         * HTTPConfigElements have properties for domain, path, method, and
         * parameters. If element A has everything filled in, but null for domain,
         * and element B is added, which has only domain filled in, then after
         * adding B to A, A will have the domain from B. If A already had a domain,
         * then the correct behavior is for A to ignore the addition of element B.
         *
         * @param config
         *            the element to be added to this ConfigElement
         */
        void addConfigElement(ConfigElement config);

        /**
         * If your config element expects to be modified in the process of a test
         * run, and you want those modifications to carry over from sample to sample
         * (as in a cookie manager - you want to save all cookies that get set
         * throughout the test), then return true for this method. Your config
         * element will not be cloned for each sample. If your config elements are
         * more static in nature, return false. If in doubt, return false.
         *
         * @return true if the element expects to be modified over the course of a
         *         test run
         */
        Boolean expectsModification();

        Object Clone();
    }
}
