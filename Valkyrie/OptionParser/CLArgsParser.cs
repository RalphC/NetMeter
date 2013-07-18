using System;
using System.Collections.Generic;
using System.Text;


namespace Valkyrie.OptionParser
{
    public class CLArgsParser
    {
        private static Int32 INVALID = Int32.MaxValue;

        private static Int32 STATE_NORMAL = 0;

        private static Int32 STATE_REQUIRE_2ARGS = 1;

        private static Int32 STATE_REQUIRE_ARG = 2;

        private static Int32 STATE_OPTIONAL_ARG = 3;

        private static Int32 STATE_NO_OPTIONS = 4;

        private static Int32 STATE_OPTION_MODE = 5;

        // Values for creating tokens
        private static Int32 TOKEN_SEPARATOR = 0;

        private static Int32 TOKEN_STRING = 1;

        private static Char[] ARG_SEPARATORS = new char[] { (char) 0, '=' };

        private static Char[] NULL_SEPARATORS = new char[] { (char) 0 };

        private CLOptionDescriptor[] m_optionDescriptors;

        private List<CLOption> m_options;

        // Key is String or Integer
        private Dictionary<Object, CLOption> m_optionIndex;

        private ParserControl m_control;

        private String m_errorMessage;

        private String[] m_unparsedArgs = new String[] {};

        // variables used while parsing options.
        private char m_ch;

        private String[] m_args;

        private Boolean m_isLong;

        private Int32 m_argIndex;

        private Int32 m_stringIndex;

        private Int32 m_stringLength;

        private Int32 m_lastChar = INVALID;

        private Int32 m_lastOptionId;

        private CLOption m_option;

        private Int32 m_state = STATE_NORMAL;

        /**
         * Retrieve an array of arguments that have not been parsed due to the
         * parser halting.
         *
         * @return an array of unparsed args
         */
        public String[] GetUnparsedArgs() 
        {
            return m_unparsedArgs;
        }

        /**
         * Retrieve a list of options that were parsed from command list.
         *
         * @return the list of options
         */
        public List<CLOption> GetArguments() 
        {
            // System.out.println( "Arguments: " + m_options );
            return m_options;
        }

        /**
         * Retrieve the {@link CLOption} with specified id, or <code>null</code>
         * if no command line option is found.
         *
         * @param id
         *            the command line option id
         * @return the {@link CLOption} with the specified id, or <code>null</code>
         *         if no CLOption is found.
         * @see CLOption
         */
        public CLOption GetArgumentById(int id) 
        {
            CLOption option = null;
            m_optionIndex.TryGetValue(id, out option);
            return option;
        }

        /**
         * Retrieve the {@link CLOption} with specified name, or <code>null</code>
         * if no command line option is found.
         *
         * @param name
         *            the command line option name
         * @return the {@link CLOption} with the specified name, or
         *         <code>null</code> if no CLOption is found.
         * @see CLOption
         */
        public CLOption GetArgumentByName(String name) 
        {
            CLOption option = null;
            m_optionIndex.TryGetValue(name, out option);
            return option;
        }

        /**
         * Get Descriptor for option id.
         *
         * @param id
         *            the id
         * @return the descriptor
         */
        private CLOptionDescriptor GetDescriptorFor(int id)
        {
            foreach (CLOptionDescriptor descrptor in m_optionDescriptors)
            {
                if (id == descrptor.GetId())
                {
                    return descrptor;
                }
            }
            return null;
        }

        /**
         * Retrieve a descriptor by name.
         *
         * @param name
         *            the name
         * @return the descriptor
         */
        private CLOptionDescriptor GetDescriptorFor(String name) 
        {
            foreach (CLOptionDescriptor descrptor in m_optionDescriptors)
            {
                if (descrptor.GetName().Equals(name))
                {
                    return descrptor;
                }
            }
            return null;
        }

        /**
         * Retrieve an error message that occured during parsing if one existed.
         *
         * @return the error string
         */
        public String GetErrorString()
        {
            // System.out.println( "ErrorString: " + m_errorMessage );
            return m_errorMessage;
        }

        /**
         * Require state to be placed in for option.
         *
         * @param descriptor
         *            the Option Descriptor
         * @return the state
         */
        private Int32 GetStateFor(CLOptionDescriptor descriptor) 
        {
            int flags = descriptor.GetFlags();
            if ((flags & CLOptionDescriptor.ARGUMENTS_REQUIRED_2) == CLOptionDescriptor.ARGUMENTS_REQUIRED_2) 
            {
                return STATE_REQUIRE_2ARGS;
            } else if ((flags & CLOptionDescriptor.ARGUMENT_REQUIRED) == CLOptionDescriptor.ARGUMENT_REQUIRED) 
            {
                return STATE_REQUIRE_ARG;
            } else if ((flags & CLOptionDescriptor.ARGUMENT_OPTIONAL) == CLOptionDescriptor.ARGUMENT_OPTIONAL) 
            {
                return STATE_OPTIONAL_ARG;
            }
            else 
            {
                return STATE_NORMAL;
            }
        }

        /**
         * Create a parser that can deal with options and parses certain args.
         *
         * @param args
         *            the args, typically that passed to the
         *            <code>public static void main(String[] args)</code> method.
         * @param optionDescriptors
         *            the option descriptors
         * @param control
         *            the parser control used determine behaviour of parser
         */
        public CLArgsParser(String[] args, CLOptionDescriptor[] optionDescriptors, ParserControl control) 
        {
            m_optionDescriptors = optionDescriptors;
            m_control = control;
            m_options = new List<CLOption>();
            m_args = args;

            try 
            {
                Parse();
                CheckIncompatibilities(m_options);
                BuildOptionIndex();
            } 
            catch (Exception pe)
            {
                m_errorMessage = pe.Message;
            }

            // System.out.println( "Built : " + m_options );
            // System.out.println( "From : " + Arrays.asList( args ) );
        }

        /**
         * Check for duplicates of an option. It is an error to have duplicates
         * unless appropriate flags is set in descriptor.
         *
         * @param arguments
         *            the arguments
         */
        private void CheckIncompatibilities(List<CLOption> arguments) 
        {
            int size = arguments.Count;

            for (int i = 0; i < size; i++) 
            {
                CLOption option = arguments[i];
                int id = option.GetDescriptor().GetId();
                CLOptionDescriptor descriptor = GetDescriptorFor(id);

                // this occurs when id == 0 and user has not supplied a descriptor
                // for arguments
                if (null == descriptor) {
                    continue;
                }

                int[] incompatible = descriptor.GetIncompatible();

                CheckIncompatible(arguments, incompatible, i);
            }
        }

        private void CheckIncompatible(List<CLOption> arguments, int[] incompatible, int original)
        {
            int size = arguments.Count;

            for (int i = 0; i < size; i++) 
            {
                if (original == i) 
                {
                    continue;
                }

                CLOption option = arguments[i];
                int id = option.GetDescriptor().GetId();

                for (int j = 0; j < incompatible.Length; j++)
                {
                    if (id == incompatible[j]) 
                    {
                        CLOption originalOption = arguments[original];
                        int originalId = originalOption.GetDescriptor().GetId();

                        String message = null;

                        if (id == originalId) 
                        {
                            message = "Duplicate options for " + DescribeDualOption(originalId) + " found.";
                        } 
                        else
                        {
                            message = "Incompatible options -" + DescribeDualOption(id) + " and "
                                    + DescribeDualOption(originalId) + " found.";
                        }
                        throw new Exception(message);
                    }
                }
            }
        }

        private String DescribeDualOption(Int32 id) 
        {
            CLOptionDescriptor descriptor = GetDescriptorFor(id);
            if (null == descriptor) 
            {
                return "<parameter>";
            }
            else 
            {
                StringBuilder sb = new StringBuilder();
                Boolean hasCharOption = false;

                if (Char.IsLetter((Char) id)) 
                {
                    sb.Append('-');
                    sb.Append((Char)id);
                    hasCharOption = true;
                }

                String longOption = descriptor.GetName();
                if (null != longOption) 
                {
                    if (hasCharOption) 
                    {
                        sb.Append('/');
                    }
                    sb.Append("--");
                    sb.Append(longOption);
                }

                return sb.ToString();
            }
        }

        /**
         * Create a parser that deals with options and parses certain args.
         *
         * @param args
         *            the args
         * @param optionDescriptors
         *            the option descriptors
         */
        public CLArgsParser(String[] args, CLOptionDescriptor[] optionDescriptors) 
            : this(args, optionDescriptors, null)
        {
        }

        /**
         * Create a string array that is subset of input array. The sub-array should
         * start at array entry indicated by index. That array element should only
         * include characters from charIndex onwards.
         *
         * @param array
         *            the original array
         * @param index
         *            the cut-point in array
         * @param charIndex
         *            the cut-point in element of array
         * @return the result array
         */
        private String[] SubArray(String[] array, int index, int charIndex) 
        {
            int remaining = array.Length - index;
            String[] result = new String[remaining];

            if (remaining > 1) 
            {
                Array.Copy(array, index + 1, result, 1, remaining - 1);
            }

            result[0] = array[index].Substring(charIndex - 1);

            return result;
        }

        /**
         * Actually parse arguments
         */
        private void Parse() 
        {
            if (0 == m_args.Length) 
            {
                return;
            }

            m_stringLength = m_args[m_argIndex].Length;

            while (true) 
            {
                m_ch = PeekAtChar();

                if (m_argIndex >= m_args.Length) 
                {
                    break;
                }

                if (null != m_control && m_control.isFinished(m_lastOptionId))
                {
                    // this may need mangling due to peeks
                    m_unparsedArgs = SubArray(m_args, m_argIndex, m_stringIndex);
                    return;
                }

                if (STATE_OPTION_MODE == m_state) 
                {
                    // if get to an arg barrier then return to normal mode
                    // else continue accumulating options
                    if (0 == m_ch) 
                    {
                        GetChar(); // strip the null
                        m_state = STATE_NORMAL;
                    } 
                    else 
                    {
                        ParseShortOption();
                    }
                }
                else if (STATE_NORMAL == m_state) 
                {
                    ParseNormal();
                } 
                else if (STATE_NO_OPTIONS == m_state)
                {
                    // should never get to here when stringIndex != 0
                    AddOption(new CLOption(m_args[m_argIndex++]));
                } 
                else
                {
                    ParseArguments();
                }
            }

            // Reached end of input arguments - perform final processing
            if (m_option != null) 
            {
                if (STATE_OPTIONAL_ARG == m_state)
                {
                    m_options.Add(m_option);
                } 
                else if (STATE_REQUIRE_ARG == m_state) 
                {
                    CLOptionDescriptor descriptor = GetDescriptorFor(m_option.GetDescriptor().GetId());
                    String message = "Missing argument to option " + GetOptionDescription(descriptor);
                    throw new Exception(message);
                } 
                else if (STATE_REQUIRE_2ARGS == m_state) 
                {
                    if (1 == m_option.GetArgumentCount()) 
                    {
                        m_option.AddArgument("");
                        m_options.Add(m_option);
                    } 
                    else 
                    {
                        CLOptionDescriptor descriptor = GetDescriptorFor(m_option.GetDescriptor().GetId());
                        String message = "Missing argument to option " + GetOptionDescription(descriptor);
                        throw new Exception(message);
                    }
                } 
                else 
                {
                    throw new Exception("IllegalState " + m_state + ": " + m_option);
                }
            }
        }

        private String GetOptionDescription(CLOptionDescriptor descriptor) 
        {
            if (m_isLong) 
            {
                return "--" + descriptor.GetName();
            } else {
                return "-" + (char) descriptor.GetId();
            }
        }

        private char PeekAtChar() 
        {
            if (INVALID == m_lastChar) 
            {
                m_lastChar = ReadChar();
            }
            return (char) m_lastChar;
        }

        private char GetChar() 
        {
            if (INVALID != m_lastChar) 
            {
                char result = (char) m_lastChar;
                m_lastChar = INVALID;
                return result;
            } 
            else 
            {
                return ReadChar();
            }
        }

        private char ReadChar()
        {
            if (m_stringIndex >= m_stringLength) 
            {
                m_argIndex++;
                m_stringIndex = 0;

                if (m_argIndex < m_args.Length) 
                {
                    m_stringLength = m_args[m_argIndex].Length;
                } else {
                    m_stringLength = 0;
                }

                return (Char)0;
            }

            if (m_argIndex >= m_args.Length) 
            {
                return (Char)0;
            }

            return m_args[m_argIndex].ToCharArray()[m_stringIndex++];
        }

        private char m_tokesep; // Keep track of token separator

        private Token NextToken(char[] separators) 
        {
            m_ch = GetChar();

            if (isSeparator(m_ch, separators)) 
            {
                m_tokesep=m_ch;
                m_ch = GetChar();
                return new Token(TOKEN_SEPARATOR, null);
            }

            StringBuilder sb = new StringBuilder();

            do
            {
                sb.Append(m_ch);
                m_ch = GetChar();
            }
            while (!isSeparator(m_ch, separators));

            m_tokesep=m_ch;
            return new Token(TOKEN_STRING, sb.ToString());
        }

        private Boolean isSeparator(char ch, char[] separators)
        {
            for (int i = 0; i < separators.Length; i++) 
            {
                if (ch == separators[i]) {
                    return true;
                }
            }

            return false;
        }

        private void AddOption(CLOption option) 
        {
            m_options.Add(option);
            m_lastOptionId = option.GetDescriptor().GetId();
            m_option = null;
        }

        private void ParseOption(CLOptionDescriptor descriptor, String optionString)
        {
            if (null == descriptor)
            {
                throw new Exception("Unknown option " + optionString);
            }

            m_state = GetStateFor(descriptor);
            m_option = new CLOption(descriptor);

            if (STATE_NORMAL == m_state) 
            {
                AddOption(m_option);
            }
        }

        private void ParseShortOption() 
        {
            m_ch = GetChar();
            CLOptionDescriptor descriptor = GetDescriptorFor(m_ch);
            m_isLong = false;
            ParseOption(descriptor, "-" + m_ch);

            if (STATE_NORMAL == m_state) {
                m_state = STATE_OPTION_MODE;
            }
        }

        private void ParseArguments() 
        {
            if (STATE_REQUIRE_ARG == m_state) 
            {
                if ('=' == m_ch || 0 == m_ch)
                {
                    GetChar();
                }

                Token token = NextToken(NULL_SEPARATORS);
                m_option.AddArgument(token.GetValue());

                AddOption(m_option);
                m_state = STATE_NORMAL;
            } 
            else if (STATE_OPTIONAL_ARG == m_state)
            {
                if ('-' == m_ch || 0 == m_ch) 
                {
                    GetChar(); // consume stray character
                    AddOption(m_option);
                    m_state = STATE_NORMAL;
                    return;
                }

                if (m_isLong && '=' != m_tokesep)
                { // Long optional arg must have = as separator
                    AddOption(m_option);
                    m_state = STATE_NORMAL;
                    return;
                }

                if ('=' == m_ch)
                {
                    GetChar();
                }

                Token token = NextToken(NULL_SEPARATORS);
                m_option.AddArgument(token.GetValue());

                AddOption(m_option);
                m_state = STATE_NORMAL;
            } 
            else if (STATE_REQUIRE_2ARGS == m_state)
            {
                if (0 == m_option.GetArgumentCount()) 
                {
                    /*
                     * Fix bug: -D arg1=arg2 was causing parse error; however
                     * --define arg1=arg2 is OK This seems to be because the parser
                     * skips the terminator for the long options, but was not doing
                     * so for the short options.
                     */
                    if (!m_isLong) 
                    {
                        if (0 == PeekAtChar()) 
                        {
                            GetChar();
                        }
                    }
                    Token token = NextToken(ARG_SEPARATORS);

                    if (TOKEN_SEPARATOR == token.GetType())
                    {
                        CLOptionDescriptor descriptor = GetDescriptorFor(m_option.GetDescriptor().GetId());
                        String message = "Unable to parse first argument for option "
                                + GetOptionDescription(descriptor);
                        throw new Exception(message);
                    } 
                    else 
                    {
                        m_option.AddArgument(token.GetValue());
                    }
                    // Are we about to start a new option?
                    if (0 == m_ch && '-' == PeekAtChar()) 
                    {
                        // Yes, so the second argument is missing
                        m_option.AddArgument("");
                        m_options.Add(m_option);
                        m_state = STATE_NORMAL;
                    }
                } 
                else // 2nd argument
                {
                    StringBuilder sb = new StringBuilder();

                    m_ch = GetChar();
                    while (!isSeparator(m_ch, NULL_SEPARATORS))
                    {
                        sb.Append(m_ch);
                        m_ch = GetChar();
                    }

                    String argument = sb.ToString();

                    // System.out.println( "Arguement:" + argument );

                    m_option.AddArgument(argument);
                    AddOption(m_option);
                    m_option = null;
                    m_state = STATE_NORMAL;
                }
            }
        }

        /**
         * Parse Options from Normal mode.
         */
        private void ParseNormal() 
        {
            if ('-' != m_ch)
            {
                // Parse the arguments that are not options
                String argument = NextToken(NULL_SEPARATORS).GetValue();
                AddOption(new CLOption(argument));
                m_state = STATE_NORMAL;
            }
            else 
            {
                GetChar(); // strip the -

                if (0 == PeekAtChar()) 
                {
                    throw new Exception("Malformed option -");
                } 
                else 
                {
                    m_ch = PeekAtChar();

                    // if it is a short option then parse it else ...
                    if ('-' != m_ch)
                    {
                        ParseShortOption();
                    }
                    else 
                    {
                        GetChar(); // strip the -
                        // -- sequence .. it can either mean a change of state
                        // to STATE_NO_OPTIONS or else a long option

                        if (0 == PeekAtChar()) 
                        {
                            GetChar();
                            m_state = STATE_NO_OPTIONS;
                        } 
                        else
                        {
                            // its a long option
                            String optionName = NextToken(ARG_SEPARATORS).GetValue();
                            CLOptionDescriptor descriptor = GetDescriptorFor(optionName);
                            m_isLong = true;
                            ParseOption(descriptor, "--" + optionName);
                        }
                    }
                }
            }
        }

        /**
         * Build the m_optionIndex lookup map for the parsed options.
         */
        private void BuildOptionIndex() 
        {
            m_optionIndex = new Dictionary<Object, CLOption>(m_options.Count * 2);

            foreach (CLOption option in m_options)
            {
                CLOptionDescriptor optionDescriptor = GetDescriptorFor(option.GetDescriptor().GetId());

                m_optionIndex.Add(option.GetDescriptor().GetId(), option);

                if (null != optionDescriptor && null != optionDescriptor.GetName())
                {
                    m_optionIndex.Add(optionDescriptor.GetName(), option);
                }
            }
        }
    }
}
