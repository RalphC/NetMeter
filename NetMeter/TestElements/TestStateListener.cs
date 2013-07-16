using System;

namespace NetMeter.TestElements
{
    interface TestStateListener
    {
        /**
         * <p>
         * Called just before the start of the test from the main engine thread.
         *
         * This is before the test elements are cloned.
         *
         * Note that not all the test
         * variables will have been set up at this point.
         * </p>
         *
         * <p>
         * <b>
         * N.B. testStarted() and testEnded() are called from different threads.
         * </b>
         * </p>
         * @see org.apache.jmeter.engine.StandardJMeterEngine#run()
         *
         */
        void TestStarted();

        /**
         * <p>
         * Called just before the start of the test from the main engine thread.
         *
         * This is before the test elements are cloned.
         *
         * Note that not all the test
         * variables will have been set up at this point.
         * </p>
         *
         * <p>
         * <b>
         * N.B. testStarted() and testEnded() are called from different threads.
         * </b>
         * </p>
         * @see org.apache.jmeter.engine.StandardJMeterEngine#run()
         * @param host name of host
         */
        void TestStarted(String host);

        /**
         * <p>
         * Called once for all threads after the end of a test.
         *
         * This will use the same element instances as at the start of the test.
         * </p>
         *
         * <p>
         * <b>
         * N.B. testStarted() and testEnded() are called from different threads.
         * </b>
         * </p>
         * @see org.apache.jmeter.engine.StandardJMeterEngine#stopTest()
         *
         */
        void TestEnded();

        /**
         * <p>
         * Called once for all threads after the end of a test.
         *
         * This will use the same element instances as at the start of the test.
         * </p>
         *
         * <p>
         * <b>
         * N.B. testStarted() and testEnded() are called from different threads.
         * </b>
         * </p>
         * @see org.apache.jmeter.engine.StandardJMeterEngine#stopTest()
         * @param host name of host
         *
         */

        void TestEnded(String host);
    }
}
