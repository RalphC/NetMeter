using System;

namespace NetMeter.Threads
{
    public class ThreadCounts
    {
        public sealed Int32 activeThreads;

        public sealed Int32 startedThreads;

        public sealed Int32 finishedThreads;

        public ThreadCounts(Int32 active, Int32 started, Int32 finished)
        {
            this.activeThreads = active;
            this.startedThreads = started;
            this.finishedThreads = finished;
        }
    }
}
