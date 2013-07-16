using System;
using System.Text;
using System.Linq;


namespace Valkyrie.OptionParser
{
    public class CLOption
    {
        /**
         * Value of {@link CLOptionDescriptor#getId} when the option is a text argument.
         */
        public static Int32 TEXT_ARGUMENT = 0;

        /**
         * Default descriptor. Required, since code assumes that getDescriptor will
         * never return null.
         */
        private static CLOptionDescriptor TEXT_ARGUMENT_DESCRIPTOR = new CLOptionDescriptor(null,
                CLOptionDescriptor.ARGUMENT_OPTIONAL, TEXT_ARGUMENT, null);

        private String[] m_arguments;

        private CLOptionDescriptor m_descriptor = TEXT_ARGUMENT_DESCRIPTOR;

        /**
         * Retrieve argument to option if it takes arguments.
         *
         * @return the (first) argument
         */
        public String getArgument() 
        {
            return getArgument(0);
        }

        /**
         * Retrieve indexed argument to option if it takes arguments.
         *
         * @param index
         *            The argument index, from 0 to {@link #getArgumentCount()}-1.
         * @return the argument
         */
        public String getArgument(int index) 
        {
            if (null == m_arguments || index < 0 || index >= m_arguments.Length) 
            {
                return null;
            } else {
                return m_arguments[index];
            }
        }

        public CLOptionDescriptor GetDescriptor() 
        {
            return m_descriptor;
        }

        /**
         * Constructor taking an descriptor
         *
         * @param descriptor
         *            the descriptor iff null, will default to a "text argument"
         *            descriptor.
         */
        public CLOption(CLOptionDescriptor descriptor) 
        {
            if (descriptor != null)
            {
                m_descriptor = descriptor;
            }
        }

        /**
         * Constructor taking argument for option.
         *
         * @param argument
         *            the argument
         */
        public CLOption(String argument)
            : this((CLOptionDescriptor)null)
        {
            AddArgument(argument);
        }

        /**
         * Mutator of Argument property.
         *
         * @param argument
         *            the argument
         */
        public void AddArgument(String argument) 
        {
            if (null == m_arguments)
            {
                m_arguments = new String[] { argument };
            } 
            else 
            {
                String[] arguments = new String[m_arguments.Length + 1];
                Array.Copy(m_arguments, 0, arguments, 0, m_arguments.Length);
                arguments[m_arguments.Length] = argument;
                m_arguments = arguments;
            }
        }

        /**
         * Get number of arguments.
         *
         * @return the number of arguments
         */
        public int GetArgumentCount() 
        {
            if (null == m_arguments)
            {
                return 0;
            } 
            else 
            {
                return m_arguments.Length;
            }
        }

        /**
         * Convert to String.
         *
         * @return the string value
         */
        public override String ToString() 
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            char id = (char) m_descriptor.GetId();
            if (id == TEXT_ARGUMENT) 
            {
                sb.Append("TEXT ");
            } 
            else 
            {
                sb.Append("Option ");
                sb.Append(id);
            }

            if (null != m_arguments) 
            {
                sb.Append(", ");
                sb.Append(m_arguments.ToArray());
            }

            sb.Append(" ]");

            return sb.ToString();
        }

        /*
         * Convert to a shorter String for test purposes
         *
         * @return the string value
         */
        public String ToShortString() 
        {
            StringBuilder sb = new StringBuilder();
            char id = (char) m_descriptor.GetId();
            if (id != TEXT_ARGUMENT) {
                sb.Append("-");
                sb.Append(id);
            }

            if (null != m_arguments) {
                if (id != TEXT_ARGUMENT) {
                    sb.Append("=");
                }
                sb.Append(m_arguments.ToArray());
            }
            return sb.ToString();
        }
    }
}
