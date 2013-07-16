using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Valkyrie.Collections
{
    /**
     * This class is used to create a tree structure of objects. Each element in the
     * tree is also a key to the next node down in the tree. It provides many ways
     * to add objects and branches, as well as many ways to retrieve.
     * 
     * HashTree implements the Map interface for convenience reasons. The main
     * difference between a Map and a HashTree is that the HashTree organizes the
     * data into a recursive tree structure, and provides the means to manipulate
     * that structure.
     * 
     * Of special interest is the traverse(HashTreeTraverser) method, which
     * provides an expedient way to traverse any HashTree by implementing the
     * link HashTreeTraverser interface in order to perform some operation on the
     * tree, or to extract information from the tree.
     *
     */
    public class HashTree : Dictionary<Object, HashTree>, ISerializable, ICloneable
    {
        // Used for the RuntimeException to short-circuit the traversal
        private static String FOUND = "found"; // $NON-NLS-1$

        // N.B. The keys can be either JMeterTreeNode or TestElement
        protected Dictionary<Object, HashTree> Data;

        public new Int32 Count { get { return Data.Count; } }

#region "Constructor"

        public HashTree()
            : this(null, null)
        {
        }

        public HashTree(Dictionary<Object, HashTree> _map)
            : this(_map, null)
        {
        }

        public HashTree(Object key)
            : this(new Dictionary<Object, HashTree>(), key)
        {
        }

        public HashTree(Dictionary<Object, HashTree> _map, Object key)
        {
            if (_map != null)
            {
                Data = _map;
            }
            else
            {
                Data = new Dictionary<Object, HashTree>();
            }
            if (key != null)
            {
                Data.Add(key, new HashTree());
            }
        }

        public HashTree(List<Object> keys)
        {
            Data = new Dictionary<Object, HashTree>();
            foreach (Object obj in keys)
            {
                Data.Add(obj, new HashTree());
            }
        }

        public HashTree(Object[] keys)
        {
            Data = new Dictionary<Object, HashTree>();
            foreach (Object obj in keys)
            {
                Data.Add(obj, new HashTree());
            }
        }

        #endregion

#region "Override Function"

        public new void Add(Object key, HashTree value)
        {
            HashTree previous = null;
            Data.TryGetValue(key, out previous);
            Put(key, value);
        }

        public new Boolean ContainsKey(Object obj)
        {
            return Data.ContainsKey(obj);
        }

        public new Boolean ContainsValue(HashTree tree)
        {
            return Data.ContainsValue(tree);
        }

        public new void Clear()
        {
            Data.Clear();
        }

        public Object Clone()
        {
            HashTree newTree = new HashTree();
            CloneTree(newTree);
            return newTree;
        }

        public new Boolean Remove(Object key)
        {
            return Data.Remove(key);
        }

        public override Int32 GetHashCode()
        {
            return Data.GetHashCode() * 3;
        }

        public override Boolean Equals(Object obj)
        {
            if ((obj is HashTree))
            {
                HashTree oo = (HashTree)obj;
                if (oo.Count == this.Count)
                {
                    return Data.Equals(oo.Data);
                }
            }
            return false;
        }

        public override String ToString()
        {
            ConvertToString converter = new ConvertToString();
            try
            {
                Traverse(converter);
            }
            catch (Exception ex)
            { // Just in case
                converter.ReportError(ex);
            }
            return converter.ToString();
        }

#endregion

#region "Set Function"
        /**
         * Sets a key and it's value in the HashTree. It actually sets up a key, and
         * then creates a node for the key and sets the value to the new node, as a
         * key. Any previous nodes that existed under the given key are lost.
         *
         * @param key
         *            key to be set up
         * @param value
         *            value to be set up as a key in the secondary node
         */
        public void Set(Object key, Object value)
        {
            Data.Add(key, CreateNewTree(value));
        }

        /**
         * Sets a key into the current tree and assigns it a HashTree as its
         * subtree. Any previous entries under the given key are removed.
         *
         * @param key
         *            key to be set up
         * @param t
         *            HashTree that the key maps to
         */
        public void Set(Object key, HashTree t)
        {
            Data.Add(key, t);
        }

        /**
         * Sets a key and its values in the HashTree. It sets up a key in the
         * current node, and then creates a node for that key, and sets all the
         * values in the array as keys in the new node. Any keys previously held
         * under the given key are lost.
         *
         * @param key
         *            Key to be set up
         * @param values
         *            Array of objects to be added as keys in the secondary node
         */
        public void Set(Object key, Object[] values)
        {
            Data.Add(key, CreateNewTree(values.ToList()));
        }

        /**
         * Sets a key and its values in the HashTree. It sets up a key in the
         * current node, and then creates a node for that key, and set all the
         * values in the array as keys in the new node. Any keys previously held
         * under the given key are removed.
         *
         * @param key
         *            key to be set up
         * @param values
         *            Collection of objects to be added as keys in the secondary
         *            node
         */
        public void Set(Object key, List<Object> values)
        {
            Data.Add(key, CreateNewTree(values));
        }

        /**
         * Sets a series of keys into the HashTree. It sets up the first object in
         * the key array as a key in the current node, recurses into the next
         * HashTree node through that key and adds the second object in the array.
         * Continues recursing in this manner until the end of the first array is
         * reached, at which point all the values of the second array are set as
         * keys to the bottom-most node. All previous keys of that bottom-most node
         * are removed.
         *
         * @param treePath
         *            array of keys to put into HashTree
         * @param values
         *            array of values to be added as keys to bottom-most node
         */
        public void Set(Object[] treePath, Object[] values)
        {
            if (treePath != null && values != null)
            {
                Set(treePath.ToList(), values.ToList());
            }
        }

        /**
         * Sets a series of keys into the HashTree. It sets up the first object in
         * the key array as a key in the current node, recurses into the next
         * HashTree node through that key and adds the second object in the array.
         * Continues recursing in this manner until the end of the first array is
         * reached, at which point all the values of the Collection of values are
         * set as keys to the bottom-most node. Any keys previously held by the
         * bottom-most node are lost.
         *
         * @param treePath
         *            array of keys to put into HashTree
         * @param values
         *            Collection of values to be added as keys to bottom-most node
         */
        public void Set(Object[] treePath, List<Object> values)
        {
            if (treePath != null)
            {
                Set(treePath.ToList(), values);
            }
        }

        /**
         * Sets a series of keys into the HashTree. It sets up the first object in
         * the key list as a key in the current node, recurses into the next
         * HashTree node through that key and adds the second object in the list.
         * Continues recursing in this manner until the end of the first list is
         * reached, at which point all the values of the array of values are set as
         * keys to the bottom-most node. Any previously existing keys of that bottom
         * node are removed.
         *
         * @param treePath
         *            collection of keys to put into HashTree
         * @param values
         *            array of values to be added as keys to bottom-most node
         */
        public void Set(List<Object> treePath, Object[] values)
        {
            HashTree tree = AddTreePath(treePath);
            tree.Set(values.ToList());
        }

        /**
         * Sets the nodes of the current tree to be the objects of the given
         * collection. Any nodes previously in the tree are removed.
         *
         * @param values
         *            Collection of objects to set as nodes.
         */
        public void Set(List<Object> values)
        {
            Clear();
            this.Put(values);
        }

        /**
         * Sets a series of keys into the HashTree. It sets up the first object in
         * the key list as a key in the current node, recurses into the next
         * HashTree node through that key and adds the second object in the list.
         * Continues recursing in this manner until the end of the first list is
         * reached, at which point all the values of the Collection of values are
         * set as keys to the bottom-most node. Any previously existing keys of that
         * bottom node are lost.
         *
         * @param treePath
         *            list of keys to put into HashTree
         * @param values
         *            collection of values to be added as keys to bottom-most node
         */
        public void Set(List<Object> treePath, List<Object> values)
        {
            HashTree tree = AddTreePath(treePath);
            tree.Set(values);
        }

#endregion

#region "Put Function"
        /**
         * Adds a key as a node at the current level and then adds the given
         * HashTree to that new node.
         *
         * @param key
         *            key to create in this tree
         * @param subTree
         *            sub tree to add to the node created for the first argument.
         */
        public void Put(Object key, HashTree subTree)
        {
            Put(key);
            GetTree(key).Put(subTree);
        }

        public void Put(HashTree newTree)
        {
            foreach (Object key in newTree.list())
            {
                if (key != null)
                {
                    Put(key);
                    GetTree(key).Put(newTree.GetTree(key));
                }
            }
        }

        /**
         * Adds an key into the HashTree at the current level.
         *
         * @param key
         *            key to be added to HashTree
         */
        public HashTree Put(Object key)
        {
            if (!Data.ContainsKey(key))
            {
                HashTree newTree = CreateNewTree();
                Data.Add(key, newTree);
                return newTree;
            }
            return GetTree(key);
        }

        /**
         * Adds all the given objects as nodes at the current level.
         *
         * @param keys
         *            Array of Keys to be added to HashTree.
         */
        public void Put(Object[] keys)
        {
            foreach (Object key in keys)
            {
                Put(key);
            }
        }

        /**
         * Adds a bunch of keys into the HashTree at the current level.
         *
         * @param keys
         *            Collection of Keys to be added to HashTree.
         */
        public void Put(List<Object> keys)
        {
            foreach (Object obj in keys)
            {
                Put(obj);
            }
        }

        /**
         * Adds a key and it's value in the HashTree. The first argument becomes a
         * node at the current level, and the second argument becomes a node of it.
         *
         * @param key
         *            key to be added
         * @param value
         *            value to be added as a key in the secondary node
         */
        public HashTree Put(Object key, Object value)
        {
            Put(key);
            return GetTree(key).Put(value);
        }

        /**
         * Adds a key and it's values in the HashTree. The first argument becomes a
         * node at the current level, and adds all the values in the array to the
         * new node.
         *
         * @param key
         *            key to be added
         * @param values
         *            array of objects to be added as keys in the secondary node
         */
        public void Put(Object key, Object[] values)
        {
            Put(key);
            GetTree(key).Put(values);
        }

        /**
         * Adds a key as a node at the current level and then adds all the objects
         * in the second argument as nodes of the new node.
         *
         * @param key
         *            key to be added
         * @param values
         *            Collection of objects to be added as keys in the secondary
         *            node
         */
        public void Put(Object key, List<Object> values)
        {
            Put(key);
            GetTree(key).Put(values);
        }

        /**
         * Adds a series of nodes into the HashTree using the given path. The first
         * argument is an array that represents a path to a specific node in the
         * tree. If the path doesn't already exist, it is created (the objects are
         * added along the way). At the path, all the objects in the second argument
         * are added as nodes.
         *
         * @param treePath
         *            an array of objects representing a path
         * @param values
         *            array of values to be added as keys to bottom-most node
         */
        public void Put(Object[] treePath, Object[] values)
        {
            if (treePath != null)
            {
                Put(treePath.ToList(), values.ToList());
            }
        }

        /**
         * Adds a series of nodes into the HashTree using the given path. The first
         * argument is an array that represents a path to a specific node in the
         * tree. If the path doesn't already exist, it is created (the objects are
         * added along the way). At the path, all the objects in the second argument
         * are added as nodes.
         *
         * @param treePath
         *            an array of objects representing a path
         * @param values
         *            collection of values to be added as keys to bottom-most node
         */
        public void Put(Object[] treePath, List<Object> values)
        {
            if (treePath != null)
            {
                Put(treePath.ToList(), values);
            }
        }

        public HashTree Put(Object[] treePath, Object value)
        {
            return Put(treePath.ToList(), value);
        }

        /**
         * Adds a series of nodes into the HashTree using the given path. The first
         * argument is a List that represents a path to a specific node in the tree.
         * If the path doesn't already exist, it is created (the objects are added
         * along the way). At the path, all the objects in the second argument are
         * added as nodes.
         *
         * @param treePath
         *            a list of objects representing a path
         * @param values
         *            array of values to be added as keys to bottom-most node
         */
        public void Put(List<Object> treePath, Object[] values)
        {
            HashTree tree = AddTreePath(treePath);
            tree.Put(values);
        }

        /**
         * Adds a series of nodes into the HashTree using the given path. The first
         * argument is a List that represents a path to a specific node in the tree.
         * If the path doesn't already exist, it is created (the objects are added
         * along the way). At the path, the object in the second argument is added
         * as a node.
         *
         * @param treePath
         *            a list of objects representing a path
         * @param value
         *            Object to add as a node to bottom-most node
         */
        public HashTree Put(List<Object> treePath, Object value)
        {
            HashTree tree = AddTreePath(treePath);
            return tree.Put(value);
        }

        /**
         * Adds a series of nodes into the HashTree using the given path. The first
         * argument is a SortedSet that represents a path to a specific node in the
         * tree. If the path doesn't already exist, it is created (the objects are
         * added along the way). At the path, all the objects in the second argument
         * are added as nodes.
         *
         * @param treePath
         *            a SortedSet of objects representing a path
         * @param values
         *            Collection of values to be added as keys to bottom-most node
         */
        public void Put(List<Object> treePath, List<Object> values)
        {
            HashTree tree = AddTreePath(treePath);
            tree.Put(values);
        }

#endregion

#region "GetArray Function"

        /**
         * Gets an array of all keys in the current HashTree node. If the HashTree
         * represented a file system, this would be like getting an array of all the
         * files in the current folder.
         *
         * @return array of all keys in this HashTree.
         */
        public Object[] GetArray() 
        {
            return Data.Keys.ToArray();
        }

        /**
         * Gets an array of all keys in the HashTree mapped to the given key of the
         * current HashTree object (in other words, one level down). If the HashTree
         * represented a file system, this would like getting a list of all files in
         * a sub-directory (of the current directory) specified by the key argument.
         *
         * @param key
         *            key used to find HashTree to get list of
         * @return array of all keys in found HashTree
         */
        public Object[] GetArray(Object key) 
        {
            HashTree t = GetTree(key);
            if (t != null)
            {
                return t.GetArray();
            }
            return null;
        }

        /**
         * Recurses down into the HashTree stucture using each subsequent key in the
         * array of keys, and returns an array of keys of the HashTree object at the
         * end of the recursion. If the HashTree represented a file system, this
         * would be like getting a list of all the files in a directory specified by
         * the treePath, relative from the current directory.
         *
         * @param treePath
         *            array of keys used to recurse into HashTree structure
         * @return array of all keys found in end HashTree
         */
        public Object[] GetArray(Object[] treePath) 
        {
            if (treePath != null) 
            {
                return GetArray(treePath.ToList());
            }
            return GetArray();
        }

        /**
         * Recurses down into the HashTree stucture using each subsequent key in the
         * treePath argument, and returns an array of keys of the HashTree object at
         * the end of the recursion. If the HashTree represented a file system, this
         * would be like getting a list of all the files in a directory specified by
         * the treePath, relative from the current directory.
         *
         * @param treePath
         *            list of keys used to recurse into HashTree structure
         * @return array of all keys found in end HashTree
         */
        public Object[] GetArray(List<Object> treePath) 
        {
            HashTree tree = GetTreePath(treePath);
            return (tree != null) ? tree.GetArray() : null;
        }

#endregion

        /**
         * If the HashTree is empty, true is returned, false otherwise.
         *
         * @return True if HashTree is empty, false otherwise.
         */
        public Boolean isEmpty()
        {
            return (0 == Data.Count);
        }

        /**
         * The Map given must also be a HashTree, otherwise an
         * UnsupportedOperationException is thrown. If it is a HashTree, this is
         * like calling the add(HashTree) method.
         *
         * Originally this is an method of Map<> in java
         */
        public void AddAll(Dictionary<Object, HashTree> map)
        {
            if (typeof(HashTree).IsAssignableFrom(map.GetType()))
            {
                this.Put((HashTree)map);
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        protected void CloneTree(HashTree newTree)
        {
            foreach (Object key in list())
            {
                newTree.Set(key, (HashTree)GetTree(key).Clone());
            }
        }

        protected HashTree AddTreePath(List<Object> treePath)
        {
            HashTree tree = this;
            foreach (Object path in treePath)
            {
                tree.Put(path);
                tree = tree.GetTree(path);
            }
            return tree;
        }

        /**
         * Creates a new tree. This method exists to allow inheriting classes to
         * generate the appropriate types of nodes. For instance, when a node is
         * added, it's value is a HashTree. Rather than directly calling the
         * HashTree() constructor, the createNewTree() method is called. Inheriting
         * classes should override these methods and create the appropriate subclass
         * of HashTree.
         *
         * @return HashTree
         */
        protected HashTree CreateNewTree()
        {
            return new HashTree();
        }

        /**
         * Creates a new tree. This method exists to allow inheriting classes to
         * generate the appropriate types of nodes. For instance, when a node is
         * added, it's value is a HashTree. Rather than directly calling the
         * HashTree() constructor, the createNewTree() method is called. Inheriting
         * classes should override these methods and create the appropriate subclass
         * of HashTree.
         *
         * @return HashTree
         */
        protected HashTree CreateNewTree(Object key)
        {
            return new HashTree(key);
        }

        /**
         * Creates a new tree. This method exists to allow inheriting classes to
         * generate the appropriate types of nodes. For instance, when a node is
         * added, it's value is a HashTree. Rather than directly calling the
         * HashTree() constructor, the createNewTree() method is called. Inheriting
         * classes should override these methods and create the appropriate subclass
         * of HashTree.
         *
         * @return HashTree
         */
        protected HashTree CreateNewTree(List<Object> values)
        {
            return new HashTree(values);
        }

        /**
         * Gets the HashTree mapped to the given key.
         *
         * @param key
         *            Key used to find appropriate HashTree()
         */
        public HashTree GetTree(Object key)
        {
            HashTree tree = null;
            if (Data.TryGetValue(key, out tree))
            {
                return tree;
            }
            return null;
        }

        /**
         * Gets the HashTree object mapped to the last key in the array by recursing
         * through the HashTree structure one key at a time.
         *
         * @param treePath
         *            array of keys.
         * @return HashTree at the end of the recursion.
         */
        public HashTree GetTree(Object[] treePath)
        {
            if (treePath != null)
            {
                return GetTree(treePath.ToList());
            }
            return this;
        }

        /**
         * Gets the HashTree object mapped to the last key in the SortedSet by
         * recursing through the HashTree structure one key at a time.
         *
         * @param treePath
         *            Collection of keys
         * @return HashTree at the end of the recursion
         */
        public HashTree GetTree(List<Object> treePath) 
        {
            return GetTreePath(treePath);
        }

        protected HashTree GetTreePath(List<Object> treePath)
        {
            HashTree tree = this;
            foreach (var path in treePath)
            {
                if (null == tree)
                {
                    return null;
                }
                else
                {
                    tree = tree.GetTree(path);
                }
            }
            return tree;
        }


        /**
         * Gets a Collection of all keys in the current HashTree node. If the
         * HashTree represented a file system, this would be like getting a
         * collection of all the files in the current folder.
         *
         * @return Set of all keys in this HashTree
         */
        public List<Object> list()
        {
            return Data.Keys.ToList();
        }


        /**
         * Gets a Set of all keys in the HashTree mapped to the given key of the
         * current HashTree object (in other words, one level down. If the HashTree
         * represented a file system, this would like getting a list of all files in
         * a sub-directory (of the current directory) specified by the key argument.
         *
         * @param key
         *            key used to find HashTree to get list of
         * @return Set of all keys in found HashTree.
         */
        public List<Object> list(Object key)
        {
            HashTree temp = null;
            if (Data.TryGetValue(key, out temp))
            {
                return temp.list();
            }
            return new HashTree().list();
        }

        /**
         * Recurses down into the HashTree stucture using each subsequent key in the
         * array of keys, and returns the Set of keys of the HashTree object at the
         * end of the recursion. If the HashTree represented a file system, this
         * would be like getting a list of all the files in a directory specified by
         * the treePath, relative from the current directory.
         *
         * @param treePath
         *            Array of keys used to recurse into HashTree structure
         * @return Set of all keys found in end HashTree
         */
        public List<Object> list(Object[] treePath)
        { // TODO not used?
            if (treePath != null)
            {
                return list(treePath.ToList());
            }
            return list();
        }

        /**
         * Recurses down into the HashTree stucture using each subsequent key in the
         * List of keys, and returns the Set of keys of the HashTree object at the
         * end of the recursion. If the HashTree represented a file system, this
         * would be like getting a list of all the files in a directory specified by
         * the treePath, relative from the current directory.
         *
         * @param treePath
         *            List of keys used to recurse into HashTree structure
         * @return Set of all keys found in end HashTree
         */
        public List<Object> list(List<Object> treePath)
        {
            HashTree tree = GetTreePath(treePath);
            if (tree != null)
            {
                return tree.list();
            }
            return new HashTree().list();
        }


        /**
         * Finds the given current key, and replaces it with the given new key. Any
         * tree structure found under the original key is moved to the new key.
         */
        public void Replace(Object currentKey, Object newKey)
        {
            HashTree tree = GetTree(currentKey);
            Data.Remove(currentKey);
            Data.Add(newKey, tree);
        }


        /**
         * Searches the HashTree structure for the given key. If it finds the key,
         * it returns the HashTree mapped to the key. If it finds nothing, it
         * returns null.
         *
         * @param key
         *            Key to search for
         * @return HashTree mapped to key, if found, otherwise <code>null</code>
         */
        public HashTree Search(Object key)
        {// TODO does not appear to be used
            HashTree result = GetTree(key);
            if (result != null)
            {
                return result;
            }
            TreeSearcher searcher = new TreeSearcher(key);
            try
            {
                Traverse(searcher);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Equals(FOUND))
                {
                    throw ex;
                }
                // do nothing - means object is found
            }
            return searcher.GetResult();
        }

        /**
         * Allows any implementation of the HashTreeTraverser interface to easily
         * traverse (depth-first) all the nodes of the HashTree. The Traverser
         * implementation will be given notification of each node visited.
         *
         * @see HashTreeTraverser
         */
        public void Traverse(HashTreeTraverser visitor)
        {
            foreach (Object obj in list())
            {
                visitor.AddNode(obj, GetTree(obj));
                GetTree(obj).TraverseInto(visitor);
            }
        }

        /**
         * The recursive method that accomplishes the tree-traversal and performs
         * the callbacks to the HashTreeTraverser.
         */
        private void TraverseInto(HashTreeTraverser visitor)
        {
            if (list().Count == 0)
            {
                visitor.ProcessPath();
            }
            else
            {
                foreach (Object item in list())
                {
                    HashTree treeItem = GetTree(item);
                    visitor.AddNode(item, treeItem);
                    treeItem.TraverseInto(visitor);
                }
            }
            visitor.SubtractNode();
        }


#region "TreeSearcher"

        private class TreeSearcher : HashTreeTraverser 
        {

            private Object target;

            private HashTree result;

            public TreeSearcher(Object t) 
            {
                target = t;
            }

            public HashTree GetResult() 
            {
                return result;
            }

            public void AddNode(Object node, HashTree subTree)
            {
                result = subTree.GetTree(target);
                if (result != null) 
                {
                    // short circuit traversal when found
                    // throw new RuntimeException(FOUND);
                }
            }

            public void ProcessPath() 
            {
                // Not used
            }

            public void SubtractNode() 
            {
                // Not used
            }
        }

#endregion

#region  "ConvertToString"

        private class ConvertToString : HashTreeTraverser
        {
            private StringBuilder strBuiler = new StringBuilder();

            private StringBuilder spaces = new StringBuilder();

            private Int32 depth = 0;

            public void AddNode(Object key, HashTree subTree)
            {
                depth++;
                strBuiler.Append("\n").Append(GetSpaces()).Append(key);
                strBuiler.Append(" {");
            }

            public void SubtractNode()
            {
                strBuiler.Append("\n" + GetSpaces() + "}");
                depth--;
            }

            public void ProcessPath()
            {
            }

            public override String ToString()
            {
                strBuiler.Append("\n}");
                return strBuiler.ToString();
            }

            public void ReportError(Exception ex)
            {
                strBuiler.Append("Error: ").Append(ex.Message);
            }

            private String GetSpaces()
            {
                if (spaces.Length < depth * 2)
                {
                    while (spaces.Length < depth * 2)
                    {
                        spaces.Append("  ");
                    }
                }
                else if (spaces.Length > depth * 2)
                {
                    spaces.Length = depth * 2;
                }
                return spaces.ToString();
            }
        }

#endregion

    }
}
