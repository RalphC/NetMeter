using log4net;
using System;
using Valkyrie.Logging;
using System.Collections.Generic;
using NetMeter.TestElements;
using NetMeter.TestElements.Property;


namespace NetMeter.Engine.Util
{
    class ValueReplacer
    {
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

        private sealed CompoundVariable masterFunction = new CompoundVariable();

        private Dictionary<String, String> variables = new Dictionary<String, String>();

        public ValueReplacer() 
        {
        }

        public ValueReplacer(TestPlan tp) 
        {
            SetUserDefinedVariables(tp.GetUserDefinedVariables());
        }

        Boolean ContainsKey(String k)
        {
            return variables.ContainsKey(k);
        }

        public void SetUserDefinedVariables(Dictionary<String, String> variables) 
        {
            this.variables = variables;
        }

        public void ReplaceValues(TestElement el) 
        {
            List<NetMeterProperty> newProps = ReplaceValues(el.propertyIterator(), new ReplaceStringWithFunctions(masterFunction,
                    variables));
            SetProperties(el, newProps);
        }

        private void SetProperties(TestElement el, List<NetMeterProperty> newProps) 
        {
            el.Clear();
            foreach (NetMeterProperty jmp in newProps)
            {
                el.SetProperty(jmp);
            }
        }

        public void ReverseReplace(TestElement el) 
        {
            List<NetMeterProperty> newProps = ReplaceValues(el.propertyIterator(), new ReplaceFunctionsWithStrings(masterFunction,
                    variables));
            SetProperties(el, newProps);
        }

        public void ReverseReplace(TestElement el, Boolean regexMatch) 
        {
            List<NetMeterProperty> newProps = ReplaceValues(el.propertyIterator(), new ReplaceFunctionsWithStrings(masterFunction,
                    variables, regexMatch));
            SetProperties(el, newProps);
        }

        public void undoReverseReplace(TestElement el) 
        {
            List<NetMeterProperty> newProps = ReplaceValues(el.propertyIterator(), new UndoVariableReplacement(masterFunction,
                    variables));
            SetProperties(el, newProps);
        }

        public void addVariable(String name, String value) 
        {
            variables.Add(name, value);
        }

        /**
         * Add all the given variables to this replacer's variables map.
         *
         * @param vars
         *            A map of variable name-value pairs (String-to-String).
         */
        public void AddVariables(Dictionary<String, String> vars) 
        {
            variables.PutAll(vars);
        }

        private LinkedList<NetMeterProperty> ReplaceValues(PropertyIterator iter, ValueTransformer transform)
        {
            LinkedList<NetMeterProperty> props = new LinkedList<NetMeterProperty>();
            while (iter.hasNext()) {
                NetMeterProperty val = iter.next();
                if (log.IsDebugEnabled) 
                {
                    log.Debug("About to replace in property of type: " + val.GetType() + ": " + val);
                }
                if (typeof(StringProperty).IsAssignableFrom(val.GetType())) 
                {
                    // Must not convert TestElement.gui_class etc
                    if (!val.getName().Equals(TestElement.GUI_CLASS) &&
                            !val.getName().Equals(TestElement.TEST_CLASS))
                    {
                        val = transform.transformValue(val);
                        if (log.IsDebugEnabled) 
                        {
                            log.Debug("Replacement result: " + val);
                        }
                    }
                } 
                else if (typeof(MultiProperty).IsAssignableFrom(val.GetType()))
                {
                    MultiProperty multiVal = (MultiProperty) val;
                    LinkedList<NetMeterProperty> newValues = ReplaceValues(multiVal.iterator(), transform);
                    multiVal.Clear();
                    foreach (NetMeterProperty jmp in newValues)
                    {
                        multiVal.addProperty(jmp);
                    }
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Replacement result: " + multiVal);
                    }
                } 
                else 
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug("Won't replace " + val);
                    }
                }
                props.AddLast(val);
            }
            return props;
        }
    }
}
