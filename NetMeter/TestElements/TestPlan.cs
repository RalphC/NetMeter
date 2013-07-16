using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using log4net;
using Valkyrie.Logging;
using NetMeter.Threads;
using NetMeter.TestElements.Property;
using System.IO;

namespace NetMeter.TestElements
{
    class TestPlan : AbstractTestElement, ISerializable, TestStateListener 
    {
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

        //+ JMX field names - do not change values
        private static sealed String FUNCTIONAL_MODE = "TestPlan.functional_mode"; //$NON-NLS-1$

        private static sealed String USER_DEFINED_VARIABLES = "TestPlan.user_defined_variables"; //$NON-NLS-1$

        private static sealed String SERIALIZE_THREADGROUPS = "TestPlan.serialize_threadgroups"; //$NON-NLS-1$

        private static sealed String CLASSPATHS = "TestPlan.user_define_classpath"; //$NON-NLS-1$

        private static sealed String TEARDOWN_ON_SHUTDOWN = "TestPlan.tearDown_on_shutdown"; //$NON-NLS-1$

        //- JMX field names

        private static sealed String CLASSPATH_SEPARATOR = ","; //$NON-NLS-1$

        private static sealed String BASEDIR = "basedir";

        private LinkedList<AbstractThreadGroup> threadGroups = new LinkedList<AbstractThreadGroup>();

        // There's only 1 test plan, so can cache the mode here
        private static volatile Boolean functionalMode = false;

        public TestPlan() 
        {
            // this("Test Plan");
            // setFunctionalMode(false);
            // setSerialized(false);
        }

        public TestPlan(String name)
        {
            SetName(name);
            // setFunctionalMode(false);
            // setSerialized(false);
        }

        // create transient item
        private Object readResolve()
        {
            threadGroups = new LinkedList<AbstractThreadGroup>();
            return this;
        }

        public void prepareForPreCompile()
        {
            GetVariables().setRunningVersion(true);
        }

        /**
         * Fetches the functional mode property
         *
         * @return functional mode
         */
        public Boolean isFunctionalMode() 
        {
            return getPropertyAsBoolean(FUNCTIONAL_MODE);
        }

        public void setUserDefinedVariables(Arguments vars) 
        {
            SetProperty(new TestElementProperty(USER_DEFINED_VARIABLES, vars));
        }

        public NetMeterProperty getUserDefinedVariablesAsProperty() 
        {
            return getProperty(USER_DEFINED_VARIABLES);
        }

        public String GetBasedir() 
        {
            return getPropertyAsString(BASEDIR);
        }

        // Does not appear to be used yet
        public void SetBasedir(String b) 
        {
            SetProperty(BASEDIR, b);
        }

        public Arguments GetArguments() 
        {
            return GetVariables();
        }

        public Dictionary<String, String> GetUserDefinedVariables() 
        {
            Arguments args = GetVariables();
            return args.getArgumentsAsMap();
        }

        private Arguments GetVariables() 
        {
            Arguments args = (Arguments) getProperty(USER_DEFINED_VARIABLES).getObjectValue();
            if (args == null) {
                args = new Arguments();
                setUserDefinedVariables(args);
            }
            return args;
        }

        public void SetFunctionalMode(Boolean funcMode) 
        {
            SetProperty(new BooleanProperty(FUNCTIONAL_MODE, funcMode));
            functionalMode = funcMode;
        }

        /**
         * Gets the static copy of the functional mode
         *
         * @return mode
         */
        public static Boolean GetFunctionalMode() 
        {
            return functionalMode;
        }

        public void setSerialized(Boolean serializeTGs) 
        {
            SetProperty(new BooleanProperty(SERIALIZE_THREADGROUPS, serializeTGs));
        }

        public void setTearDownOnShutdown(Boolean tearDown) 
        {
            SetProperty(TEARDOWN_ON_SHUTDOWN, tearDown, false);
        }

        public Boolean isTearDownOnShutdown() 
        {
            return getPropertyAsBoolean(TEARDOWN_ON_SHUTDOWN, false);
        }

        /**
         * Set the classpath for the test plan
         * @param text
         */
        public void setTestPlanClasspath(String text) 
        {
            SetProperty(CLASSPATHS,text);
        }

        public void setTestPlanClasspathArray(String[] text)
        {
            StringBuilder cat = new StringBuilder();
            for (int idx=0; idx < text.Length; idx++) 
            {
                if (idx > 0) 
                {
                    cat.Append(CLASSPATH_SEPARATOR);
                }
                cat.Append(text[idx]);
            }
            this.setTestPlanClasspath(cat.ToString());
        }

        public String[] getTestPlanClasspathArray() 
        {
            return JOrphanUtils.split(this.getTestPlanClasspath(),CLASSPATH_SEPARATOR);
        }

        /**
         * Returns the classpath
         * @return classpath
         */
        public String getTestPlanClasspath() 
        {
            return getPropertyAsString(CLASSPATHS);
        }

        /**
         * Fetch the serialize threadgroups property
         *
         * @return serialized setting
         */
        public Boolean isSerialized() 
        {
            return getPropertyAsBoolean(SERIALIZE_THREADGROUPS);
        }

        public void addParameter(String name, String value)
        {
            GetVariables().addArgument(name, value);
        }

        public void AddTestElement(TestElement tg) 
        {
            base.AddTestElement(tg);
            if (tg is AbstractThreadGroup && !isRunningVersion())
            {
                AddThreadGroup((AbstractThreadGroup) tg);
            }
        }

        /**
         * Adds a feature to the AbstractThreadGroup attribute of the TestPlan object.
         *
         * @param group
         *            the feature to be added to the AbstractThreadGroup attribute
         */
        public void AddThreadGroup(AbstractThreadGroup group) 
        {
            threadGroups.AddLast(group);
        }

        /**
         * {@inheritDoc}
         */
        public void TestEnded() 
        {
            try 
            {
                FileServer.getFileServer().closeFiles();
            } 
            catch (IOException e) 
            {
                log.Error("Problem closing files at end of test", e);
            }
        }

        /**
         * {@inheritDoc}
         */
        public void TestEnded(String host) 
        {
            TestEnded();
        }

        /**
         * {@inheritDoc}
         */
        public void TestStarted() 
        {
            if (GetBasedir() != null && GetBasedir().Length > 0) 
            {
                try
                {
                    FileServer.getFileServer().setBasedir(FileServer.getFileServer().GetBasedir() + GetBasedir());
                } 
                catch (IllegalStateException e) 
                {
                    log.Error("Failed to set file server base dir with " + GetBasedir(), e);
                }
            }
            // we set the classpath
            String[] paths = this.getTestPlanClasspathArray();
            for (int idx=0; idx < paths.Length; idx++) 
            {
                NewDriver.addURL(paths[idx]);
                log.Info("add " + paths[idx] + " to classpath");
            }
        }

        /**
         * {@inheritDoc}
         */
        public void TestStarted(String host) 
        {
            TestStarted();
        }

    }
}
