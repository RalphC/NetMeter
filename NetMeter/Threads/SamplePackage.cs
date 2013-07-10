using System;
using System.Collections.Generic;

using NetMeter.TestElements;
using NetMeter.Samplers;
using NetMeter.Assertions;
using NetMeter.Control;

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

        private Sampler sampler;

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
            //this.timers = timers;
            this.assertions = assertions;
            this.postProcessors = postProcessors;
            //this.preProcessors = preProcessors;
            this.controllers = controllers;
        }

        /**
            * Make the SamplePackage the running version, or make it no longer the
            * running version. This tells to each element of the SamplePackage that it's current state must
            * be retrievable by a call to recoverRunningVersion(). 
            * @param running boolean
            * @see TestElement#setRunningVersion(boolean)
            */
        public void setRunningVersion(Boolean running) 
        {
            setRunningVersion<ConfigTestElement>(configs, running);
            setRunningVersion<SampleListener>(sampleListeners, running);
            setRunningVersion<Assertion>(assertions, running);
            //setRunningVersion<Timer>(timers, running);
            setRunningVersion<PostProcessor>(postProcessors, running);
            //setRunningVersion(preProcessors, running);
            setRunningVersion<Controller>(controllers, running);
            sampler.setRunningVersion(running);
        }

        /**
        * Recover each member of SamplePackage to the state before the call of setRunningVersion(true)
        * @see TestElement#recoverRunningVersion()
        */
        public void recoverRunningVersion()
        {
            recoverRunningVersion<ConfigTestElement>(configs);
            recoverRunningVersion<SampleListener>(sampleListeners);
            recoverRunningVersion<Assertion>(assertions);
            //recoverRunningVersion(timers);
            recoverRunningVersion<PostProcessor>(postProcessors);
            //recoverRunningVersion(preProcessors);
            recoverRunningVersion<Controller>(controllers);
            sampler.recoverRunningVersion();
        }

        private void setRunningVersion<T>(List<T> list, Boolean running) 
        {
            // all implementations extend TestElement
            List<TestElement> telist = list.ConvertAll(new Converter<T, TestElement>( (T obj) => (TestElement)obj ));
            foreach (TestElement te in telist) 
            {
                te.setRunningVersion(running);
            }
        }

        private void recoverRunningVersion<T>(List<T> list) 
        {
            // All implementations extend TestElement
            List<TestElement> telist = list.ConvertAll(new Converter<T, TestElement>((T obj) => (TestElement)obj));
            foreach (TestElement te in telist) 
            {
                te.recoverRunningVersion();
            }
        }

        /**
            * @return List<SampleListener>
            */
        public List<SampleListener> getSampleListeners() 
        {
            return sampleListeners;
        }

        /**
            * Add Sample Listener
            * @param listener {@link SampleListener}
            */
        public void addSampleListener(SampleListener listener) 
        {
            sampleListeners.Add(listener);
        }

        ///**
        //    * @return List<Timer>
        //    */
        //public List<Timer> getTimers() 
        //{
        //    return timers;
        //}

    
        /**
            * Add Post processor
            * @param ex {@link PostProcessor}
            */
        public void addPostProcessor(PostProcessor ex) 
        {
            postProcessors.Add(ex);
        }

        /**
            * Add Pre processor
            * @param pre {@link PreProcessor}
            */
        //public void addPreProcessor(PreProcessor pre)
        //{
        //    preProcessors.Add(pre);
        //}

        ///**
        //    * Add Timer
        //    * @param timer {@link Timer}
        //    */
        //public void addTimer(Timer timer) 
        //{
        //    timers.Add(timer);
        //}

        /**
            * Add Assertion
            * @param asser {@link Assertion}
            */
        public void addAssertion(Assertion asser)
        {
            assertions.Add(asser);
        }

        /**
            * @return List<Assertion>
            */
        public List<Assertion> getAssertions() 
        {
            return assertions;
        }

        /**
            * @return List<PostProcessor>
            */
        public List<PostProcessor> getPostProcessors() 
        {
            return postProcessors;
        }

        /**
            * @return {@link Sampler}
            */
        public Sampler getSampler() 
        {
            return sampler;
        }

        /**
            * @param s {@link Sampler}
            */
        public void setSampler(Sampler s) 
        {
            sampler = s;
        }

        /**
            * Returns the preProcessors.
            * @return List<PreProcessor>
            */
        //public List<PreProcessor> getPreProcessors() 
        //{
        //    return preProcessors;
        //}

        /**
            * Returns the configs.
            *
            * @return List
            */
        public List<ConfigTestElement> getConfigs()
        {
            return configs;
        }

    }
}
