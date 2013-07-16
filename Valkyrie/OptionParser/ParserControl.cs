using System;

namespace Valkyrie.OptionParser
{
    public interface ParserControl
    {
        /**
         * Called by the parser to determine whether it should stop after last
         * option parsed.
         *
         * @param lastOptionCode
         *            the code of last option parsed
         * @return return true to halt, false to continue parsing
         */
        Boolean isFinished(Int32 lastOptionCode);
    }
}
