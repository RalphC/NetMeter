using System;

namespace Valkyrie.Collections
{
    public interface HashTreeTraverser
    {   
        /**
         * The tree traverses itself depth-first, calling addNode for each object it
         * encounters as it goes. This is a callback method, and should not be
         * called except by a HashTree during traversal.
         *
         * @param node
         *            the node currently encountered
         * @param subTree
         *            the HashTree under the node encountered
         */
        void addNode(Object node, HashTree subTree);

        /**
         * Indicates traversal has moved up a step, and the visitor should remove
         * the top node from its stack structure. This is a callback method, and
         * should not be called except by a HashTree during traversal.
         */
        void subtractNode();

        /**
         * Process path is called when a leaf is reached. If a visitor wishes to
         * generate Lists of path elements to each leaf, it should keep a Stack data
         * structure of nodes passed to it with addNode, and removing top items for
         * every {@link #subtractNode()} call. This is a callback method, and should
         * not be called except by a HashTree during traversal.
         */
        void processPath();
    }
}
