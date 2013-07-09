using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NetMeter.Threads
{
    public class NetMeterVariables
    {
        private sealed Dictionary<String, Object> variables = new Dictionary<String, Object>();

        private int iteration = 0;

        // Property names to preload into JMeter variables:
        private static sealed String [] PRE_LOAD = 
        {
          "START.MS",     // $NON-NLS-1$
          "START.YMD",    // $NON-NLS-1$
          "START.HMS",    //$NON-NLS-1$
          "TESTSTART.MS", // $NON-NLS-1$
        };

        public NetMeterVariables() 
        {
            preloadVariables();
        }

        private void preloadVariables()
        {
            for (int i = 0; i < PRE_LOAD.Length; i++)
            {
                String property = PRE_LOAD[i];
                String value = JMeterUtils.getProperty(property);
                if (value != null)
                {
                    variables.Add(property,value);
                }
            }
        }

        public String getThreadName() {
            return Thread.currentThread().getName();
        }

        public int getIteration() {
            return iteration;
        }

        public void incIteration() {
            iteration++;
        }

        // Does not appear to be used
        public void initialize() {
            variables.Clear();
            preloadVariables();
        }

        /**
         * Remove a variable.
         * 
         * @param key the variable name to remove
         * 
         * @return the variable value, or {@code null} if there was no such variable
         */
        public Object Remove(String key) {
            return variables.Remove(key);
        }

        /**
         * Creates or updates a variable with a String value.
         * 
         * @param key the variable name
         * @param value the variable value
         */
        public void Add(String key, String value) 
        {
            variables.Add(key, value);
        }

        /**
         * Creates or updates a variable with a value that does not have to be a String.
         * 
         * @param key the variable name
         * @param value the variable value
         */
        public void putObject(String key, Object value)
        {
            variables.Add(key, value);
        }

        public void putAll(Dictionary<String, Object> vars)
        {
            variables.putAll(vars);
        }

        public void putAll(JMeterVariables vars) 
        {
            putAll(vars.variables);
        }

        /**
         * Gets the value of a variable, coerced to a String.
         * 
         * @param key the name of the variable
         * @return the value of the variable, or {@code null} if it does not exist
         */
        public String get(String key) 
        {
            return (String) variables.get(key);
        }

        /**
         * Gets the value of a variable (not converted to String).
         * 
         * @param key the name of the variable
         * @return the value of the variable, or {@code null} if it does not exist
         */
        public Object getObject(String key) 
        {
            return variables.get(key);
        }

        /**
         * Gets a read-only Iterator over the variables.
         * 
         * @return the iterator
         */
        public Iterator<Entry<String, Object>> getIterator()
        {
            return Collections.unmodifiableMap(variables).entrySet().iterator() ;
        }

        // Used by DebugSampler
        public Set<Entry<String, Object>> entrySet()
        {
            return Collections.unmodifiableMap(variables).entrySet();
        }
    }
}
