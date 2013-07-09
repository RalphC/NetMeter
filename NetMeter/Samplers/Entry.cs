using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMeter.Samplers
{
    class Entry
    {
        private Dictionary<Class<?>, ConfigElement> configSet;

        // Set clonedSet;
        private Class<?> sampler;

        private List<Assertion> assertions;

        public Entry() 
        {
            configSet = new HashMap<Class<?>, ConfigElement>();
            // clonedSet = new HashSet();
            assertions = new LinkedList<Assertion>();
        }

        public void addAssertion(Assertion assertion) {
            assertions.add(assertion);
        }

        public List<Assertion> getAssertions() {
            return assertions;
        }

        public void setSamplerClass(Class<?> samplerClass) {
            this.sampler = samplerClass;
        }

        public Class<?> getSamplerClass() {
            return this.sampler;
        }

        public ConfigElement getConfigElement(Class<?> configClass) {
            return configSet.get(configClass);
        }

        public void addConfigElement(ConfigElement config) {
            addConfigElement(config, config.getClass());
        }

        /**
         * Add a config element as a specific class. Usually this is done to add a
         * subclass as one of it's parent classes.
         */
        public void addConfigElement(ConfigElement config, Class<?> asClass) {
            if (config != null) {
                ConfigElement current = configSet.get(asClass);
                if (current == null) {
                    configSet.put(asClass, cloneIfNecessary(config));
                } else {
                    current.addConfigElement(config);
                }
            }
        }

        private ConfigElement cloneIfNecessary(ConfigElement config) {
            if (config.expectsModification()) {
                return config;
            }
            return (ConfigElement) config.clone();
        }
    }
}
