using log4net;
using System;
using Valkyrie.Logging;
using System.IO;
using NetMeter.Util;
using System.Collections.Generic;
using Valkyrie.Collections;
using System.Xml;


namespace NetMeter.IOService
{
    class IOService
    {
        private static ILog log = LoggingManager.GetLoggerForClass();

        // Names of DataHolder entries for JTL processing
        public static String SAMPLE_EVENT_OBJECT = "SampleEvent"; // $NON-NLS-1$
        public static String RESULTCOLLECTOR_HELPER_OBJECT = "ResultCollectorHelper"; // $NON-NLS-1$

        // Names of DataHolder entries for JMX processing
        public static String TEST_CLASS_NAME = "TestClassName"; // $NON-NLS-1$

        //private static class XStreamWrapper : XStream 
        //{
        //    private XStreamWrapper(ReflectionProvider reflectionProvider) 
        //        : base(reflectionProvider)
        //    {
        //    }

        //    // Override wrapMapper in order to insert the Wrapper in the chain
        //    protected MapperWrapper wrapMapper(MapperWrapper next) 
        //    {
        //        // Provide our own aliasing using strings rather than classes
        //        return new MapperWrapper(next)
        //        {
        //        // Translate alias to classname and then delegate to wrapped class
        //        public Type T realClass<T>(String alias) 
        //        {
        //            String fullName = aliasToClass(alias);
        //            if (fullName != null) 
        //            {
        //                fullName = NameUpdater.getCurrentName(fullName);
        //            }
        //            return base.realClass(fullName == null ? alias : fullName);
        //        }
        //        // Translate to alias and then delegate to wrapped class
        //        @Override
        //        public String serializedClass(@SuppressWarnings("rawtypes") // superclass does not use types 
        //                Class type) {
        //            if (type == null) {
        //                return super.serializedClass(null); // was type, but that caused FindBugs warning
        //            }
        //            String alias = classToAlias(type.getName());
        //            return alias == null ? super.serializedClass(type) : alias ;
        //            }
        //        };
        //    }
        //}

        private static XStream JMXSAVER = new XStreamWrapper(new PureJavaReflectionProvider());
        private static XStream JTLSAVER = new XStreamWrapper(new PureJavaReflectionProvider());

        private static void Init()
        {
            JTLSAVER.setMode(XStream.NO_REFERENCES); // This is needed to stop XStream keeping copies of each class
            log.Info("Testplan (JMX) version: "+TESTPLAN_FORMAT+". Testlog (JTL) version: "+TESTLOG_FORMAT);
            initProps();
            //checkVersions();
        }

        // The XML header, with placeholder for encoding, since that is controlled by property
        private static String XML_HEADER = "<?xml version=\"1.0\" encoding=\"<ph>\"?>"; // $NON-NLS-1$

        // Default file name
        private static String SAVESERVICE_PROPERTIES_FILE = "/bin/saveservice.properties"; // $NON-NLS-1$

        // Property name used to define file name
        private static String SAVESERVICE_PROPERTIES = "saveservice_properties"; // $NON-NLS-1$

        // Define file format property names
        private static String FILE_FORMAT = "file_format"; // $NON-NLS-1$
        private static String FILE_FORMAT_TESTPLAN = "file_format.testplan"; // $NON-NLS-1$
        private static String FILE_FORMAT_TESTLOG = "file_format.testlog"; // $NON-NLS-1$

        // Define file format versions
        private static String VERSION_2_2 = "2.2";  // $NON-NLS-1$

        // Default to overall format, and then to version 2.2
        public static String TESTPLAN_FORMAT
            = NetMeterUtils.getPropDefault(FILE_FORMAT_TESTPLAN
            , NetMeterUtils.getPropDefault(FILE_FORMAT, VERSION_2_2));

        public static String TESTLOG_FORMAT
            = NetMeterUtils.getPropDefault(FILE_FORMAT_TESTLOG
            , NetMeterUtils.getPropDefault(FILE_FORMAT, VERSION_2_2));

        private static Boolean validateFormat(String format)
        {
            if ("2.2".Equals(format)) return true;
            if ("2.1".Equals(format)) return true;
            return false;
        }

        /** New XStream format - more compressed class names */
        public static Boolean IS_TESTPLAN_FORMAT_22 = VERSION_2_2.Equals(TESTPLAN_FORMAT);

        // Holds the mappings from the saveservice properties file
        // Key: alias Entry: full class name
        // There may be multiple aliases which map to the same class
        //private static Properties aliasToClass = new Properties();

        // Holds the reverse mappings
        // Key: full class name Entry: primary alias
        //private static Properties classToAlias = new Properties();

        // Version information for test plan header
        // This is written to JMX files by ScriptWrapperConverter
        // Also to JTL files by ResultCollector
        private static String VERSION = "1.2"; // $NON-NLS-1$

        // This is written to JMX files by ScriptWrapperConverter
        private static String propertiesVersion = "";// read from properties file; written to JMX files
    
        // Must match _version property value in saveservice.properties
        // used to ensure saveservice.properties and SaveService are updated simultaneously
        private static String PROPVERSION = "2.4";// Expected version $NON-NLS-1$

        // Internal information only
        private static String fileVersion = ""; // read from saveservice.properties file// $NON-NLS-1$
        // Must match Revision id value in saveservice.properties, 
        // used to ensure saveservice.properties and SaveService are updated simultaneously
        private static String FILEVERSION = "1427507"; // Expected value $NON-NLS-1$
        private static String fileEncoding = ""; // read from properties file// $NON-NLS-1$

        // Helper method to simplify alias creation from properties
        private static void makeAlias(String aliasList, String clazz)
        {
            String[] aliases = aliasList.Split(','); // Can have multiple aliases for same target classname
            String alias = aliases[0];
            foreach (String a in aliases)
            {
                Object old = aliasToClass.setProperty(a,clazz);
                if (old != null)
                {
                    log.Error("Duplicate class detected for "+alias+": "+clazz+" & "+old);                
                }
            }
            Object oldval=classToAlias.setProperty(clazz,alias);
            if (oldval != null) 
            {
                log.Error("Duplicate alias detected for "+clazz+": "+alias+" & "+oldval);
            }
        }

        public static Properties loadProperties() 
        {
            Properties nameMap = new Properties();
            FileInputStream fis = null;
            try 
            {
                fis = new FileInputStream(JMeterUtils.getJMeterHome()
                             + JMeterUtils.getPropDefault(SAVESERVICE_PROPERTIES, SAVESERVICE_PROPERTIES_FILE));
                nameMap.load(fis);
            } 
            finally
            {
                JOrphanUtils.closeQuietly(fis);
            }
            return nameMap;
        }

        private static void initProps() 
        {
            // Load the alias properties
            try
            {
                Properties nameMap = loadProperties();
                // now create the aliases
                foreach (Map.Entry<Object, Object> me in nameMap.entrySet())
                {
                    String key = (String) me.getKey();
                    String val = (String) me.getValue();
                    if (!key.startsWith("_")) 
                    { // $NON-NLS-1$
                        makeAlias(key, val);
                    }
                    else
                    {
                        // process special keys
                        if (key.equalsIgnoreCase("_version")) 
                        { // $NON-NLS-1$
                            propertiesVersion = val;
                            log.Info("Using SaveService properties version " + propertiesVersion);
                        } 
                        else if (key.equalsIgnoreCase("_file_version"))
                        { // $NON-NLS-1$
                                fileVersion = extractVersion(val);
                                log.Info("Using SaveService properties file version " + fileVersion);
                        } 
                        else if (key.equalsIgnoreCase("_file_encoding")) 
                        { // $NON-NLS-1$
                            fileEncoding = val;
                            log.Info("Using SaveService properties file encoding " + fileEncoding);
                        } 
                        else 
                        {
                            key = key.substring(1);// Remove the leading "_"
                            try 
                            {
                                String trimmedValue = val.trim();
                                if (trimmedValue.equals("collection") // $NON-NLS-1$
                                 || trimmedValue.equals("mapping")) 
                                { // $NON-NLS-1$
                                    registerConverter(key, JMXSAVER, true);
                                    registerConverter(key, JTLSAVER, true);
                                } 
                                else 
                                {
                                    registerConverter(key, JMXSAVER, false);
                                    registerConverter(key, JTLSAVER, false);
                                }
                            } catch (IllegalAccessException e1) {
                                log.warn("Can't register a converter: " + key, e1);
                            } catch (InstantiationException e1) {
                                log.warn("Can't register a converter: " + key, e1);
                            } catch (ClassNotFoundException e1) {
                                log.warn("Can't register a converter: " + key, e1);
                            } catch (IllegalArgumentException e1) {
                                log.warn("Can't register a converter: " + key, e1);
                            } catch (SecurityException e1) {
                                log.warn("Can't register a converter: " + key, e1);
                            } catch (InvocationTargetException e1) {
                                log.warn("Can't register a converter: " + key, e1);
                            } catch (NoSuchMethodException e1) {
                                log.warn("Can't register a converter: " + key, e1);
                            }
                        }
                    }
                }
            } catch (IOException e) 
            {
                log.Fatal("Bad saveservice properties file", e);
                throw new Exception("JMeter requires the saveservice properties file to continue");
            }
        }

        /**
         * Register converter.
         * @param key
         * @param jmxsaver
         * @param useMapper
         *
         * @throws InstantiationException
         * @throws IllegalAccessException
         * @throws InvocationTargetException
         * @throws NoSuchMethodException
         * @throws ClassNotFoundException
         */
        //private static void registerConverter(String key, XStream jmxsaver, Boolean useMapper)
        //{
        //    if (useMapper)
        //    {
        //        jmxsaver.registerConverter((Converter) Class.forName(key).getConstructor(
        //                new Class[] { Mapper.class }).newInstance(
        //                        new Object[] { jmxsaver.getMapper() }));
        //    } 
        //    else {
        //        jmxsaver.registerConverter((Converter) Class.forName(key).newInstance());
        //    }
        //}

        // For converters to use
        //public static String aliasToClass(String s)
        //{
        //    String r = aliasToClass.getProperty(s);
        //    return r == null ? s : r;
        //}

        //// For converters to use
        //public static String classToAlias(String s){
        //    String r = classToAlias.getProperty(s);
        //    return r == null ? s : r;
        //}

        // Called by Save function
        //public static void saveTree(HashTree tree, OutputStream out) throws IOException {
        //    // Get the OutputWriter to use
        //    OutputStreamWriter outputStreamWriter = getOutputStreamWriter(out);
        //    writeXmlHeader(outputStreamWriter);
        //    // Use deprecated method, to avoid duplicating code
        //    ScriptWrapper wrapper = new ScriptWrapper();
        //    wrapper.testPlan = tree;
        //    JMXSAVER.toXML(wrapper, outputStreamWriter);
        //    outputStreamWriter.write('\n');// Ensure terminated properly
        //    outputStreamWriter.close();
        //}

        // Used by Test code
        //public static void saveElement(Object el, OutputStream out) throws IOException {
        //    // Get the OutputWriter to use
        //    OutputStreamWriter outputStreamWriter = getOutputStreamWriter(out);
        //    writeXmlHeader(outputStreamWriter);
        //    // Use deprecated method, to avoid duplicating code
        //    JMXSAVER.toXML(el, outputStreamWriter);
        //    outputStreamWriter.close();
        //}

        // Used by Test code
        //public static Object loadElement(InputStream in) throws IOException {
        //    // Get the InputReader to use
        //    InputStreamReader inputStreamReader = getInputStreamReader(in);
        //    // Use deprecated method, to avoid duplicating code
        //    Object element = JMXSAVER.fromXML(inputStreamReader);
        //    inputStreamReader.close();
        //    return element;
        //}

        /**
         * Save a sampleResult to an XML output file using XStream.
         *
         * @param evt sampleResult wrapped in a sampleEvent
         * @param writer output stream which must be created using {@link #getFileEncoding(String)}
         */
        // Used by ResultCollector.sampleOccurred(SampleEvent event)
        //public synchronized static void saveSampleResult(SampleEvent evt, Writer writer) throws IOException {
        //    DataHolder dh = JTLSAVER.newDataHolder();
        //    dh.put(SAMPLE_EVENT_OBJECT, evt);
        //    // This is effectively the same as saver.toXML(Object, Writer) except we get to provide the DataHolder
        //    // Don't know why there is no method for this in the XStream class
        //    JTLSAVER.marshal(evt.getResult(), new XppDriver().createWriter(writer), dh);
        //    writer.write('\n');
        //}

        /**
         * @param elem test element
         * @param writer output stream which must be created using {@link #getFileEncoding(String)}
         */
        // Used by ResultCollector#recordStats()
        //public synchronized static void saveTestElement(TestElement elem, Writer writer) throws IOException {
        //    JMXSAVER.toXML(elem, writer); // TODO should this be JTLSAVER? Only seems to be called by MonitorHealthVisualzer
        //    writer.write('\n');
        //}

        private static Boolean versionsOK = true;

        // Extract version digits from String of the form #Revision: n.mm #
        // (where # is actually $ above)
        private static String REVPFX = "$Revision: ";
        private static String REVSFX = " $"; // $NON-NLS-1$

        private static String extractVersion(String rev) 
        {
            if (rev.Length > REVPFX.Length + REVSFX.Length) {
                return rev.Substring(REVPFX.Length, rev.Length - REVSFX.Length);
            }
            return rev;
        }


        // Routines for TestSaveService
        static Boolean checkPropertyVersion()
        {
            return IOService.PROPVERSION.Equals(IOService.propertiesVersion);
        }

        static Boolean checkFileVersion()
        {
            return IOService.FILEVERSION.Equals(IOService.fileVersion);
        }

        // Allow test code to check for spurious class references
        //static List<String> checkClasses()
        //{
        //    ClassLoader classLoader = SaveService.class.getClassLoader();
        //    List<String> missingClasses = new List<String>();

        //    foreach (Object clazz in classToAlias.keySet()) 
        //    {
        //        String name = (String) clazz;
        //        if (!NameUpdater.isMapped(name)) {// don't bother checking class is present if it is to be updated
        //            try {
        //                Class.forName(name, false, classLoader);
        //            } catch (ClassNotFoundException e) {
        //                    log.error("Unexpected entry in saveservice.properties; class does not exist and is not upgraded: "+name);              
        //                    missingClasses.add(name);
        //            }
        //        }
        //    }
        //    return missingClasses;
        //}

        //static Boolean checkVersions() {
        //    versionsOK = true;

        //    if (!PROPVERSION.equalsIgnoreCase(propertiesVersion)) 
        //    {
        //        log.warn("Bad _version - expected " + PROPVERSION + ", found " + propertiesVersion + ".");
        //    }

        //    if (versionsOK) {
        //        log.info("All converter versions present and correct");
        //    }
        //    return versionsOK;
        //}

        /**
         * Read results from JTL file.
         *
         * @param reader of the file
         * @param resultCollectorHelper helper class to enable TestResultWrapperConverter to deliver the samples
         * @throws Exception
         */
        //public static void loadTestResults(InputStream reader, ResultCollectorHelper resultCollectorHelper) throws Exception {
        //    // Get the InputReader to use
        //    InputStreamReader inputStreamReader = getInputStreamReader(reader);
        //    DataHolder dh = JTLSAVER.newDataHolder();
        //    dh.put(RESULTCOLLECTOR_HELPER_OBJECT, resultCollectorHelper); // Allow TestResultWrapper to feed back the samples
        //    // This is effectively the same as saver.fromXML(InputStream) except we get to provide the DataHolder
        //    // Don't know why there is no method for this in the XStream class
        //    JTLSAVER.unmarshal(new XppDriver().createReader(reader), null, dh);
        //    inputStreamReader.close();
        //}

        /**
         * Load a Test tree (JMX file)
         * @param reader on the JMX file
         * @return the loaded tree
         * @throws Exception if there is a problem reading the file or processing it
         */
        public static HashTree LoadTree(FileStream reader) 
        {
            XMLWrapper wrapper = null;
            try 
            {
                // Get the InputReader to use
                XmlReaderSettings setting = new XmlReaderSettings();
                setting.Async = true;
                using (XmlReader xReader = XmlReader.Create(reader,setting))
                {
                    while(xReader.Read())
                    {
                        
                    }
                }

                if (wrapper == null){
                    log.Error("Problem loading XML: see above.");
                    return null;
                }
                return wrapper.testPlan;
            } 
            catch (XmlException e) 
            {
                log.Warn("Problem loading XML, cannot determine class for element: " + e.Message);
                return null;
            }
        }

        private static InputStreamReader getInputStreamReader(InputStream inStream) {
            // Check if we have a encoding to use from properties
            Charset charset = getFileEncodingCharset();
            if(charset != null) {
                return new InputStreamReader(inStream, charset);
            }
            else {
                // We use the default character set encoding of the JRE
                return new InputStreamReader(inStream);
            }
        }

        private static OutputStreamWriter getOutputStreamWriter(OutputStream outStream) {
            // Check if we have a encoding to use from properties
            Charset charset = getFileEncodingCharset();
            if(charset != null) {
                return new OutputStreamWriter(outStream, charset);
            }
            else {
                // We use the default character set encoding of the JRE
                return new OutputStreamWriter(outStream);
            }
        }

        /**
         * Returns the file Encoding specified in saveservice.properties or the default
         * @param dflt value to return if file encoding was not provided
         *
         * @return file encoding or default
         */
        // Used by ResultCollector when creating output files
        public static String getFileEncoding(String dflt)
        {
            if(fileEncoding != null && fileEncoding.length() > 0) {
                return fileEncoding;
            }
            else {
                return dflt;
            }
        }

        private static Charset getFileEncodingCharset() {
            // Check if we have a encoding to use from properties
            if(fileEncoding != null && fileEncoding.length() > 0) {
                return Charset.forName(fileEncoding);
            }
            else {
                // We use the default character set encoding of the JRE
                return null;
            }
        }

        private static void writeXmlHeader(OutputStreamWriter writer) throws IOException {
            // Write XML header if we have the charset to use for encoding
            Charset charset = getFileEncodingCharset();
            if(charset != null) {
                // We do not use getEncoding method of Writer, since that returns
                // the historical name
                String header = XML_HEADER.replaceAll("<ph>", charset.name());
                writer.write(header);
                writer.write('\n');
            }
        }

    //  Normal output
    //  ---- Debugging information ----
    //  required-type       : org.apache.jorphan.collections.ListedHashTree
    //  cause-message       : WebServiceSampler : WebServiceSampler
    //  class               : org.apache.jmeter.save.ScriptWrapper
    //  message             : WebServiceSampler : WebServiceSampler
    //  line number         : 929
    //  path                : /jmeterTestPlan/hashTree/hashTree/hashTree[4]/hashTree[5]/WebServiceSampler
    //  cause-exception     : com.thoughtworks.xstream.alias.CannotResolveClassException
    //  -------------------------------

        /**
         * Simplify getMessage() output from XStream ConversionException
         * @param ce - ConversionException to analyse
         * @return string with details of error
         */
        //public static String CEtoString(ConversionException ce){
        //    String msg =
        //        "XStream ConversionException at line: " + ce.get("line number")
        //        + "\n" + ce.get("message")
        //        + "\nPerhaps a missing jar? See log file.";
        //    return msg;
        //}

        //public static String getPropertiesVersion() {
        //    return propertiesVersion;
        //}

        //public static String getVERSION() {
        //    return VERSION;
        //}
    }
}
