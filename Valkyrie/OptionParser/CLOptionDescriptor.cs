using System;
using System.Text;

namespace Valkyrie.OptionParser
{
    public class CLOptionDescriptor
    {
        /** Flag to say that one argument is required */
        public static int ARGUMENT_REQUIRED = 1 << 1;

        /** Flag to say that the argument is optional */
        public static int ARGUMENT_OPTIONAL = 1 << 2;

        /** Flag to say this option does not take arguments */
        public static int ARGUMENT_DISALLOWED = 1 << 3;

        /** Flag to say this option requires 2 arguments */
        public static int ARGUMENTS_REQUIRED_2 = 1 << 4;

        /** Flag to say this option may be repeated on the command line */
        public static int DUPLICATES_ALLOWED = 1 << 5;

        private int m_id;

        private int m_flags;

        private String m_name;

        private String m_description;

        private int[] m_incompatible;

        /**
         * Constructor.
         *
         * @param name
         *            the name/long option
         * @param flags
         *            the flags
         * @param id
         *            the id/character option
         * @param description
         *            description of option usage
         */
        public CLOptionDescriptor(String name, int flags, int id, String description) 
        {
            checkFlags(flags);

            m_id = id;
            m_name = name;
            m_flags = flags;
            m_description = description;
            m_incompatible = ((flags & DUPLICATES_ALLOWED) != 0) ? new int[0] : new int[] { id };
        }


        /**
         * Constructor.
         *
         * @param name
         *            the name/long option
         * @param flags
         *            the flags
         * @param id
         *            the id/character option
         * @param description
         *            description of option usage
         * @param incompatible
         *            descriptors for incompatible options
         */
        public CLOptionDescriptor(String name, int flags, int id, String description, CLOptionDescriptor[] incompatible)
        {

            checkFlags(flags);

            m_id = id;
            m_name = name;
            m_flags = flags;
            m_description = description;

            m_incompatible = new int[incompatible.Length];
            for (int i = 0; i < incompatible.Length; i++) 
            {
                m_incompatible[i] = incompatible[i].GetId();
            }
        }

        private void checkFlags(int flags)
        {
            int modeCount = 0;
            if ((ARGUMENT_REQUIRED & flags) == ARGUMENT_REQUIRED)
            {
                modeCount++;
            }
            if ((ARGUMENT_OPTIONAL & flags) == ARGUMENT_OPTIONAL) 
            {
                modeCount++;
            }
            if ((ARGUMENT_DISALLOWED & flags) == ARGUMENT_DISALLOWED)
            {
                modeCount++;
            }
            if ((ARGUMENTS_REQUIRED_2 & flags) == ARGUMENTS_REQUIRED_2)
            {
                modeCount++;
            }

            if (0 == modeCount)
            {
                String message = "No mode specified for option " + this;
                throw new Exception(message);
            } 
            else if (1 != modeCount)
            {
                String message = "Multiple modes specified for option " + this;
                throw new Exception(message);
            }
        }

        /**
         * Get the array of incompatible option ids.
         *
         * @return the array of incompatible option ids
         */
        public int[] GetIncompatible()
        {
            return m_incompatible;
        }

        /**
         * Retrieve textual description.
         *
         * @return the description
         */
        public String GetDescription() 
        {
            return m_description;
        }

        /**
         * Retrieve flags about option. Flags include details such as whether it
         * allows parameters etc.
         *
         * @return the flags
         */
        public int GetFlags() 
        {
            return m_flags;
        }

        /**
         * Retrieve the id for option. The id is also the character if using single
         * character options.
         *
         * @return the id
         */
        public int GetId() 
        {
            return m_id;
        }

        /**
         * Retrieve name of option which is also text for long option.
         *
         * @return name/long option
         */
        public String GetName() 
        {
            return m_name;
        }

        /**
         * Convert to String.
         *
         * @return the converted value to string.
         */
        public override String ToString() 
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[OptionDescriptor ");
            sb.Append(m_name);
            sb.Append(", ");
            sb.Append(m_id);
            sb.Append(", ");
            sb.Append(m_flags);
            sb.Append(", ");
            sb.Append(m_description);
            sb.Append(" ]");
            return sb.ToString();
        }
    }
}
