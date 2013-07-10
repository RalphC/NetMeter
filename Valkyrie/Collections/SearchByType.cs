using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valkyrie.Collections
{
    public class SearchByType<T> : HashTreeTraverser
    {
        private List<T> objectOfType = new List<T>();

        private Dictionary<Object, ListedHashTree> subTree = new Dictionary<Object, ListedHashTree>();

        //private sealed T searchType;

        /**
         * Creates an instance of SearchByClass, and sets the Class to be searched
         * for.
         *
         * @param searchClass
         */
        //public SearchByType() 
        //{
        //    searchType = type;
        //}

        /**
         * After traversing the HashTree, call this method to get a collection of
         * the nodes that were found.
         *
         * @return Collection All found nodes of the requested type
         */
        public List<T> getSearchResults()
        { // TODO specify collection type without breaking callers
            return objectOfType;
        }

        /**
         * Given a specific found node, this method will return the sub tree of that
         * node.
         *
         * @param root
         *            the node for which the sub tree is requested
         * @return HashTree
         */
        public HashTree getSubTree(Object root) 
        {
            ListedHashTree tree = null;
            subTree.TryGetValue(root, out tree);
            return tree;
        }

        public void addNode(Object node, HashTree subTree) 
        {
            if (node.GetType() is T)
            {
                objectOfType.Add((T)node);
                ListedHashTree tree = new ListedHashTree(node);
                tree.Set(node, subTree);
                subTree.Add(node, tree);
            }
        }

        public void subtractNode()
        {
        }

        public void processPath()
        {
        }
    }
}
