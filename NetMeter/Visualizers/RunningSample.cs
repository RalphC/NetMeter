using NetMeter.Samplers;
using System;
using System.Text;

namespace NetMeter.Visualizers
{
    public class RunningSample
    {
        private Int64 counter;

        private Int64 runningSum;

        private Int64 max, min;

        private Int64 errorCount;

        private Int64 firstTime;

        private Int64 lastTime;

        private String label;

        private Int32 index;

        /**
         * Use this constructor to create the initial instance
         */
        public RunningSample(String label, int index) 
        {
            this.label = label;
            this.index = index;
            init();
        }

        /**
         * Copy constructor to create a duplicate of existing instance (without the
         * disadvantages of clone()
         *
         * @param src existing RunningSample to be copied
         */
        public RunningSample(RunningSample src) 
        {
            this.counter = src.counter;
            this.errorCount = src.errorCount;
            this.firstTime = src.firstTime;
            this.index = src.index;
            this.label = src.label;
            this.lastTime = src.lastTime;
            this.max = src.max;
            this.min = src.min;
            this.runningSum = src.runningSum;
        }

        private void init() 
        {
            counter = 0L;
            runningSum = 0L;
            max = Int64.MinValue;
            min = Int64.MaxValue;
            errorCount = 0L;
            firstTime = Int64.MaxValue;
            lastTime = 0L;
        }

        /**
         * Clear the counters (useful for differential stats)
         *
         */
        public void Clear() 
        {
            init();
        }

        /**
         * Get the elapsed time for the samples
         *
         * @return how long the samples took
         */
        public long getElapsed() 
        {
            if (lastTime == 0) 
            {
                return 0;// No samples collected ...
            }
            return lastTime - firstTime;
        }

        /**
         * Returns the throughput associated to this sampler in requests per second.
         * May be slightly skewed because it takes the timestamps of the first and
         * last samples as the total time passed, and the test may actually have
         * started before that start time and ended after that end time.
         */
        public double getRate() 
        {
            if (counter == 0) 
            {
                return 0.0; // Better behaviour when howLong=0 or lastTime=0
            }

            long howLongRunning = lastTime - firstTime;

            if (howLongRunning == 0) 
            {
                return Double.MaxValue;
            }

            return (double) counter / howLongRunning * 1000.0;
        }

        /**
         * Returns the throughput associated to this sampler in requests per min.
         * May be slightly skewed because it takes the timestamps of the first and
         * last samples as the total time passed, and the test may actually have
         * started before that start time and ended after that end time.
         */
        public double getRatePerMin() 
        {
            if (counter == 0) 
            {
                return 0.0; // Better behaviour when howLong=0 or lastTime=0
            }

            long howLongRunning = lastTime - firstTime;

            if (howLongRunning == 0) 
            {
                return Double.MaxValue;
            }
            return (double) counter / howLongRunning * 60000.0;
        }

        /**
         * Returns a String that represents the throughput associated for this
         * sampler, in units appropriate to its dimension:
         * <p>
         * The number is represented in requests/second or requests/minute or
         * requests/hour.
         * <p>
         * Examples: "34.2/sec" "0.1/sec" "43.0/hour" "15.9/min"
         *
         * @return a String representation of the rate the samples are being taken
         *         at.
         */
        public String getRateString() 
        {
            double rate = getRate();

            if (rate == Double.MaxValue) 
            {
                return "N/A";
            }

            String unit = "sec";

            if (rate < 1.0) 
            {
                rate *= 60.0;
                unit = "min";
            }
            if (rate < 1.0) 
            {
                rate *= 60.0;
                unit = "hour";
            }

            return String.Format("{0:#.##}/{1}", rate, unit);
        }

        public String getLabel() {
            return label;
        }

        public int getIndex() {
            return index;
        }

        /**
         * Records a sample.
         *
         */
        public void AddSample(ExecuteResult res)
        {
            Int64 aTimeInMillis = res.GetTime();
        
            counter+=res.getSampleCount();
            errorCount += res.getErrorCount();

            Int64 startTime = res.getStartTime();
            Int64 endTime = res.getEndTime();

            if (firstTime > startTime) 
            {
                // this is our first sample, set the start time to current timestamp
                firstTime = startTime;
            }

            // Always update the end time
            if (lastTime < endTime) 
            {
                lastTime = endTime;
            }
            runningSum += aTimeInMillis;

            if (aTimeInMillis > max) 
            {
                max = aTimeInMillis;
            }

            if (aTimeInMillis < min) 
            {
                min = aTimeInMillis;
            }

        }

        /**
         * Adds another RunningSample to this one.
         * Does not check if it has the same label and index.
         */
        public void AddSample(RunningSample rs) 
        {
            this.counter += rs.counter;
            this.errorCount += rs.errorCount;
            this.runningSum += rs.runningSum;
            if (this.firstTime > rs.firstTime) 
            {
                this.firstTime = rs.firstTime;
            }
            if (this.lastTime < rs.lastTime) 
            {
                this.lastTime = rs.lastTime;
            }
            if (this.max < rs.max) 
            {
                this.max = rs.max;
            }
            if (this.min > rs.min)
            {
                this.min = rs.min;
            }
        }

        /**
         * Returns the time in milliseconds of the quickest sample.
         *
         * @return the time in milliseconds of the quickest sample.
         */
        public long getMin() 
        {
            long rval = 0;

            if (min != Int64.MaxValue) 
            {
                rval = min;
            }
            return rval;
        }

        /**
         * Returns the time in milliseconds of the slowest sample.
         *
         * @return the time in milliseconds of the slowest sample.
         */
        public long getMax() {
            long rval = 0;

            if (max != Int64.MinValue)
            {
                rval = max;
            }
            return rval;
        }

        /**
         * Returns the average time in milliseconds that samples ran in.
         *
         * @return the average time in milliseconds that samples ran in.
         */
        public long getAverage() 
        {
            if (counter == 0) {
                return 0;
            }
            return runningSum / counter;
        }

        /**
         * Returns the number of samples that have been recorded by this instance of
         * the RunningSample class.
         *
         * @return the number of samples that have been recorded by this instance of
         *         the RunningSample class.
         */
        public long getNumSamples() 
        {
            return counter;
        }

        /**
         * Returns the raw double value of the percentage of samples with errors
         * that were recorded. (Between 0.0 and 1.0) If you want a nicer return
         * format, see {@link #getErrorPercentageString()}.
         *
         * @return the raw double value of the percentage of samples with errors
         *         that were recorded.
         */
        public double getErrorPercentage() 
        {
            double rval = 0.0;

            if (counter == 0) {
                return rval;
            }
            rval = (double) errorCount / (double) counter;
            return rval;
        }

        /**
         * Returns a String which represents the percentage of sample errors that
         * have occurred. ("0.00%" through "100.00%")
         *
         * @return a String which represents the percentage of sample errors that
         *         have occurred.
         */
        public String getErrorPercentageString() 
        {
            double myErrorPercentage = this.getErrorPercentage();

            return String.Format("{0:0.00%}", myErrorPercentage);
        }

        /**
         * For debugging purposes, mainly.
         */
        public String ToString() 
        {
            StringBuilder mySB = new StringBuilder();

            mySB.Append("Samples: " + this.getNumSamples() + "  ");
            mySB.Append("Avg: " + this.getAverage() + "  ");
            mySB.Append("Min: " + this.getMin() + "  ");
            mySB.Append("Max: " + this.getMax() + "  ");
            mySB.Append("Error Rate: " + this.getErrorPercentageString() + "  ");
            mySB.Append("Sample Rate: " + this.getRateString());
            return mySB.ToString();
        }

        /**
         * @return errorCount
         */
        public long getErrorCount() 
        {
            return errorCount;
        }
    }
}
