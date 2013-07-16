using System;
using NetMeter.Threads;
using NetMeter.TestElements.Property;

namespace NetMeter.TestElements
{
    public interface TestElement : ICloneable
    {
        public static String NAME = "TestElement.name"; //$NON-NLS-1$

        public String GUI_CLASS = "TestElement.gui_class"; //$NON-NLS-1$

        public static String ENABLED = "TestElement.enabled"; //$NON-NLS-1$

        public String TEST_CLASS = "TestElement.test_class"; //$NON-NLS-1$

        // Needed by AbstractTestElement.
        // Also TestElementConverter and TestElementPropertyConverter for handling empty comments
        public String COMMENTS = "TestPlan.comments"; //$NON-NLS-1$
        // N.B. Comments originally only applied to Test Plans, hence the name - which can now not be easily changed

        void addTestElement(TestElement child);

        // This method should clear any test element properties that are merged
        void ClearTestElementChildren();

        void setProperty(String key, String value);

        void setProperty(String key, String value, String dflt);

        void setProperty(String key, bool value);

        void setProperty(String key, bool value, bool dflt);

        void setProperty(String key, Int32 value);

        void setProperty(String key, Int32 value, Int32 dflt);

        // Check if ENABLED property is present and true ; defaults to true ; return true if element is enabled
        bool isEnabled();

        bool isRunningVersion();

        bool getPropertyAsBoolean(String key);

        bool getPropertyAsBoolean(String key, bool defaultValue);

        Int64 getPropertyAsLong(String key);

        Int64 getPropertyAsLong(String key, Int64 defaultValue);

        Int32 getPropertyAsInt(String key);

        Int32 getPropertyAsInt(String key, Int32 defaultValue);

        float getPropertyAsFloat(String key);

        double getPropertyAsDouble(String key);

        /**
         * Make the test element the running version, or make it no longer the
         * running version. This tells the test element that it's current state must
         * be retrievable by a call to recoverRunningVersion(). It is kind of like
         * making the TestElement Read- Only, but not as strict. Changes can be made
         * and the element can be modified, but the state of the element at the time
         * of the call to setRunningVersion() must be recoverable.
         */
        void SetRunningVersion(bool run);

        /**
         * Tells the test element to return to the state it was in when
         * setRunningVersion(true) was called.
         */
        void RecoverRunningVersion();

        /**
         * Clear the TestElement of all data.
         */
        void Clear();
        // TODO - yet another ambiguous name - does it need changing?
        // See also: Clearable, JMeterGUIComponent

        String getPropertyAsString(String key);

        String getPropertyAsString(String key, String defaultValue);

        /**
         * Sets and overwrites a property in the TestElement. This call will be
         * ignored if the TestElement is currently a "running version".
         */
        void setProperty(NetMeterProperty property);

        /**
         * Given the name of the property, returns the appropriate property from
         * JMeter. If it is null, a NullProperty object will be returned.
         */
        NetMeterProperty getProperty(String propName);

        /**
         * Get a Property Iterator for the TestElements properties.
         *
         * @return PropertyIterator
         */
        PropertyIterator propertyIterator();

        void RemoveProperty(String key);

        // lifecycle methods

        Object Clone();

        /**
         * Convenient way to traverse a test element.
         */
        void Traverse(TestElementTraverser traverser);

        /**
         * @return Returns the threadContext.
         */
        NetMeterContext getThreadContext();

        /**
         * @param threadContext
         *            The threadContext to set.
         */
        void SetThreadContext(NetMeterContext threadContext);

        /**
         * @return Returns the threadName.
         */
        String getThreadName();

        /**
         * @param threadName
         *            The threadName to set.
         */
        void SetThreadName(String threadName);

        /**
         * Called by Remove to determine if it is safe to remove the element. The
         * element can either clean itself up, and return true, or the element can
         * return false.
         *
         * @return true if safe to remove the element
         */
        bool canRemove();

        String GetName();

        void SetName(String name);

        String getComment();

        void setComment(String comment);
    }
}
