using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.Engine;

namespace NetMeter
{
    public class NetMeterServer
    {
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
        public void start(String[] args) 
        {

            CLArgsParser parser = new CLArgsParser(args, options);
            String error = parser.getErrorString();

            if (null != error)
            {
                System.err.println("Error: " + error);
                System.out.println("Usage");
                System.out.println(CLUtil.describeOptions(options).toString());
                return;
            }
            try 
            {
                initializeProperties(parser); // Also initialises JMeter logging

                /*
                 * The following is needed for HTTPClient.
                 * (originally tried doing this in HTTPSampler2,
                 * but it appears that it was done too late when running in GUI mode)
                 * Set the commons logging default to Avalon Logkit, if not already defined
                 */
                if (System.getProperty("org.apache.commons.logging.Log") == null) 
                { // $NON-NLS-1$
                    System.setProperty("org.apache.commons.logging.Log" // $NON-NLS-1$
                            , "org.apache.commons.logging.impl.LogKitLogger"); // $NON-NLS-1$
                }

                //log.info(JMeterUtils.getJMeterCopyright());
                //log.info("Version " + JMeterUtils.getJMeterVersion());
                //logProperty("java.version"); //$NON-NLS-1$
                //logProperty("java.vm.name"); //$NON-NLS-1$
                //logProperty("os.name"); //$NON-NLS-1$
                //logProperty("os.arch"); //$NON-NLS-1$
                //logProperty("os.version"); //$NON-NLS-1$
                //logProperty("file.encoding"); // $NON-NLS-1$
                //log.info("Default Locale=" + Locale.getDefault().getDisplayName());
                //log.info("JMeter  Locale=" + JMeterUtils.getLocale().getDisplayName());
                //log.info("JMeterHome="     + JMeterUtils.getJMeterHome());
                //logProperty("user.dir","  ="); //$NON-NLS-1$
                //log.info("PWD       ="+new File(".").getCanonicalPath());//$NON-NLS-1$
                //log.info("IP: "+JMeterUtils.getLocalHostIP()
                //        +" Name: "+JMeterUtils.getLocalHostName()
                //        +" FullName: "+JMeterUtils.getLocalHostFullName());
                setProxy(parser);

                updateClassLoader();
                if (log.isDebugEnabled())
                {
                    String jcp=System.getProperty("java.class.path");// $NON-NLS-1$
                    String bits[] =jcp.split(File.pathSeparator);
                    log.debug("ClassPath");
                    for(String bit : bits){
                        log.debug(bit);
                    }
                    log.debug(jcp);
                }

                // Set some (hopefully!) useful properties
                long now=System.currentTimeMillis();
                JMeterUtils.setProperty("START.MS",Long.toString(now));// $NON-NLS-1$
                Date today=new Date(now); // so it agrees with above
                // TODO perhaps should share code with __time() function for this...
                JMeterUtils.setProperty("START.YMD",new SimpleDateFormat("yyyyMMdd").format(today));// $NON-NLS-1$ $NON-NLS-2$
                JMeterUtils.setProperty("START.HMS",new SimpleDateFormat("HHmmss").format(today));// $NON-NLS-1$ $NON-NLS-2$

                if (parser.getArgumentById(VERSION_OPT) != null) 
                {
                    System.out.println(JMeterUtils.getJMeterCopyright());
                    System.out.println("Version " + JMeterUtils.getJMeterVersion());
                } 
                else if (parser.getArgumentById(HELP_OPT) != null) 
                {
                    System.out.println(JMeterUtils.getResourceFileAsText("org/apache/jmeter/help.txt"));// $NON-NLS-1$
                } 
                else if (parser.getArgumentById(SERVER_OPT) != null)
                {
                    // Start the server
                    try {
                        RemoteJMeterEngineImpl.startServer(JMeterUtils.getPropDefault("server_port", 0)); // $NON-NLS-1$
                    } catch (Exception ex) {
                        System.err.println("Server failed to start: "+ex);
                        log.error("Giving up, as server failed with:", ex);
                        throw ex;
                    }
                    startOptionalServers();
                }
                else 
                {
                    String testFile=null;
                    CLOption testFileOpt = parser.getArgumentById(TESTFILE_OPT);
                    if (testFileOpt != null)
                    {
                        testFile = testFileOpt.getArgument();
                        if (USE_LAST_JMX.equals(testFile))
                        {
                            testFile = LoadRecentProject.getRecentFile(0);// most recent
                        }
                    }
                    if (parser.getArgumentById(NONGUI_OPT) == null) 
                    {
                        startGui(testFile);
                        startOptionalServers();
                    } 
                    else 
                    {
                        CLOption rem=parser.getArgumentById(REMOTE_OPT_PARAM);
                        if (rem==null)
                        { 
                            rem=parser.getArgumentById(REMOTE_OPT); 
                        }
                        CLOption jtl = parser.getArgumentById(LOGFILE_OPT);
                        String jtlFile = null;
                        if (jtl != null)
                        {
                            jtlFile=processLAST(jtl.getArgument(), ".jtl"); // $NON-NLS-1$
                        }
                        startNonGui(testFile, jtlFile, rem);
                        startOptionalServers();
                    }
                }
            } 
            catch (IllegalUserActionException e) 
            {
                System.out.println(e.getMessage());
                System.out.println("Incorrect Usage");
                System.out.println(CLUtil.describeOptions(options).toString());
            }
            catch (Throwable e) 
            {
                log.fatalError("An error occurred: ",e);
                System.out.println("An error occurred: " + e.getMessage());
                System.exit(1); // TODO - could this be return?
            }
        }


        // Update classloader if necessary
        private void updateClassLoader() 
        {
                updatePath("search_paths",";"); //$NON-NLS-1$//$NON-NLS-2$
                updatePath("user.classpath",File.pathSeparator);//$NON-NLS-1$
        }

        private void updatePath(String property, String sep) 
        {
            String userpath= JMeterUtils.getPropDefault(property,"");// $NON-NLS-1$
            if (userpath.length() <= 0) { return; }
            log.info(property+"="+userpath); //$NON-NLS-1$
            StringTokenizer tok = new StringTokenizer(userpath, sep);
            while(tok.hasMoreTokens()) {
                String path=tok.nextToken();
                File f=new File(path);
                if (!f.canRead() && !f.isDirectory()) {
                    log.warn("Can't read "+path);
                } else {
                    log.info("Adding to classpath: "+path);
                    try {
                        NewDriver.addPath(path);
                    } catch (MalformedURLException e) {
                        log.warn("Error adding: "+path+" "+e.getLocalizedMessage());
                    }
                }
            }
        }

        /**
         *
         */
        private void startOptionalServers() 
        {
            int bshport = JMeterUtils.getPropDefault("beanshell.server.port", 0);// $NON-NLS-1$
            String bshfile = JMeterUtils.getPropDefault("beanshell.server.file", "");// $NON-NLS-1$ $NON-NLS-2$
            if (bshport > 0) 
            {
                log.info("Starting Beanshell server (" + bshport + "," + bshfile + ")");
                Runnable t = new BeanShellServer(bshport, bshfile);
                t.run();
            }

            // Should we run a beanshell script on startup?
            String bshinit = JMeterUtils.getProperty("beanshell.init.file");// $NON-NLS-1$
            if (bshinit != null)
            {
                log.info("Run Beanshell on file: "+bshinit);
                try 
                {
                    BeanShellInterpreter bsi = new BeanShellInterpreter();//bshinit,log);
                    bsi.source(bshinit);
                } 
                catch (ClassNotFoundException e) 
                {
                    log.warn("Could not start Beanshell: "+e.getLocalizedMessage());
                } 
                catch (JMeterException e) 
                {
                    log.warn("Could not process Beanshell file: "+e.getLocalizedMessage());
                }
            }

            int mirrorPort=JMeterUtils.getPropDefault("mirror.server.port", 0);// $NON-NLS-1$
            if (mirrorPort > 0)
            {
                log.info("Starting Mirror server (" + mirrorPort + ")");
                try 
                {
                    Object instance = ClassTools.construct(
                            "org.apache.jmeter.protocol.http.control.HttpMirrorControl",// $NON-NLS-1$
                            mirrorPort);
                    ClassTools.invoke(instance,"startHttpMirror");
                }
                catch (JMeterException e)
                {
                    log.warn("Could not start Mirror server",e);
                }
            }
        }

        /**
         * Sets a proxy server for the JVM if the command line arguments are
         * specified.
         */
        private void setProxy(CLArgsParser parser) 
        {
            if (parser.getArgumentById(PROXY_USERNAME) != null) 
            {
                Properties jmeterProps = JMeterUtils.getJMeterProperties();
                if (parser.getArgumentById(PROXY_PASSWORD) != null) 
                {
                    String u, p;
                    Authenticator.setDefault(new ProxyAuthenticator(u = parser.getArgumentById(PROXY_USERNAME)
                            .getArgument(), p = parser.getArgumentById(PROXY_PASSWORD).getArgument()));
                    log.info("Set Proxy login: " + u + "/" + p);
                    jmeterProps.setProperty(HTTP_PROXY_USER, u);//for Httpclient
                    jmeterProps.setProperty(HTTP_PROXY_PASS, p);//for Httpclient
                } 
                else
                {
                    String u;
                    Authenticator.setDefault(new ProxyAuthenticator(u = parser.getArgumentById(PROXY_USERNAME)
                            .getArgument(), ""));
                    log.info("Set Proxy login: " + u);
                    jmeterProps.setProperty(HTTP_PROXY_USER, u);
                }
            }
            if (parser.getArgumentById(PROXY_HOST) != null && parser.getArgumentById(PROXY_PORT) != null) 
            {
                String h = parser.getArgumentById(PROXY_HOST).getArgument();
                String p = parser.getArgumentById(PROXY_PORT).getArgument();
                System.setProperty("http.proxyHost",  h );// $NON-NLS-1$
                System.setProperty("https.proxyHost", h);// $NON-NLS-1$
                System.setProperty("http.proxyPort",  p);// $NON-NLS-1$
                System.setProperty("https.proxyPort", p);// $NON-NLS-1$
                log.info("Set http[s].proxyHost: " + h + " Port: " + p);
            } 
            else if (parser.getArgumentById(PROXY_HOST) != null || parser.getArgumentById(PROXY_PORT) != null) 
            {
                throw new IllegalUserActionException(JMeterUtils.getResString("proxy_cl_error"));// $NON-NLS-1$
            }

            if (parser.getArgumentById(NONPROXY_HOSTS) != null) 
            {
                String n = parser.getArgumentById(NONPROXY_HOSTS).getArgument();
                System.setProperty("http.nonProxyHosts",  n );// $NON-NLS-1$
                System.setProperty("https.nonProxyHosts", n );// $NON-NLS-1$
                log.info("Set http[s].nonProxyHosts: "+n);
            }
        }

        private void initializeProperties(CLArgsParser parser) 
        {
            if (parser.getArgumentById(PROPFILE_OPT) != null) 
            {
                JMeterUtils.loadJMeterProperties(parser.getArgumentById(PROPFILE_OPT).getArgument());
            }
            else
            {
                JMeterUtils.loadJMeterProperties(NewDriver.getJMeterDir() + File.separator
                        + "bin" + File.separator // $NON-NLS-1$
                        + "jmeter.properties");// $NON-NLS-1$
            }

            if (parser.getArgumentById(JMLOGFILE_OPT) != null){
                String jmlogfile=parser.getArgumentById(JMLOGFILE_OPT).getArgument();
                jmlogfile = processLAST(jmlogfile, ".log");// $NON-NLS-1$
                JMeterUtils.setProperty(LoggingManager.LOG_FILE,jmlogfile);
            }

            JMeterUtils.initLogging();
            JMeterUtils.initLocale();
            // Bug 33845 - allow direct override of Home dir
            if (parser.getArgumentById(JMETER_HOME_OPT) == null) {
                JMeterUtils.setJMeterHome(NewDriver.getJMeterDir());
            } else {
                JMeterUtils.setJMeterHome(parser.getArgumentById(JMETER_HOME_OPT).getArgument());
            }

            Properties jmeterProps = JMeterUtils.getJMeterProperties();
            remoteProps = new Properties();

            // Add local JMeter properties, if the file is found
            String userProp = JMeterUtils.getPropDefault("user.properties",""); //$NON-NLS-1$
            if (userProp.length() > 0){ //$NON-NLS-1$
                FileInputStream fis=null;
                try {
                    File file = JMeterUtils.findFile(userProp);
                    if (file.canRead()){
                        log.info("Loading user properties from: "+file.getCanonicalPath());
                        fis = new FileInputStream(file);
                        Properties tmp = new Properties();
                        tmp.load(fis);
                        jmeterProps.putAll(tmp);
                        LoggingManager.setLoggingLevels(tmp);//Do what would be done earlier
                    }
                } catch (IOException e) {
                    log.warn("Error loading user property file: " + userProp, e);
                } finally {
                    JOrphanUtils.closeQuietly(fis);
                }
            }

            // Add local system properties, if the file is found
            String sysProp = JMeterUtils.getPropDefault("system.properties",""); //$NON-NLS-1$
            if (sysProp.length() > 0){
                FileInputStream fis=null;
                try {
                    File file = JMeterUtils.findFile(sysProp);
                    if (file.canRead()){
                        log.info("Loading system properties from: "+file.getCanonicalPath());
                        fis = new FileInputStream(file);
                        System.getProperties().load(fis);
                    }
                } catch (IOException e) {
                    log.warn("Error loading system property file: " + sysProp, e);
                } finally {
                    JOrphanUtils.closeQuietly(fis);
                }
            }

            // Process command line property definitions
            // These can potentially occur multiple times

            List<CLOption> clOptions = parser.getArguments();
            int size = clOptions.size();

            for (int i = 0; i < size; i++) {
                CLOption option = clOptions.get(i);
                String name = option.getArgument(0);
                String value = option.getArgument(1);
                FileInputStream fis = null;

                switch (option.getDescriptor().getId()) {

                // Should not have any text arguments
                case CLOption.TEXT_ARGUMENT:
                    throw new IllegalArgumentException("Unknown arg: "+option.getArgument());

                case PROPFILE2_OPT: // Bug 33920 - allow multiple props
                    try {
                        fis = new FileInputStream(new File(name));
                        Properties tmp = new Properties();
                        tmp.load(fis);
                        jmeterProps.putAll(tmp);
                        LoggingManager.setLoggingLevels(tmp);//Do what would be done earlier
                    } catch (FileNotFoundException e) {
                        log.warn("Can't find additional property file: " + name, e);
                    } catch (IOException e) {
                        log.warn("Error loading additional property file: " + name, e);
                    } finally {
                        JOrphanUtils.closeQuietly(fis);
                    }
                    break;
                case SYSTEM_PROPFILE:
                    log.info("Setting System properties from file: " + name);
                    try {
                        fis = new FileInputStream(new File(name));
                        System.getProperties().load(fis);
                    } catch (IOException e) {
                        log.warn("Cannot find system property file "+e.getLocalizedMessage());
                    } finally {
                        JOrphanUtils.closeQuietly(fis);
                    }
                    break;
                case SYSTEM_PROPERTY:
                    if (value.length() > 0) { // Set it
                        log.info("Setting System property: " + name + "=" + value);
                        System.getProperties().setProperty(name, value);
                    } else { // Reset it
                        log.warn("Removing System property: " + name);
                        System.getProperties().remove(name);
                    }
                    break;
                case JMETER_PROPERTY:
                    if (value.length() > 0) { // Set it
                        log.info("Setting JMeter property: " + name + "=" + value);
                        jmeterProps.setProperty(name, value);
                    } else { // Reset it
                        log.warn("Removing JMeter property: " + name);
                        jmeterProps.remove(name);
                    }
                    break;
                case JMETER_GLOBAL_PROP:
                    if (value.length() > 0) { // Set it
                        log.info("Setting Global property: " + name + "=" + value);
                        remoteProps.setProperty(name, value);
                    } else {
                        File propFile = new File(name);
                        if (propFile.canRead()) {
                            log.info("Setting Global properties from the file "+name);
                            try {
                                fis = new FileInputStream(propFile);
                                remoteProps.load(fis);
                            } catch (FileNotFoundException e) {
                                log.warn("Could not find properties file: "+e.getLocalizedMessage());
                            } catch (IOException e) {
                                log.warn("Could not load properties file: "+e.getLocalizedMessage());
                            } finally {
                                JOrphanUtils.closeQuietly(fis);
                            }
                        }
                    }
                    break;
                case LOGLEVEL:
                    if (value.length() > 0) { // Set category
                        log.info("LogLevel: " + name + "=" + value);
                        LoggingManager.setPriority(value, name);
                    } else { // Set root level
                        log.warn("LogLevel: " + name);
                        LoggingManager.setPriority(name);
                    }
                    break;
                case REMOTE_STOP:
                    remoteStop = true;
                    break;
                default:
                    // ignored
                    break;
                }
            }

            String sample_variables = (String) jmeterProps.get(SampleEvent.SAMPLE_VARIABLES);
            if (sample_variables != null){
                remoteProps.put(SampleEvent.SAMPLE_VARIABLES, sample_variables);
            }
            jmeterProps.put("jmeter.version", JMeterUtils.getJMeterVersion());
        }

        /*
         * Checks for LAST or LASTsuffix.
         * Returns the LAST name with .JMX replaced by suffix.
         */
        private String processLAST(String jmlogfile, String suffix) 
        {
            if (USE_LAST_JMX.equals(jmlogfile) || USE_LAST_JMX.concat(suffix).equals(jmlogfile)){
                String last = LoadRecentProject.getRecentFile(0);// most recent
                final String JMX_SUFFIX = ".JMX"; // $NON-NLS-1$
                if (last.toUpperCase(Locale.ENGLISH).endsWith(JMX_SUFFIX)){
                    jmlogfile=last.substring(0, last.length() - JMX_SUFFIX.length()).concat(suffix);
                }
            }
            return jmlogfile;
        }

        private void startNonGui(String testFile, String logFile, CLOption remoteStart)
        {
            // add a system property so samplers can check to see if JMeter
            // is running in NonGui mode
            System.setProperty(JMETER_NON_GUI, "true");// $NON-NLS-1$
            JMeter driver = new JMeter();// TODO - why does it create a new instance?
            driver.remoteProps = this.remoteProps;
            driver.remoteStop = this.remoteStop;
            driver.parent = this;
            PluginManager.install(this, false);

            String remote_hosts_string = null;
            if (remoteStart != null) 
            {
                remote_hosts_string = remoteStart.getArgument();
                if (remote_hosts_string == null) 
                {
                    remote_hosts_string = "127.0.0.1";
                }
            }
            if (testFile == null) 
            {
                throw new IllegalUserActionException("Non-GUI runs require a test plan");
            }
            driver.runNonGui(testFile, logFile, remoteStart != null, remote_hosts_string);
        }

        // run test in batch mode
        private void runNonGui(String testFile, String logFile, Boolean remoteStart, String remote_hosts_string) 
        {
            FileInputStream reader = null;
            try 
            {
                File f = new File(testFile);
                if (!f.exists() || !f.isFile()) 
                {
                    println("Could not open " + testFile);
                    return;
                }
                FileServer.getFileServer().setBaseForScript(f);

                reader = new FileInputStream(f);
                log.info("Loading file: " + f);

                HashTree tree = SaveService.loadTree(reader);

                // Deliberate use of deprecated ctor
                JMeterTreeModel treeModel = new JMeterTreeModel(new Object());// Create non-GUI version to avoid headless problems
                JMeterTreeNode root = (JMeterTreeNode) treeModel.getRoot();
                treeModel.addSubTree(tree, root);

                // Hack to resolve ModuleControllers in non GUI mode
                SearchByType<ReplaceableController> replaceableControllers = new SearchByType<ReplaceableController>();
                tree.traverse(replaceableControllers);
                Collection<ReplaceableController> replaceableControllersRes = replaceableControllers.getSearchResults();
                for (Iterator<ReplaceableController> iter = replaceableControllersRes.iterator(); iter.hasNext();) 
                {
                    ReplaceableController replaceableController = iter.next();
                    replaceableController.resolveReplacementSubTree(root);
                }

                // Remove the disabled items
                // For GUI runs this is done in Start.java
                convertSubTree(tree);

                Summariser summer = null;
                String summariserName = JMeterUtils.getPropDefault("summariser.name", "");//$NON-NLS-1$
                if (summariserName.length() > 0) {
                    log.info("Creating summariser <" + summariserName + ">");
                    println("Creating summariser <" + summariserName + ">");
                    summer = new Summariser(summariserName);
                }

                if (logFile != null)
                {
                    ResultCollector logger = new ResultCollector(summer);
                    logger.setFilename(logFile);
                    tree.add(tree.getArray()[0], logger);
                }
                else
                {
                    // only add Summariser if it can not be shared with the ResultCollector
                    if (summer != null) 
                    {
                        tree.add(tree.getArray()[0], summer);
                    }
                }

                List<JMeterEngine> engines = new LinkedList<JMeterEngine>();
                tree.add(tree.getArray()[0], new ListenToTest(parent, (remoteStart && remoteStop) ? engines : null));
                println("Created the tree successfully using "+testFile);
                if (!remoteStart) 
                {
                    JMeterEngine engine = new StandardJMeterEngine();
                    engine.configure(tree);
                    long now=System.currentTimeMillis();
                    println("Starting the test @ "+new Date(now)+" ("+now+")");
                    engine.runTest();
                    engines.add(engine);
                } 
                else 
                {
                    java.util.StringTokenizer st = new java.util.StringTokenizer(remote_hosts_string, ",");//$NON-NLS-1$
                    List<String> failingEngines = new ArrayList<String>(st.countTokens());
                    while (st.hasMoreElements()) 
                    {
                        String el = (String) st.nextElement();
                        println("Configuring remote engine for " + el);
                        log.info("Configuring remote engine for " + el);
                        JMeterEngine eng = doRemoteInit(el.trim(), tree);
                        if (null != eng) 
                        {
                            engines.add(eng);
                        } 
                        else 
                        {
                            failingEngines.add(el);
                            println("Failed to configure "+el);
                        }
                    }
                    if (engines.isEmpty())
                    {
                        println("No remote engines were started.");
                        return;
                    }
                    if(failingEngines.size()>0) {
                        throw new IllegalArgumentException("The following remote engines could not be configured:"+failingEngines);
                    }
                    println("Starting remote engines");
                    log.info("Starting remote engines");
                    long now=System.currentTimeMillis();
                    println("Starting the test @ "+new Date(now)+" ("+now+")");
                    for (JMeterEngine engine : engines) {
                        engine.runTest();
                    }
                    println("Remote engines have been started");
                    log.info("Remote engines have been started");
                }
                startUdpDdaemon(engines);
            } 
            catch (Exception e) 
            {
                System.out.println("Error in NonGUIDriver " + e.toString());
                log.error("Error in NonGUIDriver", e);
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
                if (obj is TestElement) 
                {
                    TestElement item = (TestElement) obj;
                    if (item.isEnabled()) 
                    {
                        if (item is ReplaceableController) 
                        {
                            ReplaceableController rc;

                            // TODO this bit of code needs to be tidied up
                            // Unfortunately ModuleController is in components, not core
                            if (item.getClass().getName().equals("org.apache.jmeter.control.ModuleController"))
                            { // Bug 47165
                                rc = (ReplaceableController) item;
                            } 
                            else 
                            {
                                // HACK: force the controller to load its tree
                                rc = (ReplaceableController) item.clone();
                            }

                            HashTree subTree = tree.getTree(item);
                            if (subTree != null)
                            {
                                HashTree replacementTree = rc.getReplacementSubTree();
                                if (replacementTree != null) 
                                {
                                    convertSubTree(replacementTree);
                                    tree.replace(item, rc);
                                    tree.set(rc, replacementTree);
                                }
                            } 
                            else
                            { // null subTree
                                convertSubTree(tree.getTree(item));
                            }
                        } 
                        else
                        { // not Replaceable Controller
                            convertSubTree(tree.getTree(item));
                        }
                    } 
                    else 
                    { // Not enabled
                        tree.remove(item);
                    }
                } 
                else
                { // Not a TestElement
                    JMeterTreeNode item = (JMeterTreeNode) obj;
                    if (item.isEnabled()) 
                    {
                        // Replacement only needs to occur when starting the engine
                        // @see StandardJMeterEngine.run()
                        if (item.getUserObject() is ReplaceableController) 
                        {
                            ReplaceableController rc = (ReplaceableController) item.getTestElement();
                            HashTree subTree = tree.getTree(item);

                            if (subTree != null) 
                            {
                                HashTree replacementTree = rc.getReplacementSubTree();
                                if (replacementTree != null) 
                                {
                                    convertSubTree(replacementTree);
                                    tree.replace(item, rc);
                                    tree.set(rc, replacementTree);
                                }
                            }
                        } 
                        else 
                        { // Not a ReplaceableController
                            convertSubTree(tree.getTree(item));
                            TestElement testElement = item.getTestElement();
                            tree.replace(item, testElement);
                        }
                     }
                    else
                    { // Not enabled
                        tree.remove(item);
                    }
                }
            }
        }

        private NetMeterEngine doRemoteInit(String hostName, HashTree testTree)
        {
            NetMeterEngine engine = null;
            try 
            {
                engine = new ClientJMeterEngine(hostName);
            } 
            catch (Exception e) 
            {
                log.fatalError("Failure connecting to remote host: "+hostName, e);
                System.err.println("Failure connecting to remote host: "+hostName+" "+e);
                return null;
            }
            engine.configure(testTree);
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
        private static class ListenToTest : TestStateListener 
        {
            private final AtomicInteger started = new AtomicInteger(0); // keep track of remote tests

            //NOT YET USED private JMeter _parent;

            private final List<JMeterEngine> engines;

            /**
             * @param unused JMeter unused for now
             * @param engines List<JMeterEngine>
             */
            public ListenToTest(JMeter unused, List<JMeterEngine> engines) {
                //_parent = unused;
                this.engines=engines;
            }

            @Override
            public void testEnded(String host) {
                long now=System.currentTimeMillis();
                log.info("Finished remote host: " + host + " ("+now+")");
                if (started.decrementAndGet() <= 0) {
                    Thread stopSoon = new Thread(this);
                    stopSoon.start();
                }
            }

            @Override
            public void testEnded() {
                long now = System.currentTimeMillis();
                println("Tidying up ...    @ "+new Date(now)+" ("+now+")");
                println("... end of run");
                checkForRemainingThreads();
            }

            @Override
            public void testStarted(String host) {
                started.incrementAndGet();
                long now=System.currentTimeMillis();
                log.info("Started remote host:  " + host + " ("+now+")");
            }

            @Override
            public void testStarted() {
                long now=System.currentTimeMillis();
                log.info(JMeterUtils.getResString("running_test")+" ("+now+")");//$NON-NLS-1$
            }

            /**
             * This is a hack to allow listeners a chance to close their files. Must
             * implement a queue for sample responses tied to the engine, and the
             * engine won't deliver testEnded signal till all sample responses have
             * been delivered. Should also improve performance of remote JMeter
             * testing.
             */
            @Override
            public void run() {
                long now = System.currentTimeMillis();
                println("Tidying up remote @ "+new Date(now)+" ("+now+")");
                if (engines!=null){ // it will be null unless remoteStop = true
                    println("Exitting remote servers");
                    for (JMeterEngine e : engines){
                        e.exit();
                    }
                }
                try {
                    Thread.sleep(5000); // Allow listeners to close files
                } catch (InterruptedException ignored) {
                }
                ClientJMeterEngine.tidyRMI(log);
                println("... end of run");
                checkForRemainingThreads();
            }

            /**
             * Runs daemon thread which waits a short while; 
             * if JVM does not exit, lists remaining non-daemon threads on stdout.
             */
            private void checkForRemainingThreads() {
                // This cannot be a JMeter class variable, because properties
                // are not initialised until later.
                final int REMAIN_THREAD_PAUSE = 
                        JMeterUtils.getPropDefault("jmeter.exit.check.pause", 2000); // $NON-NLS-1$ 
            
                if (REMAIN_THREAD_PAUSE > 0) {
                    Thread daemon = new Thread(){
                        @Override
                        public void run(){
                            try {
                                Thread.sleep(REMAIN_THREAD_PAUSE); // Allow enough time for JVM to exit
                            } catch (InterruptedException ignored) {
                            }
                            // This is a daemon thread, which should only reach here if there are other
                            // non-daemon threads still active
                            System.out.println("The JVM should have exitted but did not.");
                            System.out.println("The following non-daemon threads are still running (DestroyJavaVM is OK):");
                            JOrphanUtils.displayThreads(false);
                        }
    
                    };
                    daemon.setDaemon(true);
                    daemon.start();
                }
            }

        }

        private static void println(String str) 
        {
            System.out.println(str);
        }

        private static sealed String[][] DEFAULT_ICONS = 
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

        @Override
        public String[][] getIconMappings() {
            final String defaultIconProp = "org/apache/jmeter/images/icon.properties"; //$NON-NLS-1$
            String iconProp = JMeterUtils.getPropDefault("jmeter.icons", defaultIconProp);//$NON-NLS-1$
            Properties p = JMeterUtils.loadProperties(iconProp);
            if (p == null && !iconProp.equals(defaultIconProp)) {
                log.info(iconProp + " not found - using " + defaultIconProp);
                iconProp = defaultIconProp;
                p = JMeterUtils.loadProperties(iconProp);
            }
            if (p == null) {
                log.info(iconProp + " not found - using inbuilt icon set");
                return DEFAULT_ICONS;
            }
            log.info("Loaded icon properties from " + iconProp);
            String[][] iconlist = new String[p.size()][3];
            Enumeration<?> pe = p.keys();
            int i = 0;
            while (pe.hasMoreElements()) {
                String key = (String) pe.nextElement();
                String icons[] = JOrphanUtils.split(p.getProperty(key), " ");//$NON-NLS-1$
                iconlist[i][0] = key;
                iconlist[i][1] = icons[0];
                if (icons.length > 1) {
                    iconlist[i][2] = icons[1];
                }
                i++;
            }
            return iconlist;
        }

        @Override
        public String[][] getResourceBundles() {
            return new String[0][];
        }

        /**
         * Check if JMeter is running in non-GUI mode.
         *
         * @return true if JMeter is running in non-GUI mode.
         */
        public static Boolean isNonGUI(){
            return "true".equals(System.getProperty(JMeter.JMETER_NON_GUI)); //$NON-NLS-1$
        }

        private void logProperty(String prop){
            log.info(prop+"="+System.getProperty(prop));//$NON-NLS-1$
        }
        private void logProperty(String prop,String separator){
            log.info(prop+separator+System.getProperty(prop));//$NON-NLS-1$
        }

        private static void startUdpDdaemon(final List<JMeterEngine> engines) {
            int port = JMeterUtils.getPropDefault("jmeterengine.nongui.port", UDP_PORT_DEFAULT); // $NON-NLS-1$
            int maxPort = JMeterUtils.getPropDefault("jmeterengine.nongui.maxport", 4455); // $NON-NLS-1$
            if (port > 1000){
                final DatagramSocket socket = getSocket(port, maxPort);
                if (socket != null) {
                    Thread waiter = new Thread("UDP Listener"){
                        @Override
                        public void run() {
                            waitForSignals(engines, socket);
                        }
                    };
                    waiter.setDaemon(true);
                    waiter.start();
                } else {
                    System.out.println("Failed to create UDP port");
                }
            }
        }

        private static void waitForSignals(final List<JMeterEngine> engines, DatagramSocket socket) {
            byte[] buf = new byte[80];
            System.out.println("Waiting for possible shutdown message on port "+socket.getLocalPort());
            DatagramPacket request = new DatagramPacket(buf, buf.length);
            try {
                while(true) {
                    socket.receive(request);
                    InetAddress address = request.getAddress();
                    // Only accept commands from the local host
                    if (address.isLoopbackAddress()){
                        String command = new String(request.getData(), request.getOffset(), request.getLength(),"ASCII");
                        System.out.println("Command: "+command+" received from "+address);
                        log.info("Command: "+command+" received from "+address);
                        if (command.equals("StopTestNow")){
                            for(JMeterEngine engine : engines) {
                                engine.stopTest(true);
                            }
                        } else if (command.equals("Shutdown")) {
                            for(JMeterEngine engine : engines) {
                                engine.stopTest(false);
                            }
                        } else if (command.equals("HeapDump")) {
                            HeapDumper.dumpHeap();
                        } else {
                            System.out.println("Command: "+command+" not recognised ");
                        }
                    }
                }
            } catch (Exception e) {
                System.out.println(e);
            } finally {
                socket.close();
            }
        }

        private static DatagramSocket getSocket(int udpPort, int udpPortMax) {
            DatagramSocket socket = null;
            int i = udpPort;
            while (i<= udpPortMax) {
                try {
                    socket = new DatagramSocket(i);
                    break;
                } catch (SocketException e) {
                    i++;
                }            
            }

            return socket;
        }

    }
}
