using System;
using System.Collections.Generic;

using NetMeter.TestElements;
using NetMeter.Samplers;
using NetMeter.Assertions;
using NetMeter.Control;
using NetMeter.Processor;

namespace NetMeter.Threads
{
    class SamplePackage
    {
        /**
         * Packages methods related to sample handling.<br/>
         * A SamplePackage contains all elements associated to a Sampler:
         * <ul>
         * 	<li>SampleListener(s)</li>
         * 	<li>Timer(s)</li>
         * 	<li>Assertion(s)</li>
         * 	<li>PreProcessor(s)</li>
         * 	<li>PostProcessor(s)</li>
         * 	<li>ConfigTestElement(s)</li>
         * 	<li>Controller(s)</li>
         * </ul>
         */

        private sealed List<SampleListener> sampleListeners;

        //private sealed List<Timer> timers;

        private sealed List<Assertion> assertions;

        private sealed List<PostProcessor> postProcessors;

        //private sealed List<PreProcessor> preProcessors;

        private sealed List<ConfigTestElement> configs;

        private sealed List<Controller> controllers;

        private TestAgent sampler;

        public SamplePackage(
                List<ConfigTestElement> configs,
                List<SampleListener> listeners,
                //List<Timer> timers,
                List<Assertion> assertions, 
                List<PostProcessor> postProcessors, 
                //List<PreProcessor> preProcessors,
                List<Controller> controllers) 
        {
            this.configs = configs;
            this.sampleListeners = listeners;
            this.assertions = assertions;
            this.postProcessors = postProcessors;
            this.controllers = controllers;
        }

        /**
            * Make the SamplePackage the running version, or make it no longer the
            * running version. This tells to each element of the SamplePackage that it's current state must
            * be retrievable by a call to recoverRunningVersion(). 
            * @param running boolean
            * @see TestElement#setRunningVersion(boolean)
            */
        public void SetRunningVersion(Boolean running) 
        {
            SetRunningVersion<ConfigTestElement>(configs, running);
            SetRunningVersion<SampleListener>(sampleListeners, running);
            SetRunningVersion<Assertion>(assertions, running);
            SetRunningVersion<PostProcessor>(postProcessors, running);
            SetRunningVersion<Controller>(controllers, running);
            sampler.SetRunningVersion(running);
        }

        /**
        * Recover each member of SamplePackage to the state before the call of setRunningVersion(true)
        * @see TestElement#recoverRunningVersion()
        */
        public void RecoverRunningVersion()
        {
            RecoverRunningVersion<ConfigTestElement>(configs);
            RecoverRunningVersion<SampleListener>(sampleListeners);
            RecoverRunningVersion<Assertion>(assertions);
            RecoverRunningVersion<PostProcessor>(postProcessors);
            RecoverRunningVersion<Controller>(controllers);
            sampler.RecoverRunningVersion();
        }

        private void SetRunningVersion<T>(List<T> list, Boolean running) 
        {
            // all implementations extend TestElement
            List<TestElement> telist = list.ConvertAll(new Converter<T, TestElement>( (T obj) => (TestElement)obj ));
            foreach (TestElement te in telist) 
            {
                te.SetRunningVersion(running);
            }
        }

        private void RecoverRunningVersion<T>(List<T> list) 
        {
            // All implementations extend TestElement
            List<TestElement> telist = list.ConvertAll(new Converter<T, TestElement>((T obj) => (TestElement)obj));
            foreach (TestElement te in telist) 
            {
                te.RecoverRunningVersion();
            }
        }

        /**
            * @return List<SampleListener>
            */
        public List<SampleListener> GetSampleListeners() 
        {
            return sampleListeners;
        }

        /**
            * Add Sample Listener
            * @param listener {@link SampleListener}
            */
        public void AddSampleListener(SampleListener listener) 
        {
            sampleListeners.Add(listener);
        }
    
        /**
            * Add Post processor
            * @param ex {@link PostProcessor}
            */
        public void AddPostProcessor(PostProcessor ex) 
        {
            postProcessors.Add(ex);
        }

        /**
            * Add Assertion
            * @param asser {@link Assertion}
            */
        public void AddAssertion(Assertion asser)
        {
            assertions.Add(asser);
        }

        /**
            * @return List<Assertion>
            */
        public List<Assertion> GetAssertions() 
        {
            return assertions;
        }

        /**
            * @return List<PostProcessor>
            */
        public List<PostProcessor> GetPostProcessors() 
        {
            return postProcessors;
        }

        /**
            * @return {@link Sampler}
            */
        public TestAgent GetSampler() 
        {
            return sampler;
        }

        /**
            * @param s {@link Sampler}
            */
        public void SetSampler(TestAgent s) 
        {
            sampler = s;
        }

        /**
            * Returns the configs.
            *
            * @return List
            */
        public List<ConfigTestElement> GetConfigs()
        {
            return configs;
        }

    }
}
