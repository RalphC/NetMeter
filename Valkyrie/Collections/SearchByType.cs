using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valkyrie.Collections
{
    public class SearchByType<T> : HashTreeTraverser
    {
        private List<T> objectOfType = new List<T>();

        private Dictionary<Object, OrderedHashTree> subTree = new Dictionary<Object, OrderedHashTree>();

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
        public List<T> GetSearchResults()
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
        public HashTree GetSubTree(Object root) 
        {
            OrderedHashTree tree = null;
            subTree.TryGetValue(root, out tree);
            return tree;
        }

        public void AddNode(Object node, HashTree subTree) 
        {
            if (node.GetType() is T)
            {
                objectOfType.Add((T)node);
                OrderedHashTree tree = new OrderedHashTree(node);
                tree.Set(node, subTree);
                subTree.Add(node, tree);
            }
        }

        public void SubtractNode()
        {
        }

        public void ProcessPath()
        {
        }
    }
}
