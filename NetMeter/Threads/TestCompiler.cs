﻿using log4net;
using NetMeter.TestElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valkyrie.Collections;
using Valkyrie.Logging;
using NetMeter.Util;
using NetMeter.Samplers;
using NetMeter.Control;

namespace NetMeter.Threads
{
    public class TestCompiler : HashTreeTraverser
    {
        private static sealed ILog LOG = LoggingManager.GetLoggerForClass();

        /**
         * Set this property {@value} to true to revert to using a shared static set.
         */
        private static sealed String USE_STATIC_SET = "TestCompiler.useStaticSet";
    
        /**
         * The default value - {@value} - assumed for {@link #USE_STATIC_SET}. 
         */
        private static sealed Boolean USE_STATIC_SET_DEFAULT = false;

        public static sealed Boolean IS_USE_STATIC_SET = NetMeterUtils.getPropDefault(USE_STATIC_SET, USE_STATIC_SET_DEFAULT);

        /**
         * This set keeps track of which ObjectPairs have been seen.
         * It seems to be used to prevent adding a child to a parent if the child has already been added.
         * If the ObjectPair (child, parent) is present, then the child has been added.
         * Otherwise, the child is added to the parent and the pair is added to the Set.
         */
        private static sealed HashSet<ObjectPair> PAIRING = new HashSet<ObjectPair>();

        private sealed LinkedList<TestElement> stack = new LinkedList<TestElement>();

        private sealed Dictionary<Sampler, SamplePackage> samplerConfigMap = new Dictionary<Sampler, SamplePackage>();

        private sealed Dictionary<TransactionController, SamplePackage> transactionControllerConfigMap =
            new Dictionary<TransactionController, SamplePackage>();

        private sealed HashTree testTree;

        public TestCompiler(HashTree testTree) {
            this.testTree = testTree;
        }

        /**
         * Clears the pairing Set Called by StandardJmeterEngine at the start of a
         * test run.
         */
        public static void Initialize() 
        {
            // synch is probably not needed as only called before run starts
            //synchronized (PAIRING) 
            {
                PAIRING.Clear();
            }
        }

        /**
         * Configures sampler from SamplePackage extracted from Test plan and returns it
         * @param sampler {@link Sampler}
         * @return {@link SamplePackage}
         */
        public SamplePackage configureSampler(Sampler sampler) 
        {
            SamplePackage pack;
            if (samplerConfigMap.TryGetValue(sampler, out pack))
            {
                pack.SetSampler(sampler);
                ConfigureWithConfigElements(sampler, pack.GetConfigs());
            }
            
            return pack;
        }

        /**
         * Configures Transaction Sampler from SamplePackage extracted from Test plan and returns it
         * @param transactionSampler {@link TransactionSampler}
         * @return {@link SamplePackage}
         */
        public SamplePackage ConfigureTransactionSampler(TransactionSampler transactionSampler) 
        {
            TransactionController controller = transactionSampler.getTransactionController();
            SamplePackage pack = null;
            if (transactionControllerConfigMap.TryGetValue(controller, out pack))
            {
                pack.SetSampler(transactionSampler);
            }
            return pack;
        }

        /**
         * Reset pack to its initial state
         * @param pack
         */
        public void Done(SamplePackage pack) 
        {
            pack.RecoverRunningVersion();
        }

        /** {@inheritDoc} */
        public void AddNode(Object node, HashTree subTree) 
        {
            stack.AddLast((TestElement) node);
        }

        /** {@inheritDoc} */
        public void SubtractNode() 
        {
            LOG.Debug("Subtracting node, stack size = " + stack.Count);
            TestElement child = stack.Last.Value;
            trackIterationListeners(stack);
            if (child is Sampler) 
            {
                saveSamplerConfigs((Sampler) child);
            }
            else if(child is TransactionController) 
            {
                saveTransactionControllerConfigs((TransactionController) child);
            }
            stack.RemoveLast();
            if (stack.Count > 0) 
            {
                TestElement parent = stack.Last.Value;
                Boolean duplicate = false;
                // Bug 53750: this condition used to be in ObjectPair#addTestElements()
                if (parent is Controller && (child is Sampler || child is Controller)) 
                {
                    if (!IS_USE_STATIC_SET && parent instanceof TestCompilerHelper) 
                    {
                        TestCompilerHelper te = (TestCompilerHelper) parent;
                        duplicate = !te.addTestElementOnce(child);
                    } 
                    else
                    { // this is only possible for 3rd party controllers by default
                        ObjectPair pair = new ObjectPair(child, parent);
                        synchronized (PAIRING)
                        {// Called from multiple threads
                            if (!PAIRING.Contains(pair)) 
                            {
                                parent.addTestElement(child);
                                PAIRING.Add(pair);
                            } else {
                                duplicate = true;
                            }
                        }
                    }
                }
                if (duplicate) {
                    LOG.Warn("Unexpected duplicate for " + parent.GetType().Name + " and " + child.GetType().Name);
                }
            }
        }

        private void trackIterationListeners(LinkedList<TestElement> p_stack)
        {
            TestElement child = p_stack.Last.Value;
            if (child instanceof LoopIterationListener) 
            {
                ListIterator<TestElement> iter = p_stack.listIterator(p_stack.size());
                while (iter.hasPrevious()) {
                    TestElement item = iter.previous();
                    if (item == child) {
                        continue;
                    }
                    if (item instanceof Controller) {
                        TestBeanHelper.prepare(child);
                        ((Controller) item).addIterationListener((LoopIterationListener) child);
                        break;
                    }
                }
            }
        }

        /** {@inheritDoc} */
        @Override
        public void processPath() {
        }

        private void saveSamplerConfigs(Sampler sam) {
            List<ConfigTestElement> configs = new LinkedList<ConfigTestElement>();
            List<Controller> controllers = new LinkedList<Controller>();
            List<SampleListener> listeners = new LinkedList<SampleListener>();
            List<Timer> timers = new LinkedList<Timer>();
            List<Assertion> assertions = new LinkedList<Assertion>();
            LinkedList<PostProcessor> posts = new LinkedList<PostProcessor>();
            LinkedList<PreProcessor> pres = new LinkedList<PreProcessor>();
            for (int i = stack.size(); i > 0; i--) {
                addDirectParentControllers(controllers, stack.get(i - 1));
                List<PreProcessor>  tempPre = new LinkedList<PreProcessor> ();
                List<PostProcessor> tempPost = new LinkedList<PostProcessor>();
                for (Object item : testTree.list(stack.subList(0, i))) {
                    if ((item instanceof ConfigTestElement)) {
                        configs.add((ConfigTestElement) item);
                    }
                    if (item instanceof SampleListener) {
                        listeners.add((SampleListener) item);
                    }
                    if (item instanceof Timer) {
                        timers.add((Timer) item);
                    }
                    if (item instanceof Assertion) {
                        assertions.add((Assertion) item);
                    }
                    if (item instanceof PostProcessor) {
                        tempPost.add((PostProcessor) item);
                    }
                    if (item instanceof PreProcessor) {
                        tempPre.add((PreProcessor) item);
                    }
                }
                pres.addAll(0, tempPre);
                posts.addAll(0, tempPost);
            }

            SamplePackage pack = new SamplePackage(configs, listeners, timers, assertions,
                    posts, pres, controllers);
            pack.setSampler(sam);
            pack.setRunningVersion(true);
            samplerConfigMap.put(sam, pack);
        }

        private void saveTransactionControllerConfigs(TransactionController tc) {
            List<ConfigTestElement> configs = new LinkedList<ConfigTestElement>();
            List<Controller> controllers = new LinkedList<Controller>();
            List<SampleListener> listeners = new LinkedList<SampleListener>();
            List<Timer> timers = new LinkedList<Timer>();
            List<Assertion> assertions = new LinkedList<Assertion>();
            LinkedList<PostProcessor> posts = new LinkedList<PostProcessor>();
            LinkedList<PreProcessor> pres = new LinkedList<PreProcessor>();
            for (int i = stack.size(); i > 0; i--) {
                addDirectParentControllers(controllers, stack.get(i - 1));
                for (Object item : testTree.list(stack.subList(0, i))) {
                    if (item instanceof SampleListener) {
                        listeners.add((SampleListener) item);
                    }
                    if (item instanceof Assertion) {
                        assertions.add((Assertion) item);
                    }
                }
            }

            SamplePackage pack = new SamplePackage(configs, listeners, timers, assertions,
                    posts, pres, controllers);
            pack.setSampler(new TransactionSampler(tc, tc.getName()));
            pack.setRunningVersion(true);
            transactionControllerConfigMap.put(tc, pack);
        }

        /**
         * @param controllers
         * @param i
         */
        private void addDirectParentControllers(List<Controller> controllers, TestElement maybeController) {
            if (maybeController instanceof Controller) {
                LOG.debug("adding controller: " + maybeController + " to sampler config");
                controllers.add((Controller) maybeController);
            }
        }

        private static class ObjectPair
        {
            private final TestElement child;
            private final TestElement parent;

            public ObjectPair(TestElement child, TestElement parent) {
                this.child = child;
                this.parent = parent;
            }

            /** {@inheritDoc} */
            @Override
            public int hashCode() {
                return child.hashCode() + parent.hashCode();
            }

            /** {@inheritDoc} */
            @Override
            public boolean equals(Object o) {
                if (o instanceof ObjectPair) {
                    return child == ((ObjectPair) o).child && parent == ((ObjectPair) o).parent;
                }
                return false;
            }
        }

        private void configureWithConfigElements(Sampler sam, List<ConfigTestElement> configs) {
            sam.clearTestElementChildren();
            for (ConfigTestElement config  : configs) {
                if (!(config instanceof NoConfigMerge)) 
                {
                    if(sam instanceof ConfigMergabilityIndicator) {
                        if(((ConfigMergabilityIndicator)sam).applies(config)) {
                            sam.addTestElement(config);
                        }
                    } else {
                        // Backward compatibility
                        sam.addTestElement(config);
                    }
                }
            }
        }
    }
}
