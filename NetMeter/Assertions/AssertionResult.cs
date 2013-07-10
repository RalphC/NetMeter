using System;
using System.Runtime.Serialization;

namespace NetMeter.Assertions
{
    public class AssertionResult : ISerializable
    {
        public static sealed String RESPONSE_WAS_NULL = "Response was null"; // $NON-NLS-1$

        private static sealed Int64 serialVersionUID = 240L;

        /** Name of the assertion. */
        private sealed String name;

        /** True if the assertion failed. */
        private Boolean failure;

        /** True if there was an error checking the assertion. */
        private Boolean error;

        /** A message describing the failure. */
        private String failureMessage;

        /**
         * Create a new Assertion Result. The result will indicate no failure or
         * error.
         * @deprecated - use the named constructor
         */
        public AssertionResult() 
        { // Needs to be public for tests
            this.name = null;
        }

        /**
         * Create a new Assertion Result. The result will indicate no failure or
         * error.
         *
         * @param name the name of the assertion
         */
        public AssertionResult(String name) 
        {
            this.name = name;
        }

        /**
         * Get the name of the assertion
         *
         * @return the name of the assertion
         */
        public String getName() 
        {
            return name;
        }

        /**
         * Check if the assertion failed. If it failed, the failure message may give
         * more details about the failure.
         *
         * @return true if the assertion failed, false if the sample met the
         *         assertion criteria
         */
        public Boolean isFailure() 
        {
            return failure;
        }

        /**
         * Check if an error occurred while checking the assertion. If an error
         * occurred, the failure message may give more details about the error.
         *
         * @return true if an error occurred while checking the assertion, false
         *         otherwise.
         */
        public Boolean isError() 
        {
            return error;
        }

        /**
         * Get the message associated with any failure or error. This method may
         * return null if no message was set.
         *
         * @return a failure or error message, or null if no message has been set
         */
        public String getFailureMessage() 
        {
            return failureMessage;
        }

        /**
         * Set the flag indicating whether or not an error occurred.
         *
         * @param e
         *            true if an error occurred, false otherwise
         */
        public void setError(Boolean isErr) 
        {
            error = isErr;
        }

        /**
         * Set the flag indicating whether or not a failure occurred.
         *
         * @param f
         *            true if a failure occurred, false otherwise
         */
        public void setFailure(Boolean isFail)
        {
            failure = isFail;
        }

        /**
         * Set the failure message giving more details about a failure or error.
         *
         * @param message
         *            the message to set
         */
        public void setFailureMessage(String message) 
        {
            failureMessage = message;
        }

        /**
         * Convenience method for setting up failed results
         *
         * @param message
         *            the message to set
         * @return this
         *
         */
        public AssertionResult setResultForFailure(String message) 
        {
            error = false;
            failure = true;
            failureMessage = message;
            return this;
        }

        /**
         * Convenience method for setting up results where the response was null
         *
         * @return assertion result with appropriate fields set up
         */
        public AssertionResult setResultForNull() 
        {
            error = false;
            failure = true;
            failureMessage = RESPONSE_WAS_NULL;
            return this;
        }

        public override String ToString()
        {
            return getName() != null ? getName() : base.ToString();
        }
    }
}
