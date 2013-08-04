using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.TestElements;
using NetMeter.Samplers;
using log4net;
using Valkyrie.Logging;
using NetMeter.Engine.Event;

namespace UPMP.NetMeterPlugin
{
    class TestAgentBase : AbstractTestAgent, TestStateListener
    {
        private static ILog log = LoggingManager.GetLoggerForClass();

        public Boolean GetSendParameterValuesAsPostBody()
        {
            return true;
        }

        /**
         * Determine if we should use multipart/form-data or
         * application/x-www-form-urlencoded for the post
         *
         * @return true if multipart/form-data should be used and method is POST
         */
        public Boolean GetUseMultipartForPost()
        {
            // We use multipart if we have been told so, or files are present
            // and the files should not be send as the post body
            //HTTPFileArg[] files = getHTTPFiles();
            //if (UPMPConstant.POST.equals(getMethod()) && (getDoMultipartPost() || (files.length > 0 && !getSendFileAsPostBody())))
            //{
            //    return true;
            //}
            return false;
        }

        /**
         * Add an argument which has already been encoded
         */
        public void AddEncodedArgument(String name, String value) 
        {
            //this.AddEncodedArgument(name, value, ARG_VAL_SEP);
        }

        /**
         * Creates an HTTPArgument and adds it to the current set {@link #getArguments()} of arguments.
         * 
         * @param name - the parameter name
         * @param value - the parameter value
         * @param metaData - normally just '='
         * @param contentEncoding - the encoding, may be null
         */
        public void AddEncodedArgument(String name, String value, String metaData, String contentEncoding) 
        {
            //if (log.isDebugEnabled()){
            //    log.debug("adding argument: name: " + name + " value: " + value + " metaData: " + metaData + " contentEncoding: " + contentEncoding);
            //}

            //HTTPArgument arg = null;
            //final boolean nonEmptyEncoding = !StringUtils.isEmpty(contentEncoding);
            //if(nonEmptyEncoding) {
            //    arg = new HTTPArgument(name, value, metaData, true, contentEncoding);
            //}
            //else {
            //    arg = new HTTPArgument(name, value, metaData, true);
            //}

            //// Check if there are any difference between name and value and their encoded name and value
            //String valueEncoded = null;
            //if(nonEmptyEncoding) {
            //    try {
            //        valueEncoded = arg.getEncodedValue(contentEncoding);
            //    }
            //    catch (UnsupportedEncodingException e) {
            //        log.warn("Unable to get encoded value using encoding " + contentEncoding);
            //        valueEncoded = arg.getEncodedValue();
            //    }
            //}
            //else {
            //    valueEncoded = arg.getEncodedValue();
            //}
            //// If there is no difference, we mark it as not needing encoding
            //if (arg.getName().equals(arg.getEncodedName()) && arg.getValue().equals(valueEncoded)) 
            //{
            //    arg.setAlwaysEncoded(false);
            //}
            //this.getArguments().addArgument(arg);
        }

        public void AddEncodedArgument(String name, String value, String metaData) 
        {
            //this.addEncodedArgument(name, value, metaData, null);
        }

        public void AddNonEncodedArgument(String name, String value, String metadata) 
        {
            //HTTPArgument arg = new HTTPArgument(name, value, metadata, false);
            //arg.setAlwaysEncoded(false);
            //this.getArguments().addArgument(arg);
        }

        public void AddArgument(String name, String value) 
        {
            //this.getArguments().addArgument(new HTTPArgument(name, value));
        }

        public void AddArgument(String name, String value, String metadata)
        {
            //this.getArguments().addArgument(new HTTPArgument(name, value, metadata));
        }

        public Boolean hasArguments() 
        {
            //return getArguments().getArgumentCount() > 0;
            return true;
        }

        public void AddTestElement(TestElement el) 
        {
            //if (el instanceof CookieManager) {
            //    setCookieManager((CookieManager) el);
            //} else if (el instanceof CacheManager) {
            //    setCacheManager((CacheManager) el);
            //} else if (el instanceof HeaderManager) {
            //    setHeaderManager((HeaderManager) el);
            //} else if (el instanceof AuthManager) {
            //    setAuthManager((AuthManager) el);
            //} else {
            //    super.addTestElement(el);
            //}
        }

        /**
         * {@inheritDoc}
         * <p>
         * Clears the Header Manager property so subsequent loops don't keep merging more elements
         */
        public void ClearTestElementChildren()
        {
            //removeProperty(HEADER_MANAGER);
        }

        /**
         * Populates the provided HTTPSampleResult with details from the Exception.
         * Does not create a new instance, so should not be used directly to add a subsample.
         * 
         * @param e
         *            Exception representing the error.
         * @param res
         *            SampleResult to be modified
         * @return the modified sampling result containing details of the Exception.
         */
        protected TestExecuteResult ErrorResult(TestExecuteResult res) 
        {
            //res.setSampleLabel("Error: " + res.getSampleLabel());
            //res.setDataType(SampleResult.TEXT);
            //ByteArrayOutputStream text = new ByteArrayOutputStream(200);
            //e.printStackTrace(new PrintStream(text));
            //res.setResponseData(text.toByteArray());
            //res.setResponseCode(NON_HTTP_RESPONSE_CODE+": "+e.getClass().getName());
            //res.setResponseMessage(NON_HTTP_RESPONSE_MESSAGE+": "+e.getMessage());
            //res.setSuccessful(false);
            //res.setMonitor(this.isMonitor());
            //return res;
            return null;
        }

        /**
         * Get the URL, built from its component parts.
         *
         * <p>
         * As a special case, if the path starts with "http[s]://",
         * then the path is assumed to be the entire URL.
         * </p>
         *
         * @return The URL to be requested by this sampler.
         * @throws MalformedURLException
         */
        public Uri GetUrl()
        {
            //StringBuilder pathAndQuery = new StringBuilder(100);
            //String path = this.getPath();
            //// Hack to allow entire URL to be provided in host field
            //if (path.startsWith(HTTP_PREFIX)
            // || path.startsWith(HTTPS_PREFIX)){
            //    return new URL(path);
            //}
            //String domain = getDomain();
            //String protocol = getProtocol();
            //if (PROTOCOL_FILE.equalsIgnoreCase(protocol)) {
            //    domain=null; // allow use of relative file URLs
            //} else {
            //    // HTTP URLs must be absolute, allow file to be relative
            //    if (!path.startsWith("/")){ // $NON-NLS-1$
            //        pathAndQuery.append("/"); // $NON-NLS-1$
            //    }
            //}
            //pathAndQuery.append(path);

            //// Add the query string if it is a HTTP GET or DELETE request
            //if(UPMPConstant.GET.equals(getMethod()) || UPMPConstant.DELETE.equals(getMethod())) {
            //    // Get the query string encoded in specified encoding
            //    // If no encoding is specified by user, we will get it
            //    // encoded in UTF-8, which is what the HTTP spec says
            //    String queryString = getQueryString(getContentEncoding());
            //    if(queryString.length() > 0) {
            //        if (path.indexOf(QRY_PFX) > -1) {// Already contains a prefix
            //            pathAndQuery.append(QRY_SEP);
            //        } else {
            //            pathAndQuery.append(QRY_PFX);
            //        }
            //        pathAndQuery.append(queryString);
            //    }
            //}
            //// If default port for protocol is used, we do not include port in URL
            //if(isProtocolDefaultPort()) {
            //    return new URL(protocol, domain, pathAndQuery.toString());
            //}
            //return new URL(protocol, domain, getPort(), pathAndQuery.toString());
            return null;
        }

        /**
         * Gets the QueryString attribute of the UrlConfig object, using
         * UTF-8 to encode the URL
         *
         * @return the QueryString value
         */
        public String GetQueryString() {
            // We use the encoding which should be used according to the HTTP spec, which is UTF-8
            //return getQueryString(EncoderCache.URL_ARGUMENT_ENCODING);
            return "";
        }

        /**
         * Gets the QueryString attribute of the UrlConfig object, using the
         * specified encoding to encode the parameter values put into the URL
         *
         * @param contentEncoding the encoding to use for encoding parameter values
         * @return the QueryString value
         */
        public String GetQueryString(String contentEncoding) 
        {
             // Check if the sampler has a specified content encoding
            // if(JOrphanUtils.isBlank(contentEncoding)) {
            //     // We use the encoding which should be used according to the HTTP spec, which is UTF-8
            //     contentEncoding = EncoderCache.URL_ARGUMENT_ENCODING;
            // }
            //StringBuilder buf = new StringBuilder();
            //PropertyIterator iter = getArguments().iterator();
            //boolean first = true;
            //while (iter.hasNext()) {
            //    HTTPArgument item = null;
            //    /*
            //     * N.B. Revision 323346 introduced the ClassCast check, but then used iter.next()
            //     * to fetch the item to be cast, thus skipping the element that did not cast.
            //     * Reverted to work more like the original code, but with the check in place.
            //     * Added a warning message so can track whether it is necessary
            //     */
            //    Object objectValue = iter.next().getObjectValue();
            //    try {
            //        item = (HTTPArgument) objectValue;
            //    } catch (ClassCastException e) {
            //        log.warn("Unexpected argument type: "+objectValue.getClass().getName());
            //        item = new HTTPArgument((Argument) objectValue);
            //    }
            //    final String encodedName = item.getEncodedName();
            //    if (encodedName.length() == 0) {
            //        continue; // Skip parameters with a blank name (allows use of optional variables in parameter lists)
            //    }
            //    if (!first) {
            //        buf.append(QRY_SEP);
            //    } else {
            //        first = false;
            //    }
            //    buf.append(encodedName);
            //    if (item.getMetaData() == null) {
            //        buf.append(ARG_VAL_SEP);
            //    } else {
            //        buf.append(item.getMetaData());
            //    }

            //    // Encode the parameter value in the specified content encoding
            //    try {
            //        buf.append(item.getEncodedValue(contentEncoding));
            //    }
            //    catch(UnsupportedEncodingException e) {
            //        log.warn("Unable to encode parameter in encoding " + contentEncoding + ", parameter value not included in query string");
            //    }
            //}
            //return buf.toString();
            return "";
        }

        // Mark Walsh 2002-08-03, modified to also parse a parameter name value
        // string, where string contains only the parameter name and no equal sign.
        /**
         * This method allows a proxy server to send over the raw text from a
         * browser's output stream to be parsed and stored correctly into the
         * UrlConfig object.
         *
         * For each name found, addArgument() is called
         *
         * @param queryString -
         *            the query string, might be the post body of a http post request.
         * @param contentEncoding -
         *            the content encoding of the query string; 
         *            if non-null then it is used to decode the 
         */
        public void ParseArguments(String queryString, String contentEncoding) {
            //String[] args = JOrphanUtils.split(queryString, QRY_SEP);
            //for (int i = 0; i < args.length; i++) {
            //    // need to handle four cases:
            //    // - string contains name=value
            //    // - string contains name=
            //    // - string contains name
            //    // - empty string

            //    String metaData; // records the existance of an equal sign
            //    String name;
            //    String value;
            //    int length = args[i].length();
            //    int endOfNameIndex = args[i].indexOf(ARG_VAL_SEP);
            //    if (endOfNameIndex != -1) {// is there a separator?
            //        // case of name=value, name=
            //        metaData = ARG_VAL_SEP;
            //        name = args[i].substring(0, endOfNameIndex);
            //        value = args[i].substring(endOfNameIndex + 1, length);
            //    } else {
            //        metaData = "";
            //        name=args[i];
            //        value="";
            //    }
            //    if (name.length() > 0) {
            //        // If we know the encoding, we can decode the argument value,
            //        // to make it easier to read for the user
            //        if(!StringUtils.isEmpty(contentEncoding)) {
            //            addEncodedArgument(name, value, metaData, contentEncoding);
            //        }
            //        else {
            //            // If we do not know the encoding, we just use the encoded value
            //            // The browser has already done the encoding, so save the values as is
            //            addNonEncodedArgument(name, value, metaData);
            //        }
            //    }
            //}
        }

        public void ParseArguments(String queryString)
        {
            // We do not know the content encoding of the query string
            ParseArguments(queryString, null);
        }

        public String ToString() 
        {
            //try {
            //    StringBuilder stringBuffer = new StringBuilder();
            //    stringBuffer.append(this.getUrl().toString());
            //    // Append body if it is a post or put
            //    if(UPMPConstant.POST.equals(getMethod()) || UPMPConstant.PUT.equals(getMethod())) {
            //        stringBuffer.append("\nQuery Data: ");
            //        stringBuffer.append(getQueryString());
            //    }
            //    return stringBuffer.toString();
            //} catch (MalformedURLException e) {
            //    return "";
            //}
            return "";
        }

        /**
         * Do a sampling and return its results.
         *
         * @param e
         *            <code>Entry</code> to be sampled
         * @return results of the sampling
         */
        public ExecuteResult Execute(Entry e)
        {
            return Execute();
        }

        /**
         * Perform a sample, and return the results
         *
         * @return results of the sampling
         */
        public ExecuteResult Execute() 
        {
            //ExecuteResult res = null;
            //try 
            //{
            //    res = Execute(getUrl(), getMethod(), false, 0);
            //    res.setSampleLabel(getName());
            //    return res;
            //} catch (Exception e) {
            //    return errorResult(e, new UPMPSampleResult());
            //}
            return null;
        }

        /**
         * Samples the URL passed in and stores the result in
         * <code>HTTPSampleResult</code>, following redirects and downloading
         * page resources as appropriate.
         * <p>
         * When getting a redirect target, redirects are not followed and resources
         * are not downloaded. The caller will take care of this.
         *
         * @param u
         *            URL to sample
         * @param method
         *            HTTP method: GET, POST,...
         * @param areFollowingRedirect
         *            whether we're getting a redirect target
         * @param depth
         *            Depth of this target in the frame structure. Used only to
         *            prevent infinite recursion.
         * @return results of the sampling
         */
        protected abstract TestExecuteResult Execute(Uri u, String method, Boolean areFollowingRedirect, int depth);

	    /*
         * @param res HTTPSampleResult to check
         * @return parser class name (may be "") or null if entry does not exist
         */
        private String GetParserClass(TestExecuteResult res) 
        {
            //final String ct = res.getMediaType();
            //return parsersForType.get(ct);
            return "";
        }

        /**
         * {@inheritDoc}
         */
        public void TestEnded() 
        {
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
        public void TestIterationStart(LoopIterationEvent iterEvent) 
        {
            //if (!USE_CACHED_SSL_CONTEXT) {
            //    JsseSSLManager sslMgr = (JsseSSLManager) SSLManager.getInstance();
            //    sslMgr.resetContext();
            //    notifySSLContextWasReset();
            //}
	    }


        /**
         * {@inheritDoc}
         */
        public void TestStarted() 
        {
        }

        /**
         * {@inheritDoc}
         */
        public void TestStarted(String host) {
            TestStarted();
        }

        /**
         * Follow redirects and download page resources if appropriate. this works,
         * but the container stuff here is what's doing it. followRedirects() is
         * actually doing the work to make sure we have only one container to make
         * this work more naturally, I think this method - sample() - needs to take
         * an HTTPSamplerResult container parameter instead of a
         * boolean:areFollowingRedirect.
         *
         * @param areFollowingRedirect
         * @param frameDepth
         * @param res
         * @return the sample result
         */
        protected TestExecuteResult ResultProcessing(Boolean areFollowingRedirect, int frameDepth, TestExecuteResult res) 
        {
            //Boolean wasRedirected = false;
            //if (!areFollowingRedirect) 
            //{
            //    if (res.isRedirect()) 
            //    {
            //        log.Debug("Location set to - " + res.getRedirectLocation());

            //        if (getFollowRedirects()) {
            //            res = followRedirects(res, frameDepth);
            //            areFollowingRedirect = true;
            //            wasRedirected = true;
            //        }
            //    }
            //}
            //if (isImageParser() && (SampleResult.TEXT).equals(res.getDataType()) && res.isSuccessful()) {
            //    if (frameDepth > MAX_FRAME_DEPTH) {
            //        res.addSubResult(errorResult(new Exception("Maximum frame/iframe nesting depth exceeded."), new UPMPSampleResult(res)));
            //    } else {
            //        // Only download page resources if we were not redirected.
            //        // If we were redirected, the page resources have already been
            //        // downloaded for the sample made for the redirected url
            //        // otherwise, use null so the container is created if necessary unless
            //        // the flag is false, in which case revert to broken 2.1 behaviour 
            //        // Bug 51939 -  https://issues.apache.org/bugzilla/show_bug.cgi?id=51939
            //        if(!wasRedirected) {
            //            UPMPSampleResult container = (UPMPSampleResult) (
            //                    areFollowingRedirect ? res.getParent() : SEPARATE_CONTAINER ? null : res);
            //            res = downloadPageResources(res, container, frameDepth);
            //        }
            //    }
            //}
            return res;
        }

        /**
         * Determine if the HTTP status code is successful or not
         * i.e. in range 200 to 399 inclusive
         *
         * @return whether in range 200-399 or not
         */
        protected Boolean isSuccessCode(int code)
        {
            return (code >= 200 && code <= 399);
        }

 

        public static String[] GetValidMethodsAsArray(){
            //return METHODLIST.toArray(new String[METHODLIST.size()]);
            return null;
        }

        public static Boolean isSecure(String protocol){
            //return UPMPConstant.PROTOCOL_HTTPS.equalsIgnoreCase(protocol);
            return true;
        }

        // Implement these here, to avoid re-implementing for sub-classes
        // (previously these were implemented in all TestElements)
        public void ThreadStarted(){
        }

        public void ThreadFinished(){
        }

        /**
         * Read response from the input stream, converting to MD5 digest if the useMD5 property is set.
         *
         * For the MD5 case, the result byte count is set to the size of the original response.
         * 
         * Closes the inputStream 
         * 
         * @param sampleResult
         * @param in input stream
         * @param length expected input length or zero
         * @return the response or the MD5 of the response
         * @throws IOException
         */
        public byte[] ReadResponse(ExecuteResult sampleResult, int length)
        {
            //try {
            //    byte[] readBuffer = new byte[8192]; // 8kB is the (max) size to have the latency ('the first packet')
            //    int bufferSize=32;// Enough for MD5
    
            //    MessageDigest md=null;
            //    boolean asMD5 = useMD5();
            //    if (asMD5) {
            //        try {
            //            md = MessageDigest.getInstance("MD5"); //$NON-NLS-1$
            //        } catch (NoSuchAlgorithmException e) {
            //            log.error("Should not happen - could not find MD5 digest", e);
            //            asMD5=false;
            //        }
            //    } else {
            //        if (length <= 0) {// may also happen if long value > int.max
            //            bufferSize = 4 * 1024;
            //        } else {
            //            bufferSize = length;
            //        }
            //    }
            //    ByteArrayOutputStream w = new ByteArrayOutputStream(bufferSize);
            //    int bytesRead = 0;
            //    int totalBytes = 0;
            //    boolean first = true;
            //    while ((bytesRead = in.read(readBuffer)) > -1) {
            //        if (first) {
            //            sampleResult.latencyEnd();
            //            first = false;
            //        }
            //        if (asMD5 && md != null) {
            //            md.update(readBuffer, 0 , bytesRead);
            //            totalBytes += bytesRead;
            //        } else {
            //            w.write(readBuffer, 0, bytesRead);
            //        }
            //    }
            //    if (first){ // Bug 46838 - if there was no data, still need to set latency
            //        sampleResult.latencyEnd();
            //    }
            //    in.close();
            //    w.flush();
            //    if (asMD5 && md != null) {
            //        byte[] md5Result = md.digest();
            //        w.write(JOrphanUtils.baToHexBytes(md5Result)); 
            //        sampleResult.setBytes(totalBytes);
            //    }
            //    w.close();
            //    return w.toByteArray();
            //} finally {
            //    IOUtils.closeQuietly(in);
            //}
            return null;
        }
    }
}
