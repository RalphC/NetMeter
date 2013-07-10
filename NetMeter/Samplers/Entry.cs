using NetMeter.Assertions;
using System;
using System.Collections.Generic;
using NetMeter.Config;

namespace NetMeter.Samplers
{
    public class Entry
    {
        private Dictionary<Object, ConfigElement> configSet;

        // Set clonedSet;
        private Object sampler;

        private List<Assertion> assertions;

        public Entry() 
        {
            configSet = new Dictionary<Object, ConfigElement>();
            // clonedSet = new HashSet();
            assertions = new List<Assertion>();
        }

        public void addAssertion(Assertion assertion) 
        {
            assertions.Add(assertion);
        }

        public List<Assertion> getAssertions()
        {
            return assertions;
        }

        public void setSamplerClass(Object samplerClass) 
        {
            this.sampler = samplerClass;
        }

        public Object getSamplerClass() 
        {
            return this.sampler;
        }

        public ConfigElement getConfigElement(Object configClass) 
        {
            ConfigElement config = null;
            if (configSet.TryGetValue(configClass, out config))
            {
                return config;
            }
            return null;
        }

        public void addConfigElement(ConfigElement config) 
        {
            addConfigElement(config, config.GetType());
        }

        /**
         * Add a config element as a specific class. Usually this is done to add a
         * subclass as one of it's parent classes.
         */
        public void addConfigElement(ConfigElement config, Object asClass) 
        {
            if (config != null) 
            {
                ConfigElement current = null;
                if (configSet.TryGetValue(asClass, out current))
                {
                    current.addConfigElement(config);
                }
                else
                {
                    configSet.Add(asClass, cloneIfNecessary(config));
                }
            }
        }

        private ConfigElement cloneIfNecessary(ConfigElement config) 
        {
            if (config.expectsModification())
            {
                return config;
            }
            return (ConfigElement) config.Clone();
        }
    }
}
