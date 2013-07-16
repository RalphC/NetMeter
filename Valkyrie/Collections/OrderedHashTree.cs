using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Valkyrie.Collections
{
    public class OrderedHashTree : HashTree, ISerializable, ICloneable
    {
        private List<Object> order;

        public OrderedHashTree() : base()
        {
            order = new List<Object>();
        }

        public OrderedHashTree(Object key) : this()
        {
            Data.Add(key, new OrderedHashTree());
            order.Add(key);
        }

        public OrderedHashTree(List<Object> keys) : this()
        {
            foreach (Object key in keys)
            {
                Data.Add(key, new OrderedHashTree());
                order.Add(key);
            }
        }

        public OrderedHashTree(Object[] keys) : this()
        {
            foreach (Object key in keys)
            {
                Data.Add(key, new OrderedHashTree());
                order.Add(key);
            }
        }

        public new Object Clone() 
        {
            OrderedHashTree newTree = new OrderedHashTree();
            CloneTree(newTree);
            return newTree;
        }

        public new void Set(Object key, Object value) 
        {
            if (!Data.ContainsKey(key)) 
            {
                order.Add(key);
            }
            base.Set(key, value);
        }

        public new void Set(Object key, HashTree t) 
        {
            if (!Data.ContainsKey(key)) 
            {
                order.Add(key);
            }
            base.Set(key, t);
        }

        public new void Set(Object key, Object[] values) 
        {
            if (!Data.ContainsKey(key)) 
            {
                order.Add(key);
            }
            base.Set(key, values);
        }

        public new void Set(Object key, List<Object> values) 
        {
            if (!Data.ContainsKey(key)) 
            {
                order.Add(key);
            }
            base.Set(key, values);
        }

        public new void Replace(Object currentKey, Object newKey) 
        {
            HashTree tree = GetTree(currentKey);
            Data.Remove(currentKey);
            Data.Add(newKey, tree);
            // find order.indexOf(currentKey) using == rather than equals()
            // there may be multiple entries which compare equals (Bug 50898)
            // This will be slightly slower than the built-in method,
            // but replace() is not used frequently.
            Int32 entry = order.BinarySearch(currentKey);
            if (entry == -1)
            {
                // throw new JMeterError("Impossible state, data key not present in order: "+currentKey.GetType());
            }
            order[entry] = newKey;
        }

        public new HashTree CreateNewTree() 
        {
            return new OrderedHashTree();
        }

        public new HashTree CreateNewTree(Object key)
        {
            return new OrderedHashTree(key);
        }

        public new HashTree CreateNewTree(List<Object> values)
        {
            return new OrderedHashTree(values);
        }


        public new HashTree Put(Object key) 
        {
            if (!Data.ContainsKey(key)) 
            {
                HashTree newTree = CreateNewTree();
                Data.Add(key, newTree);
                order.Add(key);
                return newTree;
            }
            return GetTree(key);
        }

        public new List<Object> list() 
        {
            return order;
        }

        public new Boolean Remove(Object key) 
        {
            order.Remove(key);
            return Data.Remove(key);
        }

        public new Object[] GetArray() 
        {
            return order.ToArray();
        }

        /** {@inheritDoc} */
        // Make sure the hashCode depends on the order as well
        public override Int32 GetHashCode() 
        {
            Int32 hc = 17;
            hc = hc * 37 + (order == null ? 0 : order.GetHashCode());
            hc = hc * 37 + base.GetHashCode();
            return hc;
        }

        public override Boolean Equals(Object obj) 
        {
            if (!(obj is OrderedHashTree)) 
            {
                return false;
            }
            OrderedHashTree lht = (OrderedHashTree) obj;
            return (base.Equals(lht) && order.Equals(lht.order));
        }

        public List<Object> KeyList() 
        {
            return Data.Keys.ToList();
        }

        public new void Clear() 
        {
            base.Clear();
            order.Clear();
        }
    }
}
