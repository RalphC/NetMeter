using System;
using System.Text;

namespace Valkyrie.OptionParser
{
    public class Token
    {
        /** Type for a separator token */
        public static Int32 TOKEN_SEPARATOR = 0;

        /** Type for a text token */
        public static Int32 TOKEN_STRING = 1;

        private Int32 m_type;

        private String m_value;

        /**
         * New Token object with a type and value
         */
        public Token(Int32 type, String value) 
        {
            m_type = type;
            m_value = value;
        }

        /**
         * Get the value of the token
         */
        public String GetValue() 
        {
            return m_value;
        }

        /**
         * Get the type of the token
         */
        public new Int32 GetType()
        {
            return m_type;
        }

        /**
         * Convert to a string
         */
        public new String ToString() 
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_type);
            sb.Append(":");
            sb.Append(m_value);
            return sb.ToString();
        }
    }
}
