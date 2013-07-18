using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.Engine;
using Valkyrie.Logging;
using log4net;
using NetMeter.Util;
using System.Threading;
using System.IO;
using Valkyrie.OptionParser;
using NetMeter.TestElements;
using System.Net.Sockets;
using System.Net.Sockets.UdpClient;
using Valkyrie.Collections;
using System.Net;

namespace NetMeter
{
    public class NetMeterServer
    {
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

        public static int UDP_PORT_DEFAULT = 4445; // needed for ShutdownClient

        public static String HTTP_PROXY_PASS = "http.proxyPass"; // $NON-NLS-1$

        public static String HTTP_PROXY_USER = "http.proxyUser"; // $NON-NLS-1$

        public static String JMETER_NON_GUI = "JMeter.NonGui"; // $NON-NLS-1$

        // If the -t flag is to "LAST", then the last loaded file (if any) is used
        private static String USE_LAST_JMX = "LAST";
        // If the -j  or -l flag is set to LAST or LAST.log|LAST.jtl, then the last loaded file name is used to
        // generate the log file name by removing .JMX and replacing it with .log|.jtl

        private static int PROXY_PASSWORD     = 'a';// $NON-NLS-1$
        private static int JMETER_HOME_OPT    = 'd';// $NON-NLS-1$
        private static int HELP_OPT           = 'h';// $NON-NLS-1$
        // jmeter.log
        private static int JMLOGFILE_OPT      = 'j';// $NON-NLS-1$
        // sample result log file
        private static int LOGFILE_OPT        = 'l';// $NON-NLS-1$
        private static int NONGUI_OPT         = 'n';// $NON-NLS-1$
        private static int PROPFILE_OPT       = 'p';// $NON-NLS-1$
        private static int PROPFILE2_OPT      = 'q';// $NON-NLS-1$
        private static int REMOTE_OPT         = 'r';// $NON-NLS-1$
        private static int SERVER_OPT         = 's';// $NON-NLS-1$
        private static int TESTFILE_OPT       = 't';// $NON-NLS-1$
        private static int PROXY_USERNAME     = 'u';// $NON-NLS-1$
        private static int VERSION_OPT        = 'v';// $NON-NLS-1$

        private static int SYSTEM_PROPERTY    = 'D';// $NON-NLS-1$
        private static int JMETER_GLOBAL_PROP = 'G';// $NON-NLS-1$
        private static int PROXY_HOST         = 'H';// $NON-NLS-1$
        private static int JMETER_PROPERTY    = 'J';// $NON-NLS-1$
        private static int LOGLEVEL           = 'L';// $NON-NLS-1$
        private static int NONPROXY_HOSTS     = 'N';// $NON-NLS-1$
        private static int PROXY_PORT         = 'P';// $NON-NLS-1$
        private static int REMOTE_OPT_PARAM   = 'R';// $NON-NLS-1$
        private static int SYSTEM_PROPFILE    = 'S';// $NON-NLS-1$
        private static int REMOTE_STOP        = 'X';// $NON-NLS-1$



        /**
         * Define the understood options. Each CLOptionDescriptor contains:
         * <ul>
         * <li>The "long" version of the option. Eg, "help" means that "--help"
         * will be recognised.</li>
         * <li>The option flags, governing the option's argument(s).</li>
         * <li>The "short" version of the option. Eg, 'h' means that "-h" will be
         * recognised.</li>
         * <li>A description of the option.</li>
         * </ul>
         */
        private static CLOptionDescriptor[] options = new CLOptionDescriptor[] 
        {
                new CLOptionDescriptor("help", CLOptionDescriptor.ARGUMENT_DISALLOWED, HELP_OPT,
                        "print usage information and exit"),
                new CLOptionDescriptor("version", CLOptionDescriptor.ARGUMENT_DISALLOWED, VERSION_OPT,
                        "print the version information and exit"),
                new CLOptionDescriptor("propfile", CLOptionDescriptor.ARGUMENT_REQUIRED, PROPFILE_OPT,
                        "the jmeter property file to use"),
                new CLOptionDescriptor("addprop", CLOptionDescriptor.ARGUMENT_REQUIRED
                        | CLOptionDescriptor.DUPLICATES_ALLOWED, PROPFILE2_OPT,
                        "additional JMeter property file(s)"),
                new CLOptionDescriptor("testfile", CLOptionDescriptor.ARGUMENT_REQUIRED, TESTFILE_OPT,
                        "the jmeter test(.jmx) file to run"),
                new CLOptionDescriptor("logfile", CLOptionDescriptor.ARGUMENT_REQUIRED, LOGFILE_OPT,
                        "the file to log samples to"),
                new CLOptionDescriptor("jmeterlogfile", CLOptionDescriptor.ARGUMENT_REQUIRED, JMLOGFILE_OPT,
                        "jmeter run log file (jmeter.log)"),
                new CLOptionDescriptor("nongui", CLOptionDescriptor.ARGUMENT_DISALLOWED, NONGUI_OPT,
                        "run JMeter in nongui mode"),
                new CLOptionDescriptor("server", CLOptionDescriptor.ARGUMENT_DISALLOWED, SERVER_OPT,
                        "run the JMeter server"),
                new CLOptionDescriptor("proxyHost", CLOptionDescriptor.ARGUMENT_REQUIRED, PROXY_HOST,
                        "Set a proxy server for JMeter to use"),
                new CLOptionDescriptor("proxyPort", CLOptionDescriptor.ARGUMENT_REQUIRED, PROXY_PORT,
                        "Set proxy server port for JMeter to use"),
                new CLOptionDescriptor("nonProxyHosts", CLOptionDescriptor.ARGUMENT_REQUIRED, NONPROXY_HOSTS,
                        "Set nonproxy host list (e.g. *.apache.org|localhost)"),
                new CLOptionDescriptor("username", CLOptionDescriptor.ARGUMENT_REQUIRED, PROXY_USERNAME,
                        "Set username for proxy server that JMeter is to use"),
                new CLOptionDescriptor("password", CLOptionDescriptor.ARGUMENT_REQUIRED, PROXY_PASSWORD,
                        "Set password for proxy server that JMeter is to use"),
                new CLOptionDescriptor("jmeterproperty", CLOptionDescriptor.DUPLICATES_ALLOWED
                        | CLOptionDescriptor.ARGUMENTS_REQUIRED_2, JMETER_PROPERTY,
                        "Define additional JMeter properties"),
                new CLOptionDescriptor("globalproperty", CLOptionDescriptor.DUPLICATES_ALLOWED
                        | CLOptionDescriptor.ARGUMENTS_REQUIRED_2, JMETER_GLOBAL_PROP,
                        "Define Global properties (sent to servers)\n\t\te.g. -Gport=123 or -Gglobal.properties"),
                new CLOptionDescriptor("systemproperty", CLOptionDescriptor.DUPLICATES_ALLOWED
                        | CLOptionDescriptor.ARGUMENTS_REQUIRED_2, SYSTEM_PROPERTY,
                        "Define additional system properties"),
                new CLOptionDescriptor("systemPropertyFile", CLOptionDescriptor.DUPLICATES_ALLOWED
                        | CLOptionDescriptor.ARGUMENT_REQUIRED, SYSTEM_PROPFILE,
                        "additional system property file(s)"),
                new CLOptionDescriptor("loglevel", CLOptionDescriptor.DUPLICATES_ALLOWED
                        | CLOptionDescriptor.ARGUMENTS_REQUIRED_2, LOGLEVEL,
                        "[category=]level e.g. jorphan=INFO or jmeter.util=DEBUG"),
                new CLOptionDescriptor("runremote", CLOptionDescriptor.ARGUMENT_DISALLOWED, REMOTE_OPT,
                        "Start remote servers (as defined in remote_hosts)"),
                new CLOptionDescriptor("remotestart", CLOptionDescriptor.ARGUMENT_REQUIRED, REMOTE_OPT_PARAM,
                        "Start these remote servers (overrides remote_hosts)"),
                new CLOptionDescriptor("homedir", CLOptionDescriptor.ARGUMENT_REQUIRED, JMETER_HOME_OPT,
                        "the jmeter home directory to use"),
                new CLOptionDescriptor("remoteexit", CLOptionDescriptor.ARGUMENT_DISALLOWED, REMOTE_STOP,
                "Exit the remote servers at end of test (non-GUI)"),
                        
        };


        private NetMeterServer parent;

        private Boolean remoteStop;

        public NetMeterServer()
        {
        }

        /**
         * Takes the command line arguments and uses them to determine how to
         * startup JMeter.
         * 
         * Called reflectively by {@link NewDriver#main(String[])}
         */
        public void Start(String[] args) 
        {

            CLArgsParser parser = new CLArgsParser(args, options);
            String error = parser.GetErrorString();

            if (null != error)
            {
                System.Console.WriteLine("Error: " + error);
                System.Console.WriteLine("Usage");
                System.Console.WriteLine(CLUtil.DescribeOptions(options).ToString());
                return;
            }
            try 
            {
                //initializeProperties(parser); // Also initialises JMeter logging

                ///*
                // * The following is needed for HTTPClient.
                // * (originally tried doing this in HTTPSampler2,
                // * but it appears that it was done too late when running in GUI mode)
                // * Set the commons logging default to Avalon Logkit, if not already defined
                // */
                //if (System.getProperty("org.apache.commons.logging.Log") == null) 
                //{ // $NON-NLS-1$
                //    System.setProperty("org.apache.commons.logging.Log" // $NON-NLS-1$
                //            , "org.apache.commons.logging.impl.LogKitLogger"); // $NON-NLS-1$
                //}

                
                //logProperty("java.version"); //$NON-NLS-1$
                //logProperty("java.vm.name"); //$NON-NLS-1$
                //logProperty("os.name"); //$NON-NLS-1$
                //logProperty("os.arch"); //$NON-NLS-1$
                //logProperty("os.version"); //$NON-NLS-1$
                //logProperty("file.encoding"); // $NON-NLS-1$
                //log.Info("NetMeterHome="     + NetMeterUtils.getJMeterHome());
                //logProperty("user.dir","  ="); //$NON-NLS-1$
                //log.Info("PWD       =" + new File(".").getCanonicalPath());//$NON-NLS-1$
                //log.Info("IP: "+NetMeterUtils.getLocalHostIP()
                //        +" Name: "+NetMeterUtils.getLocalHostName()
                //        +" FullName: "+NetMeterUtils.getLocalHostFullName());

                //updateClassLoader();
                //if (log.IsDebugEnabled)
                //{
                //    String jcp = System.getProperty("java.class.path");// $NON-NLS-1$
                //    String[] bits = jcp.Split(File.pathSeparator);
                //    log.Debug("ClassPath");
                //    foreach(String bit in bits)
                //    {
                //        log.Debug(bit);
                //    }
                //    log.Debug(jcp);
                //}

                // Set some (hopefully!) useful properties
                Int64 now = DateTime.Now.Ticks;
                NetMeterUtils.setProperty("START.MS", now.ToString());// $NON-NLS-1$
                DateTime today = DateTime.Now; // so it agrees with above
                // TODO perhaps should share code with __time() function for this...
                //NetMeterUtils.setProperty("START.YMD",new SimpleDateFormat("yyyyMMdd").format(today));// $NON-NLS-1$ $NON-NLS-2$
                //NetMeterUtils.setProperty("START.HMS",new SimpleDateFormat("HHmmss").format(today));// $NON-NLS-1$ $NON-NLS-2$

                if (parser.GetArgumentById(SERVER_OPT) != null)
                {
                    // Start the server
                    try
                    {
                        RemoteEngineImpl.StartServer(0); // $NON-NLS-1$
                    } 
                    catch (Exception ex)
                    {
                        System.Console.WriteLine("Server failed to start: "+ex);
                        log.Error("Giving up, as server failed with:", ex);
                        throw ex;
                    }
                    //startOptionalServers();
                }
                else 
                {
                    String testFile=null;
                    CLOption testFileOpt = parser.GetArgumentById(TESTFILE_OPT);
                    if (testFileOpt != null)
                    {
                        testFile = testFileOpt.GetArgument();
                        if (USE_LAST_JMX.Equals(testFile))
                        {
                            testFile = LoadRecentProject.getRecentFile(0);// most recent
                        }
                    }

                    CLOption rem=parser.GetArgumentById(REMOTE_OPT_PARAM);
                    if (rem == null)
                    { 
                        rem = parser.GetArgumentById(REMOTE_OPT); 
                    }
                    CLOption jtl = parser.GetArgumentById(LOGFILE_OPT);
                    String jtlFile = null;
                    //if (jtl != null)
                    //{
                    //    jtlFile=processLAST(jtl.GetArgument(), ".jtl"); // $NON-NLS-1$
                    //}
                    StartNonGui(testFile, jtlFile, rem);

                }
            } 
            //catch (IllegalUserActionException e) 
            //{
            //    System.Console.WriteLine(e);
            //    System.Console.WriteLine("Incorrect Usage");
            //    System.Console.WriteLine(CLUtil.DescribeOptions(options).toString());
            //}
            catch (Exception ex) 
            {
                log.Fatal("An error occurred: ",ex);
                System.Console.WriteLine("An error occurred: " + ex.Message);
                return;
            }
        }


        // Update classloader if necessary
        //private void updateClassLoader() 
        //{
        //    updatePath("search_paths",";"); //$NON-NLS-1$//$NON-NLS-2$
        //    updatePath("user.classpath",File.pathSeparator);//$NON-NLS-1$
        //}

        //private void updatePath(String property, String sep) 
        //{
        //    String userpath= NetMeterUtils.getPropDefault(property,"");// $NON-NLS-1$
        //    if (userpath.Length <= 0) { return; }
        //    log.Info(property+"="+userpath); //$NON-NLS-1$
        //    StringTokenizer tok = new StringTokenizer(userpath, sep);
        //    while(tok.hasMoreTokens())
        //    {
        //        String path=tok.nextToken();
        //        File f=new File(path);
        //        if (!f.canRead() && !f.isDirectory())
        //        {
        //            log.Warn("Can't read "+path);
        //        } 
        //        else
        //        {
        //            log.Info("Adding to classpath: "+path);
        //            try 
        //            {
        //                NewDriver.addPath(path);
        //            } 
        //            catch (MalformedURLException e) 
        //            {
        //                log.Warn("Error adding: "+path+" "+e.getLocalizedMessage());
        //            }
        //        }
        //    }
        //}

        //private void initializeProperties(CLArgsParser parser) 
        //{
        //    if (parser.GetArgumentById(PROPFILE_OPT) != null) 
        //    {
        //        NetMeterUtils.loadJMeterProperties(parser.GetArgumentById(PROPFILE_OPT).GetArgument());
        //    }
        //    else
        //    {
        //        NetMeterUtils.loadJMeterProperties(NewDriver.getJMeterDir() + File.separator
        //                + "bin" + File.separator // $NON-NLS-1$
        //                + "jmeter.properties");// $NON-NLS-1$
        //    }

        //    if (parser.GetArgumentById(JMLOGFILE_OPT) != null){
        //        String jmlogfile=parser.GetArgumentById(JMLOGFILE_OPT).GetArgument();
        //        jmlogfile = processLAST(jmlogfile, ".log");// $NON-NLS-1$
        //        NetMeterUtils.setProperty(LoggingManager.LOG_FILE,jmlogfile);
        //    }

        //    NetMeterUtils.initLogging();
        //    // Bug 33845 - allow direct override of Home dir
        //    if (parser.GetArgumentById(JMETER_HOME_OPT) == null) {
        //        NetMeterUtils.setJMeterHome(NewDriver.getJMeterDir());
        //    } else {
        //        NetMeterUtils.setJMeterHome(parser.GetArgumentById(JMETER_HOME_OPT).GetArgument());
        //    }

        //    Properties jmeterProps = NetMeterUtils.getJMeterProperties();
        //    remoteProps = new Properties();

        //    // Add local JMeter properties, if the file is found
        //    String userProp = NetMeterUtils.getPropDefault("user.properties",""); //$NON-NLS-1$
        //    if (userProp.length() > 0){ //$NON-NLS-1$
        //        FileInputStream fis=null;
        //        try {
        //            File file = NetMeterUtils.findFile(userProp);
        //            if (file.canRead()){
        //                log.Info("Loading user properties from: "+file.getCanonicalPath());
        //                fis = new FileInputStream(file);
        //                Properties tmp = new Properties();
        //                tmp.load(fis);
        //                jmeterProps.putAll(tmp);
        //                LoggingManager.setLoggingLevels(tmp);//Do what would be done earlier
        //            }
        //        } 
        //        catch (IOException e) 
        //        {
        //            log.Warn("Error loading user property file: " + userProp, e);
        //        } 
        //        finally
        //        {
        //            JOrphanUtils.closeQuietly(fis);
        //        }
        //    }

        //    // Add local system properties, if the file is found
        //    String sysProp = NetMeterUtils.getPropDefault("system.properties",""); //$NON-NLS-1$
        //    if (sysProp.length() > 0)
        //    {
        //        FileInputStream fis=null;
        //        try 
        //        {
        //            File file = NetMeterUtils.findFile(sysProp);
        //            if (file())
        //            {
        //                log.Info("Loading system properties from: "+file.getCanonicalPath());
        //                fis = new FileInputStream(file);
        //                System.getProperties().load(fis);
        //            }
        //        } 
        //        catch (IOException e) 
        //        {
        //            log.Warn("Error loading system property file: " + sysProp, e);
        //        } finally {
        //            JOrphanUtils.closeQuietly(fis);
        //        }
        //    }

        //    // Process command line property definitions
        //    // These can potentially occur multiple times

        //    List<CLOption> clOptions = parser.GetArguments();
        //    int size = clOptions.Count;

        //    for (int i = 0; i < size; i++) 
        //    {
        //        CLOption option = clOptions[i];
        //        String name = option.GetArgument(0);
        //        String value = option.GetArgument(1);
        //        FileInputStream fis = null;

        //        switch (option.GetDescriptor().GetId()) 
        //        {

        //        // Should not have any text arguments
        //        case CLOption.TEXT_ARGUMENT:
        //            throw new IllegalArgumentException("Unknown arg: "+option.GetArgument());

        //        case PROPFILE2_OPT: // Bug 33920 - allow multiple props
        //            try 
        //            {
        //                fis = new FileInputStream(new File(name));
        //                Properties tmp = new Properties();
        //                tmp.load(fis);
        //                jmeterProps.putAll(tmp);
        //                LoggingManager.setLoggingLevels(tmp);//Do what would be done earlier
        //            } 
        //            catch (FileNotFoundException e) 
        //            {
        //                log.Warn("Can't find additional property file: " + name, e);
        //            } 
        //            catch (IOException e)
        //            {
        //                log.Warn("Error loading additional property file: " + name, e);
        //            } 
        //            finally 
        //            {
        //                JOrphanUtils.closeQuietly(fis);
        //            }
        //            break;
        //        case SYSTEM_PROPFILE:
        //            log.Info("Setting System properties from file: " + name);
        //            try 
        //            {
        //                fis = new FileInputStream(new File(name));
        //                System.getProperties().load(fis);
        //            } 
        //            catch (IOException e) 
        //            {
        //                log.Warn("Cannot find system property file "+e.getLocalizedMessage());
        //            } 
        //            finally 
        //            {
        //                JOrphanUtils.closeQuietly(fis);
        //            }
        //            break;
        //        case SYSTEM_PROPERTY:
        //            if (value.length() > 0) 
        //            { // Set it
        //                log.Info("Setting System property: " + name + "=" + value);
        //                System.getProperties().setProperty(name, value);
        //            }
        //            else 
        //            { // Reset it
        //                log.Warn("Removing System property: " + name);
        //                System.getProperties().remove(name);
        //            }
        //            break;
        //        case JMETER_PROPERTY:
        //            if (value.length() > 0) { // Set it
        //                log.Info("Setting JMeter property: " + name + "=" + value);
        //                jmeterProps.setProperty(name, value);
        //            } else { // Reset it
        //                log.Warn("Removing JMeter property: " + name);
        //                jmeterProps.remove(name);
        //            }
        //            break;
        //        case JMETER_GLOBAL_PROP:
        //            if (value.length() > 0) { // Set it
        //                log.Info("Setting Global property: " + name + "=" + value);
        //                remoteProps.setProperty(name, value);
        //            } else {
        //                File propFile = new File(name);
        //                if (propFile.canRead()) {
        //                    log.Info("Setting Global properties from the file " + name);
        //                    try 
        //                    {
        //                        fis = new FileInputStream(propFile);
        //                        remoteProps.load(fis);
        //                    } 
        //                    catch (FileNotFoundException e)
        //                    {
        //                        log.Warn("Could not find properties file: "+e.Message);
        //                    } 
        //                    catch (IOException e)
        //                    {
        //                        log.Warn("Could not load properties file: " + e.Message);
        //                    } 
        //                    finally 
        //                    {
        //                        JOrphanUtils.closeQuietly(fis);
        //                    }
        //                }
        //            }
        //            break;
        //        case LOGLEVEL:
        //            if (value.Length > 0) 
        //            { // Set category
        //                log.Info("LogLevel: " + name + "=" + value);
        //                LoggingManager.setPriority(value, name);
        //            } else { // Set root level
        //                log.Warn("LogLevel: " + name);
        //                LoggingManager.setPriority(name);
        //            }
        //            break;
        //        case REMOTE_STOP:
        //            remoteStop = true;
        //            break;
        //        default:
        //            // ignored
        //            break;
        //        }
        //    }

        //    String sample_variables = (String) jmeterProps.get(SampleEvent.SAMPLE_VARIABLES);
        //    if (sample_variables != null){
        //        remoteProps.put(SampleEvent.SAMPLE_VARIABLES, sample_variables);
        //    }
        //    jmeterProps.put("jmeter.version", NetMeterUtils.getJMeterVersion());
        //}

        /*
         * Checks for LAST or LASTsuffix.
         * Returns the LAST name with .JMX replaced by suffix.
         */
        //private String processLAST(String jmlogfile, String suffix) 
        //{
        //    if (USE_LAST_JMX.Equals(jmlogfile) || USE_LAST_JMX.concat(suffix).equals(jmlogfile)){
        //        String last = LoadRecentProject.getRecentFile(0);// most recent
        //        String JMX_SUFFIX = ".JMX"; // $NON-NLS-1$
        //        if (last.ToUpper().EndsWith(JMX_SUFFIX))
        //        {
        //            jmlogfile = last.Substring(0, last.Length - JMX_SUFFIX.Length).concat(suffix);
        //        }
        //    }
        //    return jmlogfile;
        //}

        private void StartNonGui(String testFile, String logFile, CLOption remoteStart)
        {
            // add a system property so samplers can check to see if JMeter
            // is running in NonGui mode
            //System.setProperty(JMETER_NON_GUI, "true");// $NON-NLS-1$
            NetMeterServer driver = new NetMeterServer();// TODO - why does it create a new instance?
            driver.remoteProps = this.remoteProps;
            driver.remoteStop = this.remoteStop;
            driver.parent = this;

            String remote_hosts_string = null;
            if (remoteStart != null) 
            {
                remote_hosts_string = remoteStart.GetArgument();
                if (remote_hosts_string == null) 
                {
                    remote_hosts_string = "127.0.0.1";
                }
            }
            if (testFile == null) 
            {
                throw new Exception("Non-GUI runs require a test plan");
            }
            driver.RunNonGui(testFile, logFile, remoteStart != null, remote_hosts_string);
        }

        // run test in batch mode
        private void RunNonGui(String testFile, String logFile, Boolean remoteStart, String remote_hosts_string) 
        {
            FileStream reader = null;
            try 
            {
                if (!File.Exists(testFile)) 
                {
                    println("Could not open " + testFile);
                    return;
                }
                //FileServer.getFileServer().setBaseForScript(f);

                reader = new FileStream(testFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                log.Info("Loading file: " + testFile);

                HashTree tree = IOService.loadTree(reader);

                // Deliberate use of deprecated ctor
                NetMeterTreeModel treeModel = new NetMeterTreeModel(new Object());// Create non-GUI version to avoid headless problems
                NetMeterTreeNode root = (NetMeterTreeNode)treeModel.getRoot();
                treeModel.addSubTree(tree, root);

                // Hack to resolve ModuleControllers in non GUI mode
                SearchByType<ReplaceableController> replaceableControllers = new SearchByType<ReplaceableController>();
                tree.Traverse(replaceableControllers);
                List<ReplaceableController> replaceableControllersRes = replaceableControllers.GetSearchResults();

                foreach (ReplaceableController controller in replaceableControllersRes)
                {
                    controller.resolveReplacementSubTree(root);
                }

                // Remove the disabled items
                // For GUI runs this is done in Start.java
                convertSubTree(tree);

                Summariser summer = null;
                String summariserName = "Summariser";//$NON-NLS-1$
                if (summariserName.Length > 0) 
                {
                    log.Info("Creating summariser <" + summariserName + ">");
                    println("Creating summariser <" + summariserName + ">");
                    summer = new Summariser(summariserName);
                }

                if (logFile != null)
                {
                    ResultCollector logger = new ResultCollector(summer);
                    logger.setFilename(logFile);
                    tree.Add(tree.GetArray()[0], logger);
                }
                else
                {
                    // only add Summariser if it can not be shared with the ResultCollector
                    if (summer != null) 
                    {
                        tree.Add(tree.GetArray()[0], summer);
                    }
                }

                LinkedList<NetMeterEngine> engines = new LinkedList<NetMeterEngine>();
                tree.Put(tree.GetArray()[0], new ListenToTest(parent, (remoteStart && remoteStop) ? engines : null));
                println("Created the tree successfully using "+testFile);
                if (!remoteStart) 
                {
                    NetMeterEngine engine = new StandardNetMeterEngine();
                    engine.Configure(tree);
                    Int64 now = DateTime.Now.Ticks;
                    println("Starting the test @ " + DateTime.Now.ToString() + " (" + now + ")");
                    engine.RunTest();
                    engines.AddLast(engine);
                } 
                //else 
                //{
                //    java.util.StringTokenizer st = new java.util.StringTokenizer(remote_hosts_string, ",");//$NON-NLS-1$
                //    List<String> failingEngines = new ArrayList<String>(st.countTokens());
                //    while (st.hasMoreElements()) 
                //    {
                //        String el = (String) st.nextElement();
                //        println("Configuring remote engine for " + el);
                //        log.info("Configuring remote engine for " + el);
                //        JMeterEngine eng = doRemoteInit(el.trim(), tree);
                //        if (null != eng) 
                //        {
                //            engines.add(eng);
                //        } 
                //        else 
                //        {
                //            failingEngines.add(el);
                //            println("Failed to configure "+el);
                //        }
                //    }
                //    if (engines.isEmpty())
                //    {
                //        println("No remote engines were started.");
                //        return;
                //    }
                //    if(failingEngines.size()>0) {
                //        throw new IllegalArgumentException("The following remote engines could not be configured:"+failingEngines);
                //    }
                //    println("Starting remote engines");
                //    log.Info("Starting remote engines");
                //    long now = System.currentTimeMillis();
                //    println("Starting the test @ "+new Date(now)+" ("+now+")");
                //    foreach (NetMeterEngine engine in engines) 
                //    {
                //        engine.runTest();
                //    }
                //    println("Remote engines have been started");
                //    log.Info("Remote engines have been started");
                //}
                StartUdpDdaemon(engines);
            } 
            catch (Exception e) 
            {
                System.Console.WriteLine("Error in NonGUIDriver " + e.Message);
                log.Error("Error in NonGUIDriver", e);
            } 
            finally 
            {
                JOrphanUtils.closeQuietly(reader);
            }
        }

        /**
         * Refactored from AbstractAction.java
         *
         * @param tree
         */
        public static void convertSubTree(HashTree tree) 
        {
            LinkedList<Object> copyList = new LinkedList<Object>(tree.list());
            foreach (Object obj  in copyList) 
            {
                if (typeof(TestElement).IsAssignableFrom(obj.GetType())) 
                {
                    TestElement item = (TestElement) obj;
                    if (item.isEnabled()) 
                    {
                        if (typeof(ReplaceableController).IsAssignableFrom(item.GetType())) 
                        {
                            ReplaceableController rc;

                            // TODO this bit of code needs to be tidied up
                            // Unfortunately ModuleController is in components, not core
                            if (item.GetType().Name.Equals("org.apache.jmeter.control.ModuleController"))
                            { // Bug 47165
                                rc = (ReplaceableController) item;
                            } 
                            else 
                            {
                                // HACK: force the controller to load its tree
                                rc = (ReplaceableController) item.Clone();
                            }

                            HashTree subTree = tree.GetTree(item);
                            if (subTree != null)
                            {
                                HashTree replacementTree = rc.getReplacementSubTree();
                                if (replacementTree != null) 
                                {
                                    convertSubTree(replacementTree);
                                    tree.Replace(item, rc);
                                    tree.Set(rc, replacementTree);
                                }
                            } 
                            else
                            { // null subTree
                                convertSubTree(tree.GetTree(item));
                            }
                        } 
                        else
                        { // not Replaceable Controller
                            convertSubTree(tree.GetTree(item));
                        }
                    } 
                    else 
                    { // Not enabled
                        tree.Remove(item);
                    }
                } 
                else
                { // Not a TestElement
                    NetMeterTreeNode item = (NetMeterTreeNode) obj;
                    if (item.isEnabled()) 
                    {
                        // Replacement only needs to occur when starting the engine
                        // @see StandardJMeterEngine.run()
                        if (item.getUserObject() is ReplaceableController) 
                        {
                            ReplaceableController rc = (ReplaceableController) item.getTestElement();
                            HashTree subTree = tree.GetTree(item);

                            if (subTree != null) 
                            {
                                HashTree replacementTree = rc.getReplacementSubTree();
                                if (replacementTree != null) 
                                {
                                    convertSubTree(replacementTree);
                                    tree.Replace(item, rc);
                                    tree.Set(rc, replacementTree);
                                }
                            }
                        } 
                        else 
                        { // Not a ReplaceableController
                            convertSubTree(tree.GetTree(item));
                            TestElement testElement = item.getTestElement();
                            tree.Replace(item, testElement);
                        }
                     }
                    else
                    { // Not enabled
                        tree.Remove(item);
                    }
                }
            }
        }

        private NetMeterEngine doRemoteInit(String hostName, HashTree testTree)
        {
            NetMeterEngine engine = null;
            try 
            {
                engine = new ClientEngine(hostName);
            } 
            catch (Exception e) 
            {
                log.Fatal("Failure connecting to remote host: "+hostName, e);
                System.Console.WriteLine("Failure connecting to remote host: {0}, {1}", hostName, e.Message);
                return null;
            }
            engine.Configure(testTree);
            if (!remoteProps.isEmpty())
            {
                engine.setProperties(remoteProps);
            }
            return engine;
        }

        /*
         * Listen to test and handle tidyup after non-GUI test completes.
         * If running a remote test, then after waiting a few seconds for listeners to finish files,
         * it calls ClientJMeterEngine.tidyRMI() to deal with the Naming Timer Thread.
         */
        private class ListenToTest : TestStateListener 
        {
            private Int64 started = 0; // keep track of remote tests

            //NOT YET USED private JMeter _parent;

            private LinkedList<NetMeterEngine> engines;

            /**
             * @param unused JMeter unused for now
             * @param engines List<JMeterEngine>
             */
            public ListenToTest(NetMeterServer unused, LinkedList<NetMeterEngine> engines) 
            {
                //_parent = unused;
                this.engines = engines;
            }

            public new void TestEnded(String host)
            {
                Int64 now = DateTime.Now.Ticks;
                log.Info("Finished remote host: " + host + " ("+now+")");
                if (Interlocked.Decrement(ref started) <= 0)
                {
                    Thread stopSoon = new Thread(Run);
                    stopSoon.Start();
                }
            }

            public new void TestEnded() 
            {
                Int64 now = DateTime.Now.Ticks;
                println("Tidying up ...    @ " + DateTime.Now.ToString() + " ("+now+")");
                println("... end of run");
                CheckForRemainingThreads();
            }

            public new void testStarted(String host) 
            {
                Interlocked.Increment(ref started);
                Int64 now = DateTime.Now.Ticks;
                log.Info("Started remote host:  " + host + " ("+now+")");
            }

            public new void testStarted() 
            {
                Int64 now = DateTime.Now.Ticks;
                //log.Info(NetMeterUtils.getResString("running_test")+" ("+now+")");//$NON-NLS-1$
            }

            /**
             * This is a hack to allow listeners a chance to close their files. Must
             * implement a queue for sample responses tied to the engine, and the
             * engine won't deliver testEnded signal till all sample responses have
             * been delivered. Should also improve performance of remote JMeter
             * testing.
             */
            public new void Run() 
            {
                Int64 now = DateTime.Now.Ticks;
                println("Tidying up remote @ " + DateTime.Now.ToString() + " (" + now + ")");
                if (engines != null)
                { // it will be null unless remoteStop = true
                    println("Exitting remote servers");
                    foreach (NetMeterEngine engine in engines)
                    {
                        engine.Exit();
                    }
                }
                try 
                {
                    Thread.Sleep(5000); // Allow listeners to close files
                } 
                catch (Exception ignored) 
                {
                }
                ClientEngine.tidyRMI(log);
                println("... end of run");
                CheckForRemainingThreads();
            }

            /**
             * Runs daemon thread which waits a short while; 
             * if JVM does not exit, lists remaining non-daemon threads on stdout.
             */
            private void CheckForRemainingThreads()
            {
                // This cannot be a JMeter class variable, because properties
                // are not initialised until later.
                int REMAIN_THREAD_PAUSE = 2000; // $NON-NLS-1$ 
            
                if (REMAIN_THREAD_PAUSE > 0) 
                {
                    Thread daemon = new Thread(
                        () =>
                            {
                                try 
                                {
                                    Thread.Sleep(REMAIN_THREAD_PAUSE); // Allow enough time for JVM to exit
                                } 
                                catch (Exception ignored) 
                                {
                                }
                                // This is a daemon thread, which should only reach here if there are other
                                // non-daemon threads still active
                                System.Console.WriteLine("The JVM should have exitted but did not.");
                                System.Console.WriteLine("The following non-daemon threads are still running (DestroyJavaVM is OK):");
                                //JOrphanUtils.displayThreads(false);
                            }
                        );
                    daemon.IsBackground = true;
                    //daemon.setDaemon(true);
                    daemon.Start();
                }
            }

        }

        private static void println(String str) 
        {
            System.Console.WriteLine(str);
        }

        private static sealed String[,] DEFAULT_ICONS = 
        {
            { "org.apache.jmeter.control.gui.TestPlanGui",               "org/apache/jmeter/images/beaker.gif" },     //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.timers.gui.AbstractTimerGui",           "org/apache/jmeter/images/timer.gif" },      //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.threads.gui.ThreadGroupGui",            "org/apache/jmeter/images/thread.gif" },     //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.visualizers.gui.AbstractListenerGui",   "org/apache/jmeter/images/meter.png" },      //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.config.gui.AbstractConfigGui",          "org/apache/jmeter/images/testtubes.png" },  //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.processor.gui.AbstractPreProcessorGui", "org/apache/jmeter/images/leafnode.gif"},    //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.processor.gui.AbstractPostProcessorGui","org/apache/jmeter/images/leafnodeflip.gif"},//$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.control.gui.AbstractControllerGui",     "org/apache/jmeter/images/knob.gif" },       //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.control.gui.WorkBenchGui",              "org/apache/jmeter/images/clipboard.gif" },  //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.samplers.gui.AbstractSamplerGui",       "org/apache/jmeter/images/pipet.png" },      //$NON-NLS-1$ $NON-NLS-2$
            { "org.apache.jmeter.assertions.gui.AbstractAssertionGui",   "org/apache/jmeter/images/question.gif"}     //$NON-NLS-1$ $NON-NLS-2$
        };

        //public String[][] GetIconMappings() 
        //{
        //    String defaultIconProp = "org/apache/jmeter/images/icon.properties"; //$NON-NLS-1$
        //    String iconProp = JMeterUtils.getPropDefault("jmeter.icons", defaultIconProp);//$NON-NLS-1$
        //    Properties p = JMeterUtils.loadProperties(iconProp);
        //    if (p == null && !iconProp.equals(defaultIconProp)) 
        //    {
        //        log.info(iconProp + " not found - using " + defaultIconProp);
        //        iconProp = defaultIconProp;
        //        p = JMeterUtils.loadProperties(iconProp);
        //    }
        //    if (p == null) {
        //        log.info(iconProp + " not found - using inbuilt icon set");
        //        return DEFAULT_ICONS;
        //    }
        //    log.info("Loaded icon properties from " + iconProp);
        //    String[,] iconlist = new String[p.size(), 3];
        //    Enumeration<?> pe = p.keys();
        //    int i = 0;
        //    while (pe.hasMoreElements()) {
        //        String key = (String) pe.nextElement();
        //        String icons[] = JOrphanUtils.split(p.getProperty(key), " ");//$NON-NLS-1$
        //        iconlist[i][0] = key;
        //        iconlist[i][1] = icons[0];
        //        if (icons.length > 1) {
        //            iconlist[i][2] = icons[1];
        //        }
        //        i++;
        //    }
        //    return iconlist;
        //}

        //@Override
        //public String[][] getResourceBundles() {
        //    return new String[0][];
        //}

        /**
         * Check if JMeter is running in non-GUI mode.
         *
         * @return true if JMeter is running in non-GUI mode.
         */
        public static Boolean isNonGUI()
        {
            return true; //$NON-NLS-1$
        }

        //private void logProperty(String prop)
        //{
        //    log.Info(prop+"="+System.getProperty(prop));//$NON-NLS-1$
        //}

        //private void logProperty(String prop,String separator)
        //{
        //    log.Info(prop+separator+System.getProperty(prop));//$NON-NLS-1$
        //}

        private static void StartUdpDdaemon(LinkedList<NetMeterEngine> engines) 
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Loopback, UDP_PORT_DEFAULT);
            try
            {
                //UdpClient client = new UdpClient(RemoteIpEndPoint);
                Thread waiter = new Thread(
                    () =>
                        {
                            WaitForSignals(engines, RemoteIpEndPoint);
                        }
                    );
                //waiter.setDaemon(true);
                waiter.Start();
            }
            catch (SocketException ex)
            {
            	System.Console.WriteLine("Failed to create UDP port: {0}", ex.Message);
            }
        }

        private static void WaitForSignals(LinkedList<NetMeterEngine> engines, IPEndPoint ip) 
        {
            System.Console.WriteLine("Waiting for possible shutdown message on port 4455");

            UdpClient udpClient = new UdpClient(4455);
            //DatagramPacket request = new DatagramPacket(buf, buf.Length);
            try
            {
                while(true)
                {
                    Byte[] receiveBytes = udpClient.Receive(ref ip);
                    string command = Encoding.ASCII.GetString(receiveBytes);
                    System.Console.WriteLine("Command: " + command + " received from local");
                    log.Info("Command: " + command + " received from local");
                    if (command.Equals("StopTestNow"))
                    {
                        foreach (NetMeterEngine engine in engines)
                        {
                            engine.StopTest(true);
                        }
                    }
                    else if (command.Equals("Shutdown"))
                    {
                        foreach (NetMeterEngine engine in engines)
                        {
                            engine.StopTest(false);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Command: {0} not recognised ", command);
                    }
                }
            } 
            catch (SocketException e)
            {
                System.Console.WriteLine(e.Message);
            } 
            finally 
            {
                udpClient.Close();
            }
        }
    }
}
