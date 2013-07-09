using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Valkyrie.Collections
{
    public class ListedHashTree : HashTree, ISerializable, ICloneable
    {
        
        private sealed List<Object> order;

        public ListedHashTree() 
        {
            //super();
            order = new List<Object>();
        }

        public ListedHashTree(Object key) : this()
        {
            Data.Add(key, new ListedHashTree());
            order.Add(key);
        }

        public ListedHashTree(List<Object> keys) : this()
        {
            foreach (Object key in keys)
            {
                Data.Add(key, new ListedHashTree());
                order.Add(key);
            }
        }

        public ListedHashTree(Object[] keys) : this()
        {
            foreach (Object key in keys)
            {
                Data.Add(key, new ListedHashTree());
                order.Add(key);
            }
        }

        public override Object Clone() 
        {
            ListedHashTree newTree = new ListedHashTree();
            cloneTree(newTree);
            return newTree;
        }

        public override void Set(Object key, Object value) 
        {
            if (!Data.ContainsKey(key)) 
            {
                order.Add(key);
            }
            super.set(key, value);
        }

        /** {@inheritDoc} */
        @Override
        public void set(Object key, HashTree t) {
            if (!data.containsKey(key)) {
                order.add(key);
            }
            super.set(key, t);
        }

        /** {@inheritDoc} */
        @Override
        public void set(Object key, Object[] values) {
            if (!data.containsKey(key)) {
                order.add(key);
            }
            super.set(key, values);
        }

        public override void Set(Object key, List<Object> values) 
        {
            if (!data.containsKey(key)) 
            {
                order.add(key);
            }
            super.set(key, values);
        }

        public override void replace(Object currentKey, Object newKey) 
        {
            HashTree tree = getTree(currentKey);
            Data.Remove(currentKey);
            Data.Add(newKey, tree);
            // find order.indexOf(currentKey) using == rather than equals()
            // there may be multiple entries which compare equals (Bug 50898)
            // This will be slightly slower than the built-in method,
            // but replace() is not used frequently.
            int entry = -1;
            for (int i=0; i < order.Count; i++) 
            {
                Object ent = order.(i);
                if (ent == currentKey) {
                    entry = i;
                    break;
                }
            }
            if (entry == -1) {
                throw new JMeterError("Impossible state, data key not present in order: "+currentKey.GetType());
            }
            order.Set(entry, newKey);
        }

        public override HashTree createNewTree() 
        {
            return new ListedHashTree();
        }

        public override HashTree createNewTree(Object key)
        {
            return new ListedHashTree(key);
        }

        public override HashTree createNewTree(List<Object> values)
        {
            return new ListedHashTree(values);
        }


        public override HashTree Put(Object key) 
        {
            if (!data.containsKey(key)) {
                HashTree newTree = createNewTree();
                data.put(key, newTree);
                order.add(key);
                return newTree;
            }
            return getTree(key);
        }

        /** {@inheritDoc} */
        @Override
        public Collection<Object> list() {
            return order;
        }

        /** {@inheritDoc} */
        @Override
        public HashTree remove(Object key) {
            order.remove(key);
            return data.remove(key);
        }

        /** {@inheritDoc} */
        @Override
        public Object[] getArray() {
            return order.toArray();
        }

        /** {@inheritDoc} */
        // Make sure the hashCode depends on the order as well
        @Override
        public int hashCode() {
            int hc = 17;
            hc = hc * 37 + (order == null ? 0 : order.hashCode());
            hc = hc * 37 + super.hashCode();
            return hc;
        }

        public override Boolean Equals(Object obj) 
        {
            if (!(obj is ListedHashTree)) 
            {
                return false;
            }
            ListedHashTree lht = (ListedHashTree) obj;
            return (super.equals(lht) && order.Equals(lht.order));
        }

        /** {@inheritDoc} */
        @Override
        public Set<Object> keySet() {
            return data.keySet();
        }

        /** {@inheritDoc} */
        @Override
        public int size() {
            return data.size();
        }

        private void readObject(ObjectInputStream ois) throws ClassNotFoundException, IOException {
            ois.defaultReadObject();
        }

        private void writeObject(ObjectOutputStream oos) throws IOException {
            oos.defaultWriteObject();
        }

        public override void Clear() 
        {
            super.clear();
            order.clear();
        }
    }
}
