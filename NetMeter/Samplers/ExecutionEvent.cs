using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;
using NetMeter.Threads;
using System.Net.Sockets;
using log4net;
using Valkyrie.Logging;
using NetMeter.Util;

namespace NetMeter.Samplers
{
    public class ExecutionEvent : ISerializable
    {
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

        private static sealed Int64 serialVersionUID = 232L;

        public static sealed String SAMPLE_VARIABLES = "sample_variables"; // $NON-NLS-1$

        public static sealed String HOSTNAME;

        // List of variable names to be saved in JTL files
        private static sealed String[] variableNames;
        // Number of variable names
        private static sealed Int32 varCount;

        // The values. Entries be null, but there will be the correct number.
        private sealed String[] values;

        private sealed ExecuteResult result;

        private sealed String threadGroup; // TODO appears to duplicate the threadName field in SampleResult

        private sealed String hostname;

        private sealed Boolean isTransaction;

        /*
         * Only for Unit tests
         */
        public ExecutionEvent() 
            : this(null, null)
        {
        }

        /**
         * Creates SampleEvent without saving any variables.
         *
         * Use by Proxy and StatisticalSampleSender.
         *
         * @param result SampleResult
         * @param threadGroup name
         */
        public ExecutionEvent(ExecuteResult result, String threadGroup)
            : this(result, threadGroup, HOSTNAME, false)
        {
        }

        /**
         * Contructor used for normal samples, saves variable values if any are defined.
         *
         * @param result
         * @param threadGroup name
         * @param jmvars Jmeter variables
         */
        public ExecutionEvent(ExecuteResult result, String threadGroup, NetMeterVariables jmvars) 
            : this(result, threadGroup, jmvars, false)
        {
        }

        /**
         * Only intended for use when loading results from a file.
         *
         * @param result
         * @param threadGroup
         * @param hostname
         */
        public ExecutionEvent(ExecuteResult result, String threadGroup, String hostname) 
            : this(result, threadGroup, hostname, false)
        {
        }
    
        private ExecutionEvent(ExecuteResult result, String threadGroup, String hostname, Boolean isTransactionSampleEvent) 
        {
            String hn="";
            try 
            {
                hn = Dns.GetHostName();
            } 
            catch (SocketException e) 
            {
                log.Error("Cannot obtain local host name "+e);
            }
            HOSTNAME = hn;

            String vars = NetMeterUtils.getProperty(SAMPLE_VARIABLES);
            variableNames=vars != null ? vars.Split(',') : new String[0];
            varCount = variableNames.Length;

            if (varCount > 0)
            {
                log.Info(varCount + " sample_variables have been declared: "+vars);
            }
            this.result = result;
            this.threadGroup = threadGroup;
            this.hostname = hostname;
            values = new String[variableNames.Length];
            this.isTransaction = isTransactionSampleEvent;
        }

        /**
         * @param result
         * @param threadGroup
         * @param jmvars
         * @param isTransactionSampleEvent
         */
        public ExecutionEvent(ExecuteResult result, String threadGroup, NetMeterVariables jmvars, Boolean isTransactionSampleEvent)
            : this(result, threadGroup, HOSTNAME, isTransactionSampleEvent)
        {
            saveVars(jmvars);
        }

        private void saveVars(NetMeterVariables vars)
        {
            for(int i = 0; i < variableNames.Length; i++)
            {
                values[i] = vars.get(variableNames[i]);
            }
        }

        /** Return the number of variables defined */
        public static int getVarCount()
        {
            return varCount;
        }

        /** Get the nth variable name (zero-based) */
        public static String getVarName(int i)
        {
            return variableNames[i];
        }

        /** Get the nth variable value (zero-based) */
        public String getVarValue(int i)
        {
            try
            {
                return values[i];
            } 
            catch (Exception e) 
            {
                //throw new NetMeterError("Check the sample_variable settings!", e);
            }
        }

        public ExecuteResult getResult() 
        {
            return result;
        }

        public String getThreadGroup() 
        {
            return threadGroup;
        }

        public String getHostname() 
        {
            return hostname;
        }

        /**
         * @return the isTransactionSampleEvent
         */
        public Boolean isTransactionSampleEvent() 
        {
            return isTransaction;
        }
    }
}
