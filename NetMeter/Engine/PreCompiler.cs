using Valkyrie.Collections;
using System;
using log4net;
using Valkyrie.Logging;
using NetMeter.Threads;
using NetMeter.TestElements;
using System.Collections.Generic;
using NetMeter.Engine.Util;

namespace NetMeter.Engine
{
    public class PreCompiler : HashTreeTraverser
    {
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

        private sealed ValueReplacer replacer;

        //   Used by both StandardJMeterEngine and ClientJMeterEngine.
        //   In the latter case, only ResultCollectors are updated,
        //   as only these are relevant to the client, and updating
        //   other elements causes all sorts of problems.
        private sealed Boolean isRemote; // skip certain processing for remote tests

        public PreCompiler() 
        {
            replacer = new ValueReplacer();
            isRemote = false;
        }

        public PreCompiler(Boolean remote) 
        {
            replacer = new ValueReplacer();
            isRemote = remote;
        }

        public void AddNode(Object node, HashTree subTree) 
        {
            if(isRemote && typeof(ResultCollector).IsAssignableFrom(node.GetType()))
            {
                try 
                {
                    replacer.ReplaceValues((TestElement) node);
                } 
                catch (InvalidVariableException e)
                {
                    log.Error("invalid variables", e);
                }
            }
            if (isRemote) {
                return;
            }
            if(typeof(TestElement).IsAssignableFrom(node.GetType()))
            {
                try 
                {
                    replacer.ReplaceValues((TestElement) node);
                } 
                catch (Exception ex) 
                {
                    log.Error("invalid variables", e);
                }
            }
            if (typeof(TestPlan).IsAssignableFrom(node.GetType()))
            {
                ((TestPlan)node).prepareForPreCompile(); //A hack to make user-defined variables in the testplan element more dynamic
                Dictionary<String, String> args = ((TestPlan) node).GetUserDefinedVariables();
                replacer.SetUserDefinedVariables(args);
                NetMeterVariables vars = new NetMeterVariables();
                vars.PutAll(args);
                NetMeterContextManager.GetContext().SetVariables(vars);
            }

            if (typeof(Arguments).IsAssignableFrom(node.GetType()))
            {
                ((Arguments)node).setRunningVersion(true);
                Dictionary<String, String> args = ((Arguments) node).GetArgumentsAsMap();
                replacer.AddVariables(args);
                NetMeterContextManager.GetContext().GetVariables().PutAll(args);
            }
        }

        public void subtractNode() 
        {
        }


        public void processPath() 
        {
        }
    }
}
