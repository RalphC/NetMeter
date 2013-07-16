using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NetMeter.TestElements.Property;
using NetMeter.Threads;
using Valkyrie.Logging;
using log4net;

namespace NetMeter.TestElements
{
	public abstract class AbstractTestElement : TestElement, ISerializable
	{
        private static sealed ILog log = LoggingManager.GetLoggerForClass();

		private sealed Dictionary<String, NetMeterProperty> propDic = new Dictionary<String, NetMeterProperty>();

		/**
		 * Holds properties added when isRunningVersion is true
		 */
		private HashSet<NetMeterProperty> temporaryProperties;

		private bool runningVersion = false;

		// Thread-specific variables saved here to save recalculation
		private NetMeterContext threadContext = null;

		private String threadName = null;

		public object Clone()
		{
			try
			{
				TestElement clonedElement = (TestElement)this.Clone();

				return clonedElement;
			}
			catch (System.Exception ex)
			{
				throw ex;
			}
		}

		/**
		 * Clear properties
		 */
		public void Clear() 
		{
			propDic.Clear();
		}

		/**
		 * Default implementation - does nothing
		 */
		public void ClearTestElementChildren()
		{
			// NOOP
		}

		/**
		 * Remove key
		 */
		public void RemoveProperty(String key) 
		{
			propDic.Remove(key);
		}

		/**
		 * {@inheritDoc}
		 */
		public bool Equals(Object o) 
		{
			if (o is AbstractTestElement) {
				return ((AbstractTestElement) o).propDic.Equals(propDic);
			} else {
				return false;
			}
		}

		// TODO temporary hack to avoid unnecessary bug reports for subclasses

		/*
		 * URGENT: TODO - sort out equals and hashCode() - at present equal
		 * instances can/will have different hashcodes - problem is, when a proper
		 * hashcode is used, tests stop working, e.g. listener data disappears when
		 * switching views... This presumably means that instances currently
		 * regarded as equal, aren't really equal.
		 *
		 * @see java.lang.Object#hashCode()
		 */

		/**
		 * {@inheritDoc}
		 */
		public void addTestElement(TestElement el)
		{
			mergeIn(el);
		}

		public void SetName(String name) 
		{
			setProperty(TestElement.NAME, name);
		}

		public String GetName() 
		{
			return TestElement.NAME;
		}

		public void setComment(String comment)
		{
			setProperty(new StringProperty(TestElement.COMMENTS, comment));
		}

		public String getComment()
		{
			return getProperty(TestElement.COMMENTS).getStringValue();
		}

		/**
		 * Get the named property. If it doesn't exist, a new NullProperty object is
		 * created with the same name and returned.
		 */
		public NetMeterProperty getProperty(String key) 
		{
			NetMeterProperty prop;
			if(!propDic.TryGetValue(key, out prop))
			{
				prop = new NullProperty(key);
			}
			return prop;
		}

		public void Traverse(TestElementTraverser traverser) 
		{
			PropertyIterator iter = propertyIterator();
			traverser.startTestElement(this);
			while (iter.hasNext()) {
				TraverseProperty(traverser, iter.next());
			}
			traverser.endTestElement(this);
		}

		protected void TraverseProperty(TestElementTraverser traverser, NetMeterProperty value) 
		{
			traverser.startProperty(value);
			if (value is TestElementProperty) 
            {
				((TestElement) value.getObjectValue()).Traverse(traverser);
			} 
            else if (value is CollectionProperty) 
            {
				traverseCollection((CollectionProperty) value, traverser);
			} 
            else if (value is MapProperty) 
            {
				TraverseMap((MapProperty) value, traverser);
			}
			traverser.endProperty(value);
		}

		protected void TraverseMap(MapProperty map, TestElementTraverser traverser)
		{
			PropertyIterator iter = map.valueIterator();
			while (iter.hasNext()) 
            {
				TraverseProperty(traverser, iter.next());
			}
		}

		protected void traverseCollection(CollectionProperty col, TestElementTraverser traverser) 
		{
			PropertyIterator iter = col.iterator();
			while (iter.hasNext()) {
				TraverseProperty(traverser, iter.next());
			}
		}

		public int getPropertyAsInt(String key) 
		{
			return getProperty(key).getIntValue();
		}

		public int getPropertyAsInt(String key, int defaultValue) 
        {
			NetMeterProperty jmp = getProperty(key);
			return jmp is NullProperty ? defaultValue : jmp.getIntValue();
		}

		public bool getPropertyAsBoolean(String key) 
		{
			return getProperty(key).getBooleanValue();
		}

		public bool getPropertyAsBoolean(String key, bool defaultVal) 
        {
			NetMeterProperty jmp = getProperty(key);
			return jmp is NullProperty ? defaultVal : jmp.getBooleanValue();
		}

		public float getPropertyAsFloat(String key) 
        {
			return getProperty(key).getFloatValue();
		}

		public Int64 getPropertyAsLong(String key) 
        {
			return getProperty(key).getLongValue();
		}

		public Int64 getPropertyAsLong(String key, Int64 defaultValue) 
        {
			NetMeterProperty jmp = getProperty(key);
			return jmp is NullProperty ? defaultValue : jmp.getLongValue();
		}

		public double getPropertyAsDouble(String key) 
        {
			return getProperty(key).getDoubleValue();
		}

		public String getPropertyAsString(String key) 
        {
			return getProperty(key).getStringValue();
		}

		public String getPropertyAsString(String key, String defaultValue) 
        {
			NetMeterProperty jmp = getProperty(key);
			return jmp is NullProperty ? defaultValue : jmp.getStringValue();
		}

		/**
		 * Add property to test element
		 * @param property {@link JMeterProperty} to add to current Test Element
		 * @param clone clone property
		 */
		protected void addProperty(NetMeterProperty property, bool clone) {
			NetMeterProperty propertyToPut = property;
			if(clone) {
				propertyToPut = property.clone();
			}
			if (isRunningVersion()) {
				setTemporary(propertyToPut);
			} else {
				clearTemporary(property);
			}
			NetMeterProperty prop = getProperty(property.getName());

			if (prop is NullProperty || (prop is StringProperty && prop.getStringValue().Equals(""))) {
				propDic.Add(property.getName(), propertyToPut);
			} else {
				prop.mergeIn(propertyToPut);
			}
		}

		/**
		 * Add property to test element without cloning it
		 * @param property {@link JMeterProperty}
		 */
		protected void addProperty(NetMeterProperty property) 
        {
			addProperty(property, false);
		}

		/**
		 * Remove property from temporaryProperties
		 * @param property {@link JMeterProperty}
		 */
		protected void clearTemporary(NetMeterProperty property) 
        {
			if (temporaryProperties != null) 
            {
				temporaryProperties.Remove(property);
			}
		}

		/**
		 * Log the properties of the test element
		 *
		 * @see TestElement#setProperty(JMeterProperty)
		 */
		protected void logProperties()
 {
			if (log.IsDebugEnabled)
            {
				PropertyIterator iter = propertyIterator();
				while (iter.hasNext()) 
                {
					NetMeterProperty prop = iter.next();
					//log.debug("Property " + prop.getName() + " is temp? " + isTemporary(prop) + " and is a " + prop.getObjectValue());
				}
			}
		}

		public void setProperty(NetMeterProperty property) 
        {
			if (isRunningVersion()) {
				if (getProperty(property.getName()) is NullProperty)
                {
					addProperty(property);
				} 
                else 
                {
					getProperty(property.getName()).setObjectValue(property.getObjectValue());
				}
			} 
            else
            {
				propDic.Add(property.getName(), property);
			}
		}

		public void setProperty(String name, String value)
        {
			setProperty(new StringProperty(name, value));
		}

		/**
		 * Create a String property - but only if it is not the default.
		 * This is intended for use when adding new properties to JMeter
		 * so that JMX files are not expanded unnecessarily.
		 *
		 * N.B. - must agree with the default applied when reading the property.
		 *
		 * @param name property name
		 * @param value current value
		 * @param dflt default
		 */
		public void setProperty(String name, String value, String dflt) {
			if (dflt.Equals(value)) {
				RemoveProperty(name);
			} else {
				setProperty(new StringProperty(name, value));
			}
		}

		public void setProperty(String name, bool value) {
			setProperty(new BooleanProperty(name, value));
		}

		/**
		 * Create a boolean property - but only if it is not the default.
		 * This is intended for use when adding new properties to JMeter
		 * so that JMX files are not expanded unnecessarily.
		 *
		 * N.B. - must agree with the default applied when reading the property.
		 *
		 * @param name property name
		 * @param value current value
		 * @param dflt default
		 */
		public void setProperty(String name, bool value, bool dflt) {
			if (value == dflt) {
				RemoveProperty(name);
			} else {
				setProperty(new BooleanProperty(name, value));
			}
		}

		public void setProperty(String name, int value) {
			setProperty(new IntegerProperty(name, value));
		}

		/**
		 * Create a boolean property - but only if it is not the default.
		 * This is intended for use when adding new properties to JMeter
		 * so that JMX files are not expanded unnecessarily.
		 *
		 * N.B. - must agree with the default applied when reading the property.
		 *
		 * @param name property name
		 * @param value current value
		 * @param dflt default
		 */
		public void setProperty(String name, int value, int dflt) {
			if (value == dflt) {
				RemoveProperty(name);
			} else {
				setProperty(new IntegerProperty(name, value));
			}
		}

		public PropertyIterator propertyIterator() 
        {
			return new PropertyIteratorImpl(propMap.values());
		}

		/**
		 * Add to this the properties of element (by reference)
		 * @param element {@link TestElement}
		 */
		protected void mergeIn(TestElement element) {
			PropertyIterator iter = element.propertyIterator();
			while (iter.hasNext()) {
				NetMeterProperty prop = iter.next();
				addProperty(prop, false);
			}
		}

		/**
		 * Returns the runningVersion.
		 */
		public bool isRunningVersion() {
			return runningVersion;
		}

		/**
		 * Sets the runningVersion.
		 *
		 * @param runningVersion
		 *            the runningVersion to set
		 */
		public void SetRunningVersion(bool runningVersion) {
			this.runningVersion = runningVersion;
			PropertyIterator iter = propertyIterator();
			while (iter.hasNext()) {
				iter.next().setRunningVersion(runningVersion);
			}
		}

		/**
		 * {@inheritDoc}
		 */
		public void RecoverRunningVersion() 
        {
            foreach (String propPair in propDic.Keys)
            {
                NetMeterProperty prop;
                if (propDic.TryGetValue(propPair, out prop))
                {
                    if (isTemporary(prop))
                    {
                        propDic.Remove(propPair);
                        clearTemporary(prop);
                    }
                    else
                    {
                        prop.recoverRunningVersion(this);
                    }
                }
            }
			emptyTemporary();
		}

		/**
		 * Clears temporaryProperties
		 */
		protected void emptyTemporary() {
			if (temporaryProperties != null) {
				temporaryProperties.Clear();
			}
		}

		/**
		 * {@inheritDoc}
		 */
		public bool isTemporary(NetMeterProperty property) {
			if (temporaryProperties == null) {
				return false;
			} else {
				return temporaryProperties.Contains(property);
			}
		}

		/**
		 * {@inheritDoc}
		 */
		public void setTemporary(NetMeterProperty property) {
			if (temporaryProperties == null) {
				temporaryProperties = new HashSet<NetMeterProperty>();
			}
			temporaryProperties.Add(property);
			if (property is MultiProperty) 
            {
				PropertyIterator iter = ((MultiProperty) property).iterator();
				while (iter.hasNext()) {
					setTemporary(iter.next());
				}
			}
		}

		/**
		 * @return Returns the threadContext.
		 */
		public NetMeterContext getThreadContext() {
			if (threadContext == null) {
				/*
				 * Only samplers have the thread context set up by JMeterThread at
				 * present, so suppress the warning for now
				 */
				// log.warn("ThreadContext was not set up - should only happen in
				// JUnit testing..."
				// ,new Throwable("Debug"));
				threadContext = NetMeterContextManager.getContext();
			}
			return threadContext;
		}

		/**
		 * @param inthreadContext
		 *            The threadContext to set.
		 */
		public void SetThreadContext(NetMeterContext inthreadContext)
        {
			if (threadContext != null)
            {
				if (inthreadContext != threadContext) 
                {
					throw new Exception("Attempting to reset the thread context");
				}
			}
			this.threadContext = inthreadContext;
		}

		/**
		 * @return Returns the threadName.
		 */
		public String getThreadName() 
        {
			return threadName;
		}

		/**
		 * @param inthreadName
		 *            The threadName to set.
		 */
		public void SetThreadName(String inthreadName) 
        {
			if (threadName != null)
            {
				if (!threadName.Equals(inthreadName)) 
                {
					throw new Exception("Attempting to reset the thread name");
				}
			}
			this.threadName = inthreadName;
		}

		public AbstractTestElement() 
		{
			
		}

		/**
		 * {@inheritDoc}
		 */
		// Default implementation
		public bool canRemove() 
		{
			return true;
		}

		/**
		 * {@inheritDoc}
		 */
		// Moved from JMeter class
		public bool isEnabled() 
		{
			return getProperty(TestElement.ENABLED) is NullProperty || getPropertyAsBoolean(TestElement.ENABLED);
		}
	
		/** 
		 * {@inheritDoc}}
		 */
		public List<String> getSearchableTokens() 
		{
			List<String> result = new List<String>(25);
            foreach (String res in result)
            {

            }

			PropertyIterator iterator = propertyIterator();
			while(iterator.hasNext()) {
				NetMeterProperty jMeterProperty = iterator.next();    
				result.Add(jMeterProperty.getName());
				result.Add(jMeterProperty.getStringValue());
			}
			return result;
		}
	
		/**
		 * Add to result the values of propertyNames
		 * @param result List<String> values of propertyNames
		 * @param propertyNames Set<String> properties to extract
		 */
		protected sealed void addPropertiesValues(List<String> result, HashSet<String> propertyNames) 
        {
			PropertyIterator iterator = propertyIterator();
			while(iterator.hasNext()) {
				NetMeterProperty jMeterProperty = iterator.next();	
				if(propertyNames.Contains(jMeterProperty.getName()))
                {
					result.Add(jMeterProperty.getStringValue());
				}
			}
		} 

	}
}
