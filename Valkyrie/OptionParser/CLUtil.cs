using System.Text;
using System;

namespace Valkyrie.OptionParser
{
    public class CLUtil
    {
        private static Int32 MAX_DESCRIPTION_COLUMN_LENGTH = 60;

        /**
         * Private Constructor so that no instance can ever be created.
         *
         */
        private CLUtil() 
        {
        }

        /**
         * Format options into StringBuilder and return. This is typically used to
         * print "Usage" text in response to a "--help" or invalid option.
         *
         * @param options
         *            the option descriptors
         * @return the formatted description/help for options
         */
        public static StringBuilder DescribeOptions(CLOptionDescriptor[] options) 
        {
            String lSep = "\n\t";
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < options.Length; i++) 
            {
                char ch = (char) options[i].GetId();
                String name = options[i].GetName();
                String description = options[i].GetDescription();
                int flags = options[i].GetFlags();
                Boolean argumentOptional = ((flags & CLOptionDescriptor.ARGUMENT_OPTIONAL) == CLOptionDescriptor.ARGUMENT_OPTIONAL);
                Boolean argumentRequired = ((flags & CLOptionDescriptor.ARGUMENT_REQUIRED) == CLOptionDescriptor.ARGUMENT_REQUIRED);
                Boolean twoArgumentsRequired = ((flags & CLOptionDescriptor.ARGUMENTS_REQUIRED_2) == CLOptionDescriptor.ARGUMENTS_REQUIRED_2);
                Boolean needComma = false;
                if (twoArgumentsRequired)
                {
                    argumentRequired = true;
                }

                sb.Append('\t');

                if (Char.IsLetter(ch)) 
                {
                    sb.Append("-");
                    sb.Append(ch);
                    needComma = true;
                }

                if (null != name)
                {
                    if (needComma) 
                    {
                        sb.Append(", ");
                    }

                    sb.Append("--");
                    sb.Append(name);
                }

                if (argumentOptional)
                {
                    sb.Append(" [<argument>]");
                }
                if (argumentRequired) 
                {
                    sb.Append(" <argument>");
                }
                if (twoArgumentsRequired)
                {
                    sb.Append("=<value>");
                }
                sb.Append(lSep);

                if (null != description) 
                {
                    while (description.Length > MAX_DESCRIPTION_COLUMN_LENGTH)
                    {
                        String descriptionPart = description.Substring(0, MAX_DESCRIPTION_COLUMN_LENGTH);
                        description = description.Substring(MAX_DESCRIPTION_COLUMN_LENGTH);
                        sb.Append("\t\t");
                        sb.Append(descriptionPart);
                        sb.Append(lSep);
                    }

                    sb.Append("\t\t");
                    sb.Append(description);
                    sb.Append(lSep);
                }
            }
            return sb;
        }
    }
}
