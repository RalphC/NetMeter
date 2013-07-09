using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valkyrie.Collections
{
    public class SearchByType<T> : HashTreeTraverser
    {
        private sealed List<T> objectOfType = new List<T>();

        private sealed Dictionary<Object, ListedHashTree> subTree = new Dictionary<Object, ListedHashTree>();
    }
}
