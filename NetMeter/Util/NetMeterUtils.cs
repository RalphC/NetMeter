using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMeter.Util
{
    public class NetMeterUtils
    {
        // Note: cannot use a static variable here, because that would be processed before the JMeter properties
        // have been defined (Bug 52783)
        private static class LazyPatternCacheHolder
        {
            public static sealed PatternCacheLRU INSTANCE = new PatternCacheLRU(
                    getPropDefault("oro.patterncache.size",1000), // $NON-NLS-1$
                    new Perl5Compiler());
        }

        private static sealed String EXPERT_MODE_PROPERTY = "jmeter.expertMode"; // $NON-NLS-1$

        private static volatile Properties appProperties;

        private static volatile ResourceBundle resources;

        // What host am I running on?

        //@GuardedBy("this")
        private static String localHostIP = null;
        //@GuardedBy("this")
        private static String localHostName = null;
        //@GuardedBy("this")
        private static String localHostFullName = null;

        private static volatile Boolean ignoreResorces = false; // Special flag for use in debugging resources

        //private static sealed ThreadLocal<Perl5Matcher> localMatcher = new ThreadLocal<Perl5Matcher>() 
        //{
        //    protected override Perl5Matcher initialValue() 
        //    {
        //        return new Perl5Matcher();
        //    }
        //};

        // Provide Random numbers to whomever wants one
        private static sealed Random rand = new Random();

        /**
         * This method is used by the init method to load the property file that may
         * even reside in the user space, or in the classpath under
         * org.apache.jmeter.jmeter.properties.
         *
         * The method also initialises logging and sets up the default Locale
         *
         * TODO - perhaps remove?
         * [still used
         *
         * @param file
         *            the file to load
         * @return the Properties from the file
         * @see #getJMeterProperties()
         * @see #loadJMeterProperties(String)
         * @see #initLogging()
         * @see #initLocale()
         */
        public static Properties getProperties(String file)
        {
            loadJMeterProperties(file);
            initLogging();
            return appProperties;
        }

        /**
         * Initialise JMeter logging
         */
        public static void initLogging() 
        {
            LoggingManager.initializeLogging(appProperties);
        }


        /**
         * Load the JMeter properties file; if not found, then
         * default to "org/apache/jmeter/jmeter.properties" from the classpath
         *
         * c.f. loadProperties
         *
         */
        public static void loadJMeterProperties(String file) 
        {
            Properties p = new Properties(System.getProperties());
            InputStream ins = null;
            try
            {
                File f = new File(file);
                ins = new FileInputStream(f);
                p.load(ins);
            } 
            catch (IOException e) 
            {
                try 
                {
                    ins = ClassLoader.getSystemResourceAsStream("org/apache/jmeter/jmeter.properties"); // $NON-NLS-1$
                    if (ins == null) 
                    {
                        //throw new RuntimeException("Could not read JMeter properties file");
                    }
                    p.load(ins);
                }
                catch (IOException ex)
                {
                    // JMeter.fail("Could not read internal resource. " +
                    // "Archive is broken.");
                }
            }
            finally 
            {
                JOrphanUtils.closeQuietly(ins);
            }
            appProperties = p;
        }

        /**
         * This method loads a property file that may reside in the user space, or
         * in the classpath
         *
         * @param file
         *            the file to load
         * @return the Properties from the file, may be null (e.g. file not found)
         */
        public static Properties loadProperties(String file) 
        {
            return loadProperties(file, null);
        }

        /**
         * This method loads a property file that may reside in the user space, or
         * in the classpath
         *
         * @param file
         *            the file to load
         * @param defaultProps a set of default properties
         * @return the Properties from the file; if it could not be processed, the defaultProps are returned.
         */
        public static Properties loadProperties(String file, Properties defaultProps) 
        {
            Properties p = new Properties(defaultProps);
            InputStream ins = null;
            try 
            {
                File f = new File(file);
                ins = new FileInputStream(f);
                p.load(ins);
            } 
            catch (IOException e) 
            {
                //try 
                //{
                //    sealed URL resource = NetMeterUtils.class.getClassLoader().getResource(file);
                //    if (resource == null) 
                //    {
                //        //log.warn("Cannot find " + file);
                //        return defaultProps;
                //    }
                //    ins = resource.openStream();
                //    if (ins == null) 
                //    {
                //        log.warn("Cannot open " + file);
                //        return defaultProps;
                //    }
                //    p.load(ins);
                //} 
                //catch (IOException ex) 
                //{
                //    log.warn("Error reading " + file + " " + ex.toString());
                //    return defaultProps;
                //}
            } 
            finally 
            {
                JOrphanUtils.closeQuietly(ins);
            }
            return p;
        }

        public override void initializeProperties(String file) 
        {
            System.Console.WriteLine("Initializing Properties: " + file);
            getProperties(file);
        }

        /**
         * Convenience method for
         * {@link ClassFinder#findClassesThatExtend(String[], Class[], boolean)}
         * with the option to include inner classes in the search set to false
         * and the path list is derived from JMeterUtils.getSearchPaths().
         *
         * @param superClass - single class to search for
         * @return List of Strings containing discovered class names.
         */
        public static List<String> findClassesThatExtend(Class<Object> superClass)
        {
            return ClassFinder.findClassesThatExtend(getSearchPaths(), new Class[]{superClass}, false);
        }

        /**
         * Generate a list of paths to search.
         * The output array always starts with
         * JMETER_HOME/lib/ext
         * and is followed by any paths obtained from the "search_paths" JMeter property.
         * 
         * @return array of path strings
         */
        public static String[] getSearchPaths() 
        {
            String p = NetMeterUtils.getPropDefault("search_paths", null); // $NON-NLS-1$
            String[] result = new String[1];

            if (p != null) {
                String[] paths = p.Split(';'); // $NON-NLS-1$
                result = new String[paths.Length + 1];
                System.arraycopy(paths, 0, result, 1, paths.Length);
            }
            result[0] = getJMeterHome() + "\lib\ext"; // $NON-NLS-1$
            return result;
        }

        /**
         * Provide random numbers
         *
         * @param r -
         *            the upper bound (exclusive)
         */
        public static int getRandomInt(int r) 
        {
            return rand.Next(r);
        }

        /**
         * Gets the resource string for this key.
         *
         * If the resource is not found, a warning is logged
         *
         * @param key
         *            the key in the resource file
         * @return the resource string if the key is found; otherwise, return
         *         "[res_key="+key+"]"
         */
        public static String getResString(String key) 
        {
            return getResStringDefault(key, RES_KEY_PFX + key + "]"); // $NON-NLS-1$
        }
    
        /**
         * Gets the resource string for this key in Locale.
         *
         * If the resource is not found, a warning is logged
         *
         * @param key
         *            the key in the resource file
         * @param forcedLocale Force a particular locale
         * @return the resource string if the key is found; otherwise, return
         *         "[res_key="+key+"]"
         * @since 2.7
         */
        public static String getResString(String key, Locale forcedLocale) 
        {
            return getResStringDefault(key, RES_KEY_PFX + key + "]", // $NON-NLS-1$
                    forcedLocale); 
        }

        public static sealed String RES_KEY_PFX = "[res_key="; // $NON-NLS-1$

        /**
         * Gets the resource string for this key.
         *
         * If the resource is not found, a warning is logged
         *
         * @param key
         *            the key in the resource file
         * @param defaultValue -
         *            the default value
         *
         * @return the resource string if the key is found; otherwise, return the
         *         default
         * @deprecated Only intended for use in development; use
         *             getResString(String) normally
         */
        public static String getResString(String key, String defaultValue) 
        {
            return getResStringDefault(key, defaultValue);
        }

        /*
         * Helper method to do the actual work of fetching resources; allows
         * getResString(S,S) to be deprecated without affecting getResString(S);
         */
        private static String getResStringDefault(String key, String defaultValue) 
        {
            return getResStringDefault(key, defaultValue, null);
        }
        /*
         * Helper method to do the actual work of fetching resources; allows
         * getResString(S,S) to be deprecated without affecting getResString(S);
         */
        private static String getResStringDefault(String key, String defaultValue, Locale forcedLocale) 
        {
            if (key == null) 
            {
                return null;
            }
            // Resource keys cannot contain spaces, and are forced to lower case
            String resKey = key.Replace(' ', '_'); // $NON-NLS-1$ // $NON-NLS-2$
            resKey = resKey.ToLower();
            String resString = null;
            try {
                ResourceBundle bundle = resources;
                if(forcedLocale != null) {
                    bundle = ResourceBundle.getBundle("org.apache.jmeter.resources.messages", forcedLocale); // $NON-NLS-1$
                }
                if (bundle.containsKey(resKey)) {
                    resString = bundle.getString(resKey);
                } else {
                    //log.warn("ERROR! Resource string not found: [" + resKey + "]");
                    resString = defaultValue;                
                }
                if (ignoreResorces ){ // Special mode for debugging resource handling
                    return "["+key+"]";
                }
            } 
            catch (MissingResourceException mre)
            {
                if (ignoreResorces ){ // Special mode for debugging resource handling
                    return "[?"+key+"?]";
                }
                //log.warn("ERROR! Resource string not found: [" + resKey + "]", mre);
                resString = defaultValue;
            }
            return resString;
        }

        /**
         * To get I18N label from properties file
         * 
         * @param key
         *            in messages.properties
         * @return I18N label without (if exists) last colon ':' and spaces
         */
        public static String getParsedLabel(String key) 
        {
            String value = NetMeterUtils.getResString(key);
            return value.replaceFirst("(?m)\\s*?:\\s*$", ""); // $NON-NLS-1$ $NON-NLS-2$
        }
   
        /**
         * This gets the currently defined appProperties. It can only be called
         * after the {@link #getProperties(String)} or {@link #loadJMeterProperties(String)} 
         * method has been called.
         *
         * @return The JMeterProperties value, 
         *         may be null if {@link #loadJMeterProperties(String)} has not been called
         * @see #getProperties(String)
         * @see #loadJMeterProperties(String)
         */
        public static Properties getJMeterProperties() 
        {
            return appProperties;
        }

        //public static String getResourceFileAsText(String name) {
        //    BufferedReader fileReader = null;
        //    try {
        //        String lineEnd = System.getProperty("line.separator"); // $NON-NLS-1$
        //        InputStream is = NetMeterUtils.getClassLoader().getResourceAsStream(name);
        //        if(is != null) {
        //            fileReader = new BufferedReader(new InputStreamReader(is));
        //            StringBuilder text = new StringBuilder();
        //            String line = "NOTNULL"; // $NON-NLS-1$
        //            while (line != null) {
        //                line = fileReader.readLine();
        //                if (line != null) {
        //                    text.append(line);
        //                    text.append(lineEnd);
        //                }
        //            }
        //            // Done by finally block: fileReader.close();
        //            return text.toString();
        //        } else {
        //            return ""; // $NON-NLS-1$                
        //        }
        //    } catch (IOException e) {
        //        return ""; // $NON-NLS-1$
        //    } finally 
        //    {
        //        IOUtils.closeQuietly(fileReader);
        //    }
        //}

        /**
         * Creates the vector of Timers plugins.
         *
         * @param properties
         *            Description of Parameter
         * @return The Timers value
         */
        public static Vector<Object> getTimers(Properties properties) {
            return instantiate(getVector(properties, "timer."), // $NON-NLS-1$
                    "org.apache.jmeter.timers.Timer"); // $NON-NLS-1$
        }

        /**
         * Creates the vector of visualizer plugins.
         *
         * @param properties
         *            Description of Parameter
         * @return The Visualizers value
         */
        public static Vector<Object> getVisualizers(Properties properties) {
            return instantiate(getVector(properties, "visualizer."), // $NON-NLS-1$
                    "org.apache.jmeter.visualizers.Visualizer"); // $NON-NLS-1$
        }

        /**
         * Creates a vector of SampleController plugins.
         *
         * @param properties
         *            The properties with information about the samplers
         * @return The Controllers value
         */
        // TODO - does not appear to be called directly
        public static Vector<Object> getControllers(Properties properties) {
            String name = "controller."; // $NON-NLS-1$
            Vector<Object> v = new Vector<Object>();
            Enumeration<?> names = properties.keys();
            while (names.hasMoreElements()) {
                String prop = (String) names.nextElement();
                if (prop.startsWith(name)) {
                    Object o = instantiate(properties.getProperty(prop),
                            "org.apache.jmeter.control.SamplerController"); // $NON-NLS-1$
                    v.addElement(o);
                }
            }
            return v;
        }

        /**
         * Create a string of class names for a particular SamplerController
         *
         * @param properties
         *            The properties with info about the samples.
         * @param name
         *            The name of the sampler controller.
         * @return The TestSamples value
         */
        public static String[] getTestSamples(Properties properties, String name) {
            Vector<String> vector = getVector(properties, name + ".testsample"); // $NON-NLS-1$
            return vector.toArray(new String[vector.size()]);
        }

        /**
         * Create an instance of an org.xml.sax.Parser based on the default props.
         *
         * @return The XMLParser value
         */
        // TODO only called by UserParameterXMLParser.getXMLParameters which is a deprecated class
        //public static XMLReader getXMLParser() 
        //{
        //    XMLReader reader = null;
        //    final String parserName = getPropDefault("xml.parser", // $NON-NLS-1$
        //            "org.apache.xerces.parsers.SAXParser");  // $NON-NLS-1$
        //    try {
        //        reader = (XMLReader) instantiate(parserName,
        //                "org.xml.sax.XMLReader"); // $NON-NLS-1$
        //        // reader = xmlFactory.newSAXParser().getXMLReader();
        //    } catch (Exception e) {
        //        reader = (XMLReader) instantiate(parserName, // $NON-NLS-1$
        //                "org.xml.sax.XMLReader"); // $NON-NLS-1$
        //    }
        //    return reader;
        //}

        /**
         * Creates the vector of alias strings.
         *
         * @param properties
         * @return The Alias value
         */
        public static Hashtable<String, String> getAlias(Properties properties) {
            return getHashtable(properties, "alias."); // $NON-NLS-1$
        }

        /**
         * Creates a vector of strings for all the properties that start with a
         * common prefix.
         *
         * @param properties
         *            Description of Parameter
         * @param name
         *            Description of Parameter
         * @return The Vector value
         */
        //public static Vector<String> getVector(Properties properties, String name) {
        //    Vector<String> v = new Vector<String>();
        //    Enumeration<?> names = properties.keys();
        //    while (names.hasMoreElements()) {
        //        String prop = (String) names.nextElement();
        //        if (prop.startsWith(name)) {
        //            v.addElement(properties.getProperty(prop));
        //        }
        //    }
        //    return v;
        //}

        /**
         * Creates a table of strings for all the properties that start with a
         * common prefix.
         *
         * @param properties input to search
         * @param prefix to match against properties
         * @return a Hashtable where the keys are the original keys with the prefix removed
         */
        //public static Hashtable<String, String> getHashtable(Properties properties, String prefix) {
        //    Hashtable<String, String> t = new Hashtable<String, String>();
        //    Enumeration<?> names = properties.keys();
        //    final int length = prefix.length();
        //    while (names.hasMoreElements()) {
        //        String prop = (String) names.nextElement();
        //        if (prop.startsWith(prefix)) {
        //            t.put(prop.substring(length), properties.getProperty(prop));
        //        }
        //    }
        //    return t;
        //}

        /**
         * Get a int value with default if not present.
         *
         * @param propName
         *            the name of the property.
         * @param defaultVal
         *            the default value.
         * @return The PropDefault value
         */
        public static int getPropDefault(String propName, int defaultVal) {
            int ans = defaultVal;
            try 
            {
                ans = appProperties.getProperty(propName, defaultVal.ToString()).ToInt32();
            } 
            catch (Exception e) 
            {
            }
            return ans;
        }

        /**
         * Get a boolean value with default if not present.
         *
         * @param propName
         *            the name of the property.
         * @param defaultVal
         *            the default value.
         * @return The PropDefault value
         */
        public static Boolean getPropDefault(String propName, Boolean defaultVal) 
        {
            Boolean ans = defaultVal;
            try 
            {
                String strVal = appProperties.getProperty(propName, defaultVal.ToString()).trim();
                if (strVal.Equals("True") || strVal.Equals("t")) 
                { // $NON-NLS-1$  // $NON-NLS-2$
                    ans = true;
                } 
                else 
                {
                    if (strVal.Equals("False") || strVal.Equals("f")) 
                    { // $NON-NLS-1$  // $NON-NLS-2$
                        ans = false;
                    } 
                    else 
                    {
                        ans = (strVal.Equals(true.ToString()));
                    }
                }

            } 
            catch (Exception e) 
            {
            }
            return ans;
        }

        /**
         * Get a long value with default if not present.
         *
         * @param propName
         *            the name of the property.
         * @param defaultVal
         *            the default value.
         * @return The PropDefault value
         */
        public static long getPropDefault(String propName, Int64 defaultVal) 
        {
            Int64 ans = defaultVal;
            try 
            {
                ans = appProperties.getProperty(propName, defaultVal.ToString()).ToInt64();
            } 
            catch (Exception e) 
            {
            }
            return ans;
        }

        /**
         * Get a String value with default if not present.
         *
         * @param propName
         *            the name of the property.
         * @param defaultVal
         *            the default value.
         * @return The PropDefault value
         */
        public static String getPropDefault(String propName, String defaultVal) 
        {
            String ans = defaultVal;
            try
            {
                String value = appProperties.getProperty(propName, defaultVal);
                if(value != null)
                {
                    ans = value.Trim();
                }
            } 
            catch (Exception e) 
            {
            }
            return ans;
        }
    
        /**
         * Get the value of a JMeter property.
         *
         * @param propName
         *            the name of the property.
         * @return the value of the JMeter property, or null if not defined
         */
        public static String getProperty(String propName) 
        {
            String ans = null;
            try {
                ans = appProperties.getProperty(propName);
            } catch (Exception e) {
                ans = null;
            }
            return ans;
        }

        /**
         * Set a String value
         *
         * @param propName
         *            the name of the property.
         * @param propValue
         *            the value of the property
         * @return the previous value of the property
         */
        public static Object setProperty(String propName, String propValue) 
        {
            return appProperties.setProperty(propName, propValue);
        }

        /**
         * Instatiate an object and guarantee its class.
         *
         * @param className
         *            The name of the class to instantiate.
         * @param impls
         *            The name of the class it must be an instance of
         * @return an instance of the class, or null if instantiation failed or the class did not implement/extend as required 
         */
        // TODO probably not needed
        //public static Object instantiate(String className, String impls)
        //{
        //    if (className != null) 
        //    {
        //        className = className.Trim();
        //    }

        //    if (impls != null)
        //    {
        //        impls = impls.Trim();
        //    }

        //    try 
        //    {
        //        Type<Object> c = Class.forName(impls);
        //        try {
        //            Class<?> o = Class.forName(className);
        //            Object res = o.newInstance();
        //            if (c.isInstance(res)) {
        //                return res;
        //            }
        //            throw new IllegalArgumentException(className + " is not an instance of " + impls);
        //        } catch (ClassNotFoundException e) {
        //            log.error("Error loading class " + className + ": class is not found");
        //        } catch (IllegalAccessException e) {
        //            log.error("Error loading class " + className + ": does not have access");
        //        } catch (InstantiationException e) {
        //            log.error("Error loading class " + className + ": could not instantiate");
        //        } catch (NoClassDefFoundError e) {
        //            log.error("Error loading class " + className + ": couldn't find class " + e.getMessage());
        //        }
        //    } catch (ClassNotFoundException e) {
        //        log.error("Error loading class " + impls + ": was not found.");
        //    }
        //    return null;
        //}

        /**
         * Instantiate a vector of classes
         *
         * @param v
         *            Description of Parameter
         * @param className
         *            Description of Parameter
         * @return Description of the Returned Value
         */
        //public static Vector<Object> instantiate(Vector<String> v, String className) {
        //    Vector<Object> i = new Vector<Object>();
        //    try {
        //        Class<?> c = Class.forName(className);
        //        Enumeration<String> elements = v.elements();
        //        while (elements.hasMoreElements()) {
        //            String name = elements.nextElement();
        //            try {
        //                Object o = Class.forName(name).newInstance();
        //                if (c.isInstance(o)) {
        //                    i.addElement(o);
        //                }
        //            } catch (ClassNotFoundException e) {
        //                log.error("Error loading class " + name + ": class is not found");
        //            } catch (IllegalAccessException e) {
        //                log.error("Error loading class " + name + ": does not have access");
        //            } catch (InstantiationException e) {
        //                log.error("Error loading class " + name + ": could not instantiate");
        //            } catch (NoClassDefFoundError e) {
        //                log.error("Error loading class " + name + ": couldn't find class " + e.getMessage());
        //            }
        //        }
        //    } catch (ClassNotFoundException e) {
        //        log.error("Error loading class " + className + ": class is not found");
        //    }
        //    return i;
        //}


        /**
         * Finds a string in an array of strings and returns the
         *
         * @param array
         *            Array of strings.
         * @param value
         *            String to compare to array values.
         * @return Index of value in array, or -1 if not in array.
         */
        //TODO - move to JOrphanUtils?
        public static int findInArray(String[] array, String value) 
        {
            int count = -1;
            int index = -1;
            if (array != null && value != null) 
            {
                while (++count < array.Length) 
                {
                    if (array[count] != null && array[count].Equals(value)) 
                    {
                        index = count;
                        break;
                    }
                }
            }
            return index;
        }

        /**
         * Takes an array of strings and a tokenizer character, and returns a string
         * of all the strings concatenated with the tokenizer string in between each
         * one.
         *
         * @param splittee
         *            Array of Objects to be concatenated.
         * @param splitChar
         *            Object to unsplit the strings with.
         * @return Array of all the tokens.
         */
        //TODO - move to JOrphanUtils?
        public static String unsplit(Object[] splittee, Object splitChar) 
        {
            StringBuilder retVal = new StringBuilder();
            int count = -1;
            while (++count < splittee.Length) 
            {
                if (splittee[count] != null) 
                {
                    retVal.Append(splittee[count]);
                }
                if (count + 1 < splittee.Length && splittee[count + 1] != null) 
                {
                    retVal.Append(splitChar);
                }
            }
            return retVal.ToString();
        }

        // End Method

        /**
         * Takes an array of strings and a tokenizer character, and returns a string
         * of all the strings concatenated with the tokenizer string in between each
         * one.
         *
         * @param splittee
         *            Array of Objects to be concatenated.
         * @param splitChar
         *            Object to unsplit the strings with.
         * @param def
         *            Default value to replace null values in array.
         * @return Array of all the tokens.
         */
        //TODO - move to JOrphanUtils?
        public static String unsplit(Object[] splittee, Object splitChar, String def) 
        {
            StringBuilder retVal = new StringBuilder();
            int count = -1;
            while (++count < splittee.Length) 
            {
                if (splittee[count] != null) 
                {
                    retVal.Append(splittee[count]);
                } 
                else
                {
                    retVal.Append(def);
                }
                if (count + 1 < splittee.Length) 
                {
                    retVal.Append(splitChar);
                }
            }
            return retVal.ToString();
        }

        /**
         * Get the JMeter home directory - does not include the trailing separator.
         *
         * @return the home directory
         */
        public static String getJMeterHome() 
        {
            return jmDir;
        }

        /**
         * Get the JMeter bin directory - does not include the trailing separator.
         *
         * @return the bin directory
         */
        public static String getJMeterBinDir() 
        {
            return jmBin;
        }

        public static void setJMeterHome(String home) 
        {
            jmDir = home;
            jmBin = jmDir + "\bin"; // $NON-NLS-1$
        }

        // TODO needs to be synch? Probably not changed after threads have started
        private static String jmDir; // JMeter Home directory (excludes trailing separator)
        private static String jmBin; // JMeter bin directory (excludes trailing separator)


        ///**
        // * Gets the JMeter Version.
        // *
        // * @return the JMeter version string
        // */
        //public static String getJMeterVersion() {
        //    return JMeterVersion.getVERSION();
        //}

        ///**
        // * Gets the JMeter copyright.
        // *
        // * @return the JMeter copyright string
        // */
        //public static String getJMeterCopyright() {
        //    return JMeterVersion.getCopyRight();
        //}

        /**
         * Determine whether we are in 'expert' mode. Certain features may be hidden
         * from user's view unless in expert mode.
         *
         * @return true iif we're in expert mode
         */
        public static Boolean isExpertMode()
        {
            return NetMeterUtils.getPropDefault(EXPERT_MODE_PROPERTY, false);
        }

        ///**
        // * Find a file in the current directory or in the JMeter bin directory.
        // *
        // * @param fileName
        // * @return File object
        // */
        //public static File findFile(String fileName)
        //{
        //    File f =new File(fileName);
        //    if (!f.exists()){
        //        f=new File(getJMeterBinDir(),fileName);
        //    }
        //    return f;
        //}

        /**
         * Returns the cached result from calling
         * InetAddress.getLocalHost().getHostAddress()
         *
         * @return String representation of local IP address
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static String getLocalHostIP()
        {
            if (localHostIP == null) {
                getLocalHostDetails();
            }
            return localHostIP;
        }

        /**
         * Returns the cached result from calling
         * InetAddress.getLocalHost().getHostName()
         *
         * @return local host name
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static String getLocalHostName()
        {
            if (localHostName == null) {
                getLocalHostDetails();
            }
            return localHostName;
        }

        /**
         * Returns the cached result from calling
         * InetAddress.getLocalHost().getCanonicalHostName()
         *
         * @return local host name in canonical form
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static String getLocalHostFullName()
        {
            if (localHostFullName == null) {
                getLocalHostDetails();
            }
            return localHostFullName;
        }

        //private static void getLocalHostDetails()
        //{
        //    InetAddress localHost=null;
        //    try {
        //        localHost = InetAddress.getLocalHost();
        //    } catch (UnknownHostException e1) {
        //        log.error("Unable to get local host IP address.");
        //        return; // TODO - perhaps this should be a fatal error?
        //    }
        //    localHostIP=localHost.getHostAddress();
        //    localHostName=localHost.getHostName();
        //    localHostFullName=localHost.getCanonicalHostName();
        //}
    
        /**
         * Split line into name/value pairs and remove colon ':'
         * 
         * @param headers
         *            multi-line string headers
         * @return a map name/value for each header
         */
        //public static LinkedHashMap<String, String> parseHeaders(String headers) 
        //{
        //    LinkedHashMap<String, String> linkedHeaders = new LinkedHashMap<String, String>();
        //    String[] list = headers.split("\n"); // $NON-NLS-1$
        //    for (String header : list) {
        //        int colon = header.indexOf(':'); // $NON-NLS-1$
        //        if (colon <= 0) {
        //            linkedHeaders.put(header, ""); // Empty value // $NON-NLS-1$
        //        } else {
        //            linkedHeaders.put(header.substring(0, colon).trim(), header
        //                    .substring(colon + 1).trim());
        //        }
        //    }
        //    return linkedHeaders;
        //}

    
        /**
         * Help GC by triggering GC and finalization
         */
        public static sealed void helpGC() 
        {
            System.GC.Collect();
        }
    }
}
