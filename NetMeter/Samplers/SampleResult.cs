using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using NetMeter.Util;
using NetMeter.Assertions;
using Valkyrie.Logging;
using log4net;
using System.Runtime.CompilerServices;
using System.Net;

namespace NetMeter.Samplers
{
    public class SampleResult : ISerializable
    {
        static sealed ILog log = LoggingManager.getLoggerForClass();
        /**
         * The default encoding to be used if not overridden.
         * The value is ISO-8859-1.
         */
        public static sealed String DEFAULT_HTTP_ENCODING = "ISO-8859-1";  // $NON-NLS-1$

        /**
         * The default encoding to be used to decode the responseData byte array.
         * The value is defined by the property "sampleresult.default.encoding"
         * with a default of DEFAULT_HTTP_ENCODING if that is not defined.
         */
        static sealed String DEFAULT_ENCODING = NetMeterUtils.getPropDefault("sampleresult.default.encoding", DEFAULT_HTTP_ENCODING);

        /* The default used by {@link #setResponseData(String, String)} */
        private static sealed String DEFAULT_CHARSET = "UTF-8";

        /**
         * Data type value indicating that the response data is text.
         *
         * @see #getDataType
         * @see #setDataType(java.lang.String)
         */
        public static sealed String TEXT = "text"; // $NON-NLS-1$

        /**
         * Data type value indicating that the response data is binary.
         *
         * @see #getDataType
         * @see #setDataType(java.lang.String)
         */
        public static sealed String BINARY = "bin"; // $NON-NLS-1$

        /* empty arrays which can be returned instead of null */
        public static sealed byte[] EMPTY_BA = new byte[0];

        private static sealed SampleResult[] EMPTY_SR = new SampleResult[0];

        private static sealed AssertionResult[] EMPTY_AR = new AssertionResult[0];
    
        private static sealed Boolean GETBYTES_BODY_REALSIZE = NetMeterUtils.getPropDefault("sampleresult.getbytes.body_real_size", true); // $NON-NLS-1$

        private static sealed Boolean GETBYTES_HEADERS_SIZE = NetMeterUtils.getPropDefault("sampleresult.getbytes.headers_size", true); // $NON-NLS-1$
    
        private static sealed Boolean GETBYTES_NETWORK_SIZE = GETBYTES_HEADERS_SIZE && GETBYTES_BODY_REALSIZE ? true : false;

        private SampleSaveConfiguration saveConfig;

        private SampleResult parent = null;

        /**
         * @param propertiesToSave
         *            The propertiesToSave to set.
         */
        public void setSaveConfig(SampleSaveConfiguration propertiesToSave)
        {
            this.saveConfig = propertiesToSave;
        }

        public SampleSaveConfiguration getSaveConfig() 
        {
            return saveConfig;
        }

        private byte[] responseData = EMPTY_BA;

        private String responseCode = "";// Never return null

        private String label = "";// Never return null

        /** Filename used by ResultSaver */
        private String resultFileName = "";

        /** The data used by the sampler */
        private String samplerData;

        private String threadName = ""; // Never return null

        private String responseMessage = "";

        private String responseHeaders = ""; // Never return null

        private String contentType = ""; // e.g. text/html; charset=utf-8

        private String requestHeaders = "";

        // TODO timeStamp == 0 means either not yet initialised or no stamp available (e.g. when loading a results file)
        /** the time stamp - can be start or end */
        private long timeStamp = 0;

        private long startTime = 0;

        private long endTime = 0;

        private long idleTime = 0;// Allow for non-sample time

        /** Start of pause (if any) */
        private long pauseTime = 0;

        private List<AssertionResult> assertionResults;

        private List<SampleResult> subResults;

        private String dataType=""; // Don't return null if not set

        private Boolean success;

        //@GuardedBy("this"")
        /** files that this sample has been saved in */
        /** In Non GUI mode and when best config is used, size never exceeds 1, 
         * but as a compromise set it to 3 
         */
        private sealed HashSet<String> files = new HashSet<String>();

        private String dataEncoding;// (is this really the character set?) e.g.
                                    // ISO-8895-1, UTF-8

        /** elapsed time */
        private long time = 0;

        /** time to first response */
        private long latency = 0;
    
        /** Should thread start next iteration ? */
        private Boolean startNextThreadLoop = false;

        /** Should thread terminate? */
        private Boolean stopThread = false;

        /** Should test terminate? */
        private Boolean stopTest = false;

        /** Should test terminate abruptly? */
        private Boolean stopTestNow = false;

        /** Is the sampler acting as a monitor? */
        private Boolean isMon = false;

        private Int32 sampleCount = 1;

        private Int32 bytes = 0; // Allows override of sample size in case sampler does not want to store all the data
    
        private Int32 headersSize = 0;
    
        private Int32 bodySize = 0;

        /** Currently active threads in this thread group */
        private volatile Int32 groupThreads = 0;

        /** Currently active threads in all thread groups */
        private volatile Int32 allThreads = 0;

        // TODO do contentType and/or dataEncoding belong in HTTPSampleResult instead?

        private static sealed Boolean startTimeStamp = false;  // $NON-NLS-1$

        // Allow read-only access from test code
        static sealed Boolean USENANOTIME = true;  // $NON-NLS-1$

        // How long between checks of nanotime; default 5000ms; set to <=0 to disable the thread
        private static sealed Int32 NANOTHREAD_SLEEP = 5000;  // $NON-NLS-1$;

        private static NanoOffset Offset;

        private void init()
        {
            //if (startTimeStamp) 
            //{
            //    log.info("Note: Sample TimeStamps are START times");
            //} 
            //else
            //{
            //    log.info("Note: Sample TimeStamps are END times");
            //}
            //log.info("sampleresult.default.encoding is set to " + DEFAULT_ENCODING);
            //log.info("sampleresult.useNanoTime="+USENANOTIME);
            //log.info("sampleresult.nanoThreadSleep="+NANOTHREAD_SLEEP);

            if (USENANOTIME && NANOTHREAD_SLEEP > 0) 
            {
                // Make sure we start with a reasonable value
                Offset = new NanoOffset();
                Offset.nanoOffset = DateTime.Now.TimeOfDay.Ticks - SampleResult.sampleNsClockInMs();

                Thread newThread = new Thread(Offset.run);
                newThread.Start();
            }
        }


        private sealed Int64 nanoTimeOffset;

        // Allow testcode access to the settings
        sealed Boolean useNanoTime;
    
        sealed Int64 nanoThreadSleep;
    
        /**
         * Cache for responseData as string to avoid multiple computations
         */
        private volatile String responseDataAsString;
    
        private Int64 initOffset()
        {
            if (useNanoTime)
            {
                return nanoThreadSleep > 0 ? Offset.getNanoOffset() : DateTime.Now.TimeOfDay.Ticks - sampleNsClockInMs();
            } 
            else 
            {
                return Int64.MinValue;
            }
        }

        public SampleResult() 
            : this(USENANOTIME, NANOTHREAD_SLEEP)
        {
        }

        // Allow test code to change the default useNanoTime setting
        SampleResult(Boolean nanoTime) 
            : this(nanoTime, NANOTHREAD_SLEEP)
        {
        }

        // Allow test code to change the default useNanoTime and nanoThreadSleep settings
        SampleResult(Boolean nanoTime, Int64 nanoThreadSleep) 
        {
            init();
            this.time = 0;
            this.useNanoTime = nanoTime;
            this.nanoThreadSleep = nanoThreadSleep;
            this.nanoTimeOffset = initOffset();
        }

        /**
         * Copy constructor.
         * 
         * @param res existing sample result
         */
        public SampleResult(SampleResult res) : this()
        {
            allThreads = res.allThreads;//OK
            assertionResults = res.assertionResults;// TODO ??
            bytes = res.bytes;
            headersSize = res.headersSize;
            bodySize = res.bodySize;
            contentType = res.contentType;//OK
            dataEncoding = res.dataEncoding;//OK
            dataType = res.dataType;//OK
            endTime = res.endTime;//OK
            // files is created automatically, and applies per instance
            groupThreads = res.groupThreads;//OK
            idleTime = res.idleTime;
            isMon = res.isMon;
            label = res.label;//OK
            latency = res.latency;
            location = res.location;//OK
            parent = res.parent; // TODO ??
            pauseTime = res.pauseTime;
            requestHeaders = res.requestHeaders;//OK
            responseCode = res.responseCode;//OK
            responseData = res.responseData;//OK
            responseDataAsString = null;
            responseHeaders = res.responseHeaders;//OK
            responseMessage = res.responseMessage;//OK
            // Don't copy this; it is per instance resultFileName = res.resultFileName;
            sampleCount = res.sampleCount;
            samplerData = res.samplerData;
            saveConfig = res.saveConfig;
            startTime = res.startTime;//OK
            stopTest = res.stopTest;
            stopTestNow = res.stopTestNow;
            stopThread = res.stopThread;
            startNextThreadLoop = res.startNextThreadLoop;
            subResults = res.subResults; // TODO ??
            success = res.success;//OK
            threadName = res.threadName;//OK
            time = res.time;
            timeStamp = res.timeStamp;
        }

        public Boolean isStampedAtStart()
        {
            return startTimeStamp;
        }

        /**
         * Create a sample with a specific elapsed time but don't allow the times to
         * be changed later
         *
         * (only used by HTTPSampleResult)
         *
         * @param elapsed
         *            time
         * @param atend
         *            create the sample finishing now, else starting now
         */
        protected SampleResult(Int64 elapsed, Boolean atend) 
            : this()
        {
            Int64 now = currentTimeInMillis();
            if (atend) 
            {
                setTimes(now - elapsed, now);
            }
            else
            {
                setTimes(now, now + elapsed);
            }
        }

        /**
         * Create a sample with specific start and end times for test purposes, but
         * don't allow the times to be changed later
         *
         * (used by StatVisualizerModel.Test)
         *
         * @param start
         *            start time
         * @param end
         *            end time
         */
        public static SampleResult createTestSample(Int64 start, Int64 end)
        {
            SampleResult res = new SampleResult();
            res.setStartTime(start);
            res.setEndTime(end);
            return res;
        }

        /**
         * Create a sample with a specific elapsed time for test purposes, but don't
         * allow the times to be changed later
         *
         * @param elapsed -
         *            desired elapsed time
         */
        public static SampleResult createTestSample(Int64 elapsed) 
        {
            Int64 now = DateTime.Now.TimeOfDay.Ticks;
            return createTestSample(now, now + elapsed);
        }

        /**
         * Allow users to create a sample with specific timestamp and elapsed times
         * for cloning purposes, but don't allow the times to be changed later
         *
         * Currently used by OldSaveService, CSVSaveService and StatisticalSampleResult
         *
         * @param stamp -
         *            this may be a start time or an end time
         * @param elapsed
         */
        public SampleResult(Int64 stamp, Int64 elapsed) 
            : this()
        {
            stampAndTime(stamp, elapsed);
        }

        private static long sampleNsClockInMs() 
        {
            return (Int64)DateTime.Now.TimeOfDay.TotalMilliseconds;
        }

        // Helper method to get 1 ms resolution timing.
        public long currentTimeInMillis() 
        {
            if (useNanoTime)
            {
                if (nanoTimeOffset == Int64.MinValue)
                {
                    //throw new RuntimeException("Invalid call; nanoTimeOffset as not been set");
                }
                return sampleNsClockInMs() + nanoTimeOffset;            
            }
            return DateTime.Now.TimeOfDay.Ticks;
        }

        // Helper method to maintain timestamp relationships
        private void stampAndTime(Int64 stamp, Int64 elapsed) 
        {
            if (startTimeStamp) 
            {
                startTime = stamp;
                endTime = stamp + elapsed;
            } 
            else 
            {
                startTime = stamp - elapsed;
                endTime = stamp;
            }
            timeStamp = stamp;
            time = elapsed;
        }

        /*
         * For use by SaveService only.
         *
         * @param stamp -
         *            this may be a start time or an end time
         * @param elapsed
         */
        public void setStampAndTime(Int64 stamp, Int64 elapsed)
        {
            if (startTime != 0 || endTime != 0)
            {
                //throw new RuntimeException("Calling setStampAndTime() after start/end times have been set");
            }
            stampAndTime(stamp, elapsed);
        }

        /**
         * Set the "marked" flag to show that the result has been written to the file.
         *
         * @param filename
         * @return true if the result was previously marked
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Boolean markFile(String filename)
        {
            return !files.Add(filename);
        }

        public String getResponseCode() {
            return responseCode;
        }

        private static sealed String OK_CODE = HttpStatusCode.OK.ToString();
        private static sealed String OK_MSG = "OK"; // $NON-NLS-1$

        /**
         * Set response code to OK, i.e. "200"
         *
         */
        public void setResponseCodeOK()
        {
            responseCode=OK_CODE;
        }

        public void setResponseCode(String code) 
        {
            responseCode = code;
        }

        public Boolean isResponseCodeOK()
        {
            return responseCode.Equals(OK_CODE);
        }
        public String getResponseMessage() 
        {
            return responseMessage;
        }

        public void setResponseMessage(String msg) 
        {
            responseMessage = msg;
        }

        public void setResponseMessageOK() 
        {
            responseMessage = OK_MSG;
        }

        /**
         * Set result statuses OK - shorthand method to set:
         * <ul>
         * <li>ResponseCode</li>
         * <li>ResponseMessage</li>
         * <li>Successful status</li>
         * </ul>
         */
        public void setResponseOK()
        {
            setResponseCodeOK();
            setResponseMessageOK();
            setSuccessful(true);
        }

        public String getThreadName()
        {
            return threadName;
        }

        public void setThreadName(String threadName) 
        {
            this.threadName = threadName;
        }

        /**
         * Get the sample timestamp, which may be either the start time or the end time.
         *
         * @see #getStartTime()
         * @see #getEndTime()
         *
         * @return timeStamp in milliseconds
         */
        public long getTimeStamp() 
        {
            return timeStamp;
        }

        public String getSampleLabel()
        {
            return label;
        }

        /**
         * Get the sample label for use in summary reports etc.
         *
         * @param includeGroup whether to include the thread group name
         * @return the label
         */
        public String getSampleLabel(Boolean includeGroup) 
        {
            if (includeGroup) 
            {
                StringBuilder sb = new StringBuilder(threadName.Substring(0,threadName.LastIndexOf(' '))); //$NON-NLS-1$
                return sb.Append(":").Append(label).ToString(); //$NON-NLS-1$
            }
            return label;
        }

        public void setSampleLabel(String label) 
        {
            this.label = label;
        }

        public void addAssertionResult(AssertionResult assertResult)
        {
            if (assertionResults == null)
            {
                assertionResults = new List<AssertionResult>();
            }
            assertionResults.Add(assertResult);
        }

        /**
         * Gets the assertion results associated with this sample.
         *
         * @return an array containing the assertion results for this sample.
         *         Returns empty array if there are no assertion results.
         */
        public AssertionResult[] getAssertionResults() 
        {
            if (assertionResults == null) 
            {
                return EMPTY_AR;
            }
            return assertionResults.ToArray();
        }

        /**
         * Add a subresult and adjust the parent byte count and end-time.
         * 
         * @param subResult
         */
        public void addSubResult(SampleResult subResult)
        {
            String tn = getThreadName();
            if (0 == tn.Length) 
            {
                tn = Thread.CurrentThread.Name;//TODO do this more efficiently
                this.setThreadName(tn);
            }
            subResult.setThreadName(tn); // TODO is this really necessary?

            // Extend the time to the end of the added sample
            setEndTime(Math.Max(getEndTime(), subResult.getEndTime() + nanoTimeOffset - subResult.nanoTimeOffset)); // Bug 51855
            // Include the byte count for the added sample
            setBytes(getBytes() + subResult.getBytes());
            setHeadersSize(getHeadersSize() + subResult.getHeadersSize());
            setBodySize(getBodySize() + subResult.getBodySize());
            addRawSubResult(subResult);
        }
    
        /**
         * Add a subresult to the collection without updating any parent fields.
         * 
         * @param subResult
         */
        public void addRawSubResult(SampleResult subResult)
        {
            storeSubResult(subResult);
        }

        /**
         * Add a subresult read from a results file.
         *
         * As for addSubResult(), except that the fields don't need to be accumulated
         *
         * @param subResult
         */
        public void storeSubResult(SampleResult subResult) 
        {
            if (subResults == null) 
            {
                subResults = new List<SampleResult>();
            }
            subResults.Add(subResult);
            subResult.setParent(this);
        }

        /**
         * Gets the subresults associated with this sample.
         *
         * @return an array containing the subresults for this sample. Returns an
         *         empty array if there are no subresults.
         */
        public SampleResult[] getSubResults()
        {
            if (subResults == null) {
                return EMPTY_SR;
            }
            return subResults.ToArray();
        }

        /**
         * Sets the responseData attribute of the SampleResult object.
         *
         * If the parameter is null, then the responseData is set to an empty byte array.
         * This ensures that getResponseData() can never be null.
         *
         * @param response
         *            the new responseData value
         */
        public void setResponseData(byte[] response) 
        {
            responseDataAsString = null;
            responseData = response == null ? EMPTY_BA : response;
        }

        /**
         * Sets the responseData attribute of the SampleResult object.
         * Should only be called after setting the dataEncoding (if necessary)
         *
         * @param response
         *            the new responseData value (String)
         *
         * @deprecated - only intended for use from BeanShell code
         */
        public void setResponseData(String response)
        {
            responseDataAsString = null;
            try 
            {
                responseData = Encoding.UTF8.GetBytes(response);
            } 
            catch (FormatException e) 
            {
                log.Warn("Could not convert string, using default encoding. ");
                responseData = Encoding.UTF8.GetBytes(response); // N.B. default charset is used deliberately here
            }
        }

        /**
         * Sets the encoding and responseData attributes of the SampleResult object.
         *
         * @param response the new responseData value (String)
         * @param encoding the encoding to set and then use (if null, use platform default)
         *
         */
        public void setResponseData(String response, String encoding)
        {
            responseDataAsString = null;
            String encodeUsing = encoding != null? encoding : DEFAULT_CHARSET;
            try {
                responseData = Encoding.UTF8.GetBytes(response);
                setDataEncoding(encodeUsing);
            } 
            catch (FormatException e)
            {
                log.Warn("Could not convert string using '" + encodeUsing + "', using default encoding: " + DEFAULT_CHARSET, e);
                responseData = Encoding.UTF8.GetBytes(response); // N.B. default charset is used deliberately here
                setDataEncoding(DEFAULT_CHARSET);
            }
        }

        /**
         * Gets the responseData attribute of the SampleResult object.
         * <p>
         * Note that some samplers may not store all the data, in which case
         * getResponseData().length will be incorrect.
         *
         * Instead, always use {@link #getBytes()} to obtain the sample result byte count.
         * </p>
         * @return the responseData value (cannot be null)
         */
        public byte[] getResponseData()
        {
            return responseData;
        }

        /**
         * Gets the responseData of the SampleResult object as a String
         *
         * @return the responseData value as a String, converted according to the encoding
         */
        public String getResponseDataAsString() 
        {
            try 
            {
                if(responseDataAsString == null)
                {
                    responseDataAsString= Encoding.UTF8.GetString(responseData);
                }
                return responseDataAsString;
            }
            catch (FormatException e)
            {
                log.Warn("Using platform default as UTF-8 caused " + e);
                return Encoding.UTF8.GetString(responseData); // N.B. default charset is used deliberately here
            }
        }

        public void setSamplerData(String s)
        {
            samplerData = s;
        }

        public String getSamplerData() 
        {
            return samplerData;
        }

        /**
         * Get the time it took this sample to occur.
         *
         * @return elapsed time in milliseonds
         *
         */
        public long getTime()
        {
            return time;
        }

        public Boolean isSuccessful()
        {
            return success;
        }

        public void setDataType(String dataType) 
        {
            this.dataType = dataType;
        }

        public String getDataType()
        {
            return dataType;
        }
        /**
         * Extract and save the DataEncoding and DataType from the parameter provided.
         * Does not save the full content Type.
         * @see #setContentType(String) which should be used to save the full content-type string
         *
         * @param ct - content type (may be null)
         */
        public void setEncodingAndType(String ct)
        {
            if (ct != null) {
                // Extract charset and store as DataEncoding
                // N.B. The meta tag:
                // <META http-equiv="content-type" content="text/html; charset=foobar">
                // is now processed by HTTPSampleResult#getDataEncodingWithDefault
                String CS_PFX = "charset="; // $NON-NLS-1$
                int cset = ct.ToLower().IndexOf(CS_PFX);
                if (cset >= 0)
                {
                    String charSet = ct.Substring(cset + CS_PFX.Length);
                    // handle: ContentType: text/plain; charset=ISO-8859-1; format=flowed
                    int semiColon = charSet.IndexOf(';');
                    if (semiColon >= 0) 
                    {
                        charSet=charSet.Substring(0, semiColon);
                    }
                    // Check for quoted string
                    if (charSet.StartsWith("\""))
                    { // $NON-NLS-1$
                        setDataEncoding(charSet.Substring(1, charSet.Length - 1)); // remove quotes
                    } else {
                        setDataEncoding(charSet);
                    }
                }
                if (isBinaryType(ct)) {
                    setDataType(BINARY);
                } else {
                    setDataType(TEXT);
                }
            }
        }

        // List of types that are known to be binary
        private static sealed String[] BINARY_TYPES = {
            "image/",       //$NON-NLS-1$
            "audio/",       //$NON-NLS-1$
            "video/",       //$NON-NLS-1$
            };

        // List of types that are known to be ascii, although they may appear to be binary
        private static sealed String[] NON_BINARY_TYPES = {
            "video/f4m",       //$NON-NLS-1$ (Flash Media Manifest)
            };

        /*
         * Determine if content-type is known to be binary, i.e. not displayable as text.
         *
         * @param ct content type
         * @return true if content-type is of type binary.
         */
        private static Boolean isBinaryType(String ct)
        {
            foreach (String entry in NON_BINARY_TYPES)
            {
                if (ct.StartsWith(entry))
                {
                    return false;
                }
            }
            foreach (String binType in BINARY_TYPES)
            {
                if (ct.StartsWith(binType))
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * Sets the successful attribute of the SampleResult object.
         *
         * @param success
         *            the new successful value
         */
        public void setSuccessful(Boolean success)
        {
            this.success = success;
        }

        /**
         * Returns the display name.
         *
         * @return display name of this sample result
         */
        public override String ToString()
        {
            return getSampleLabel();
        }

        /**
         * Returns the dataEncoding or the default if no dataEncoding was provided.
         * 
         * @return the value of the dataEncoding or DEFAULT_ENCODING
         */
        public String getDataEncodingWithDefault() 
        {
            return getDataEncodingWithDefault(DEFAULT_ENCODING);
        }

        /**
         * Returns the dataEncoding or the default if no dataEncoding was provided.
         * 
         * @param defaultEncoding the default to be applied
         * @return the value of the dataEncoding or the provided default
         */
        protected String getDataEncodingWithDefault(String defaultEncoding) 
        {
            if (dataEncoding != null && dataEncoding.Length > 0) 
            {
                return dataEncoding;
            }
            return defaultEncoding;
        }

        /**
         * Returns the dataEncoding. May be null or the empty String.
         * @return the value of the dataEncoding
         */
        public String getDataEncodingNoDefault()
        {
            return dataEncoding;
        }

        /**
         * Sets the dataEncoding.
         *
         * @param dataEncoding
         *            the dataEncoding to set, e.g. ISO-8895-1, UTF-8
         */
        public void setDataEncoding(String dataEncoding)
        {
            this.dataEncoding = dataEncoding;
        }

        /**
         * @return whether to stop the test
         */
        public Boolean isStopTest() 
        {
            return stopTest;
        }

        /**
         * @return whether to stop the test now
         */
        public Boolean isStopTestNow() 
        {
            return stopTestNow;
        }

        /**
         * @return whether to stop this thread
         */
        public Boolean isStopThread() 
        {
            return stopThread;
        }

        public void setStopTest(Boolean b)
        {
            stopTest = b;
        }

        public void setStopTestNow(Boolean b)
        {
            stopTestNow = b;
        }

        public void setStopThread(Boolean b) 
        {
            stopThread = b;
        }

        /**
         * @return the request headers
         */
        public String getRequestHeaders()
        {
            return requestHeaders;
        }

        /**
         * @return the response headers
         */
        public String getResponseHeaders()
        {
            return responseHeaders;
        }

        /**
         * @param string -
         *            request headers
         */
        public void setRequestHeaders(String str) 
        {
            requestHeaders = str;
        }

        /**
         * @param string -
         *            response headers
         */
        public void setResponseHeaders(String str) 
        {
            responseHeaders = str;
        }

        /**
         * @return the full content type - e.g. text/html [;charset=utf-8 ]
         */
        public String getContentType() 
        {
            return contentType;
        }

        /**
         * Get the media type from the Content Type
         * @return the media type - e.g. text/html (without charset, if any)
         */
        public String getMediaType()
        {
            return contentType.Split(';')[1].ToLower();
        }

        /**
         * Stores the content-type string, e.g. "text/xml; charset=utf-8"
         * @see #setEncodingAndType(String) which can be used to extract the charset.
         *
         * @param string
         */
        public void setContentType(String str)
        {
            contentType = str;
        }

        /**
         * @return idleTime
         */
        public long getIdleTime()
        {
            return idleTime;
        }

        /**
         * @return the end time
         */
        public long getEndTime()
        {
            return endTime;
        }

        /**
         * @return the start time
         */
        public long getStartTime() 
        {
            return startTime;
        }

        /*
         * Helper methods N.B. setStartTime must be called before setEndTime
         *
         * setStartTime is used by HTTPSampleResult to clone the parent sampler and
         * allow the original start time to be kept
         */
        protected sealed void setStartTime(Int64 start) 
        {
            startTime = start;
            if (startTimeStamp) 
            {
                timeStamp = startTime;
            }
        }

        public void setEndTime(Int64 end) 
        {
            endTime = end;
            if (!startTimeStamp)
            {
                timeStamp = endTime;
            }
            if (startTime == 0) {
                //log.error("setEndTime must be called after setStartTime", new Throwable("Invalid call sequence"));
                // TODO should this throw an error?
            } 
            else 
            {
                time = endTime - startTime - idleTime;
            }
        }

        /**
         * Set idle time pause.
         * For use by SampleResultConverter/CSVSaveService.
         * @param idle long
         */
        public void setIdleTime(Int64 idle)
        {
            idleTime = idle;
        }

        private void setTimes(Int64 start, Int64 end) 
        {
            setStartTime(start);
            setEndTime(end);
        }

        /**
         * Record the start time of a sample
         *
         */
        public void sampleStart() 
        {
            if (0 ==startTime) 
            {
                setStartTime(currentTimeInMillis());
            } 
            else
            {
                // log.error("sampleStart called twice", new Throwable("Invalid call sequence"));
            }
        }

        /**
         * Record the end time of a sample and calculate the elapsed time
         *
         */
        public void sampleEnd() 
        {
            if (0 == endTime) 
            {
                setEndTime(currentTimeInMillis());
            } 
            else 
            {
                // log.error("sampleEnd called twice", new Throwable("Invalid call sequence"));
            }
        }

        /**
         * Pause a sample
         *
         */
        public void samplePause()
        {
            if (pauseTime != 0) 
            {
                //log.error("samplePause called twice", new Throwable("Invalid call sequence"));
            }
            pauseTime = currentTimeInMillis();
        }

        /**
         * Resume a sample
         *
         */
        public void sampleResume()
        {
            if (pauseTime == 0) 
            {
                //log.error("sampleResume without samplePause", new Throwable("Invalid call sequence"));
            }
            idleTime += currentTimeInMillis() - pauseTime;
            pauseTime = 0;
        }

        /**
         * When a Sampler is working as a monitor
         *
         * @param monitor
         */
        public void setMonitor(Boolean monitor) 
        {
            isMon = monitor;
        }

        /**
         * If the sampler is a monitor, method will return true.
         *
         * @return true if the sampler is a monitor
         */
        public Boolean isMonitor()
        {
            return isMon;
        }

        /**
         * The statistical sample sender aggregates several samples to save on
         * transmission costs.
         * 
         * @param count number of samples represented by this instance
         */
        public void setSampleCount(Int32 count) 
        {
            sampleCount = count;
        }

        /**
         * return the sample count. by default, the value is 1.
         *
         * @return the sample count
         */
        public int getSampleCount() 
        {
            return sampleCount;
        }

        /**
         * Returns the count of errors.
         *
         * @return 0 - or 1 if the sample failed
         * 
         * TODO do we need allow for nested samples?
         */
        public int getErrorCount()
        {
            return success ? 0 : 1;
        }

        public void setErrorCount(Int32 i)
        {// for reading from CSV files
            // ignored currently
        }

        /*
         * TODO: error counting needs to be sorted out.
         *
         * At present the Statistical Sampler tracks errors separately
         * It would make sense to move the error count here, but this would
         * mean lots of changes.
         * It's also tricky maintaining the count - it can't just be incremented/decremented
         * when the success flag is set as this may be done multiple times.
         * The work-round for now is to do the work in the StatisticalSampleResult,
         * which overrides this method.
         * Note that some JMS samplers also create samples with > 1 sample count
         * Also the Transaction Controller probably needs to be changed to do
         * proper sample and error accounting.
         * The purpose of this work-round is to allow at least minimal support for
         * errors in remote statistical batch mode.
         *
         */
        /**
         * In the event the sampler does want to pass back the actual contents, we
         * still want to calculate the throughput. The bytes is the bytes of the
         * response data.
         *
         * @param length
         */
        public void setBytes(Int32 length)
        {
            bytes = length;
        }

        /**
         * return the bytes returned by the response.
         *
         * @return byte count
         */
        public int getBytes()
        {
            if (GETBYTES_NETWORK_SIZE)
            {
                int tmpSum = this.getHeadersSize() + this.getBodySize();
                return tmpSum == 0 ? bytes : tmpSum;
            } 
            else 
            {
                if (GETBYTES_HEADERS_SIZE) 
                {
                return this.getHeadersSize();
                } 
                else 
                {
                    if (GETBYTES_BODY_REALSIZE)
                    {
                        return this.getBodySize();
                    }
                }
            }
            return bytes == 0 ? responseData.Length : bytes;
        }

        /**
         * @return Returns the latency.
         */
        public long getLatency() 
        {
            return latency;
        }

        /**
         * Set the time to the first response
         *
         */
        public void latencyEnd()
        {
            latency = currentTimeInMillis() - startTime - idleTime;
        }

        /**
         * This is only intended for use by SampleResultConverter!
         *
         * @param latency
         *            The latency to set.
         */
        public void setLatency(Int64 latency) 
        {
            this.latency = latency;
        }

        /**
         * This is only intended for use by SampleResultConverter!
         *
         * @param timeStamp
         *            The timeStamp to set.
         */
        public void setTimeStamp(Int64 timeStamp)
        {
            this.timeStamp = timeStamp;
        }

        private Uri location;

        public void setURL(Uri location)
        {
            this.location = location;
        }

        public Uri getURL() 
        {
            
            return location;
        }

        /**
         * Get a String representation of the URL (if defined).
         *
         * @return ExternalForm of URL, or empty string if url is null
         */
        public String getUrlAsString() 
        {
            return location == null ? "" : location.OriginalString;
        }

        /**
         * @return Returns the parent.
         */
        public SampleResult getParent() 
        {
            return parent;
        }

        /**
         * @param parent
         *            The parent to set.
         */
        public void setParent(SampleResult parent)
        {
            this.parent = parent;
        }

        public String getResultFileName() 
        {
            return resultFileName;
        }

        public void setResultFileName(String resultFileName) 
        {
            this.resultFileName = resultFileName;
        }

        public int getGroupThreads()
        {
            return groupThreads;
        }

        public void setGroupThreads(Int32 n) 
        {
            this.groupThreads = n;
        }

        public int getAllThreads() 
        {
            return allThreads;
        }

        public void setAllThreads(Int32 n) 
        {
            this.allThreads = n;
        }

        // Bug 47394
        /**
         * Allow custom SampleSenders to drop unwanted assertionResults
         */
        public void removeAssertionResults() 
        {
            this.assertionResults = null;
        }

        /**
         * Allow custom SampleSenders to drop unwanted subResults
         */
        public void removeSubResults() 
        {
            this.subResults = null;
        }
    
        /**
         * Set the headers size in bytes
         * 
         * @param size
         */
        public void setHeadersSize(int size)
        {
            this.headersSize = size;
        }
    
        /**
         * Get the headers size in bytes
         * 
         * @return the headers size
         */
        public int getHeadersSize() 
        {
            return headersSize;
        }

        /**
         * @return the body size in bytes
         */
        public int getBodySize() 
        {
            return bodySize == 0 ? responseData.Length : bodySize;
        }

        /**
         * @param bodySize the body size to set
         */
        public void setBodySize(Int32 bodySize) 
        {
            this.bodySize = bodySize;
        }

        public class NanoOffset
        {
            public volatile Int64 nanoOffset; 

            public Int64 getNanoOffset() 
            {
                return nanoOffset;
            }

            public void run() 
            {
                // Wait longer than a clock pulse (generally 10-15ms)
                getOffset(30); // Catch an early clock pulse to reduce slop.
                while(true) 
                {
                    getOffset(NANOTHREAD_SLEEP); // Can now afford to wait a bit longer between checks
                }
            }

            private void getOffset(Int32 wait) 
            {
                try 
                {
                    Thread.Sleep(wait);
                    Int64 clock = DateTime.Now.TimeOfDay.Ticks;
                    Int64 nano = SampleResult.sampleNsClockInMs();
                    nanoOffset = clock - nano;
                }
                catch (Exception ignore) 
                {
                    // ignored
                }
            }
        
        }

        /**
         * @return the startNextThreadLoop
         */
        public Boolean isStartNextThreadLoop() 
        {
            return startNextThreadLoop;
        }

        /**
         * @param startNextThreadLoop the startNextLoop to set
         */
        public void setStartNextThreadLoop(Boolean startNextThreadLoop) 
        {
            this.startNextThreadLoop = startNextThreadLoop;
        }

        /**
         * Clean up cached data
         */
        public void cleanAfterSample() 
        {
            this.responseDataAsString = null;
        }
    }
}
