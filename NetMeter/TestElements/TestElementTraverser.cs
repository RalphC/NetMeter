using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMeter.TestElements.Property;

namespace NetMeter.TestElements
{
    public interface TestElementTraverser
    {
        /**
         * Notification that a new test element is about to be traversed.
         *
         * @param el
         */
        void startTestElement(TestElement el);

        /**
         * Notification that the test element is now done.
         *
         * @param el
         */
        void endTestElement(TestElement el);

        /**
         * Notification that a property is starting. This could be a test element
         * property or a Map property - depends on the context.
         *
         * @param key
         */
        void startProperty(NetMeterProperty key);

        /**
         * Notification that a property is ending. Again, this could be a test
         * element or a Map property, dependig on the context.
         *
         * @param key
         */
        void endProperty(NetMeterProperty key);
    }
}
