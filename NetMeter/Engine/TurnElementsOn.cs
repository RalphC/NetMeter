using System;
using Valkyrie.Collections;
using NetMeter.TestElements;

namespace NetMeter.Engine
{
    public class TurnElementsOn : HashTreeTraverser
    {
        public void addNode(Object node, HashTree subTree) 
        {
            if (typeof(TestElement).IsAssignableFrom(node.GetType()) && !typeof(TestPlan).IsAssignableFrom(node.GetType())) 
            {
                ((TestElement) node).SetRunningVersion(true);
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
