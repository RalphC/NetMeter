using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace NetMeter.TestElements.Property
{
    public interface NetMeterProperty : ICloneable, ISerializable, IComparable<NetMeterProperty>
    {
        bool isRunningVersion();

        String getName();

        void setName(String name);
        
        /**
         * Make the property a running version or turn it off as the running
         * version. A property that is made a running version will preserve the
         * current state in such a way that it is retrievable by a future call to
         * 'recoverRunningVersion()'. Additionally, a property that is a running
         * version will resolve all functions prior to returning it's property
         * value. A non-running version property will return functions as their
         * uncompiled string representation.
         *
         * @param runningVersion
         */
        void setRunningVersion(bool runningVersion);

        /**
         * Tell the property to revert to the state at the time
         * setRunningVersion(true) was called.
         */
        void recoverRunningVersion(TestElement owner);

        /**
         * Take the given property object and merge it's value with the current
         * property object. For most property types, this will simply be ignored.
         * But for collection properties and test element properties, more complex
         * behavior is required.
         *
         * @param prop
         */
        void mergeIn(NetMeterProperty prop);

        int getIntValue();

        long getLongValue();

        double getDoubleValue();

        float getFloatValue();

        bool getBooleanValue();

        String getStringValue();

        Object getObjectValue();

        void setObjectValue(Object value);

        NetMeterProperty clone();
    }
}
