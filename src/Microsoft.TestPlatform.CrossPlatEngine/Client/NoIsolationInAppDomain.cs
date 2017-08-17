// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;

    class NoIsolationInAppDomain
#if NET451
        : MarshalByRefObject
#endif
    {
        private ITestHostManagerFactory testHostManagerFactory;

        /// <summary>
        /// Serializer for the data objects
        /// </summary>
        private IDataSerializer dataSerializer;
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoIsolationProxyexecutionManager"/> class.
        /// </summary>
        public NoIsolationInAppDomain() : this(new TestHostManagerFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoIsolationProxyexecutionManager"/> class.
        /// </summary>
        /// <param name="testHostManagerFactory">
        /// Manager factory
        /// </param>
        protected NoIsolationInAppDomain(ITestHostManagerFactory testHostManagerFactory)
        {
            this.dataSerializer = JsonDataSerializer.Instance;
            this.testHostManagerFactory = testHostManagerFactory;
        }

        /// <summary>
        /// Initializes the execution manager.
        /// </summary>
        /// <param name="pathToExtensions"> The path to extensions. </param>
        public void Initialize(IEnumerable<string> pathToExtensions)
        {
            if (!this.IsInitialized)
            {
                if (pathToExtensions != null && pathToExtensions.Count() > 0)
                {
                    var executionManager = this.testHostManagerFactory.GetExecutionManager();

                    // Initialize extension before execution
                    executionManager.Initialize(pathToExtensions);

                    this.IsInitialized = true;
                }
            }
        }

        public int StartTestRun(string testRunCriteriaMessage, ITestRunEventsHandler eventHandler)
        {
            var message = this.dataSerializer.DeserializeMessage(testRunCriteriaMessage);
            var testRunCriteria = this.dataSerializer.DeserializePayload<TestRunCriteria>(message);
            var executionManager = this.testHostManagerFactory.GetExecutionManager();

            if (!this.IsInitialized)
            {
                executionManager.Initialize(Enumerable.Empty<string>());
            }

            var executionContext = new TestExecutionContext(
                        testRunCriteria.FrequencyOfRunStatsChangeEvent,
                        testRunCriteria.RunStatsChangeEventTimeout,
                        inIsolation: false,
                        keepAlive: testRunCriteria.KeepAlive,
                        isDataCollectionEnabled: false,
                        areTestCaseLevelEventsRequired: false,
                        hasTestRun: true,
                        isDebug: (testRunCriteria.TestHostLauncher != null && testRunCriteria.TestHostLauncher.IsDebug),
                        testCaseFilter: testRunCriteria.TestCaseFilter);

            if (testRunCriteria.HasSpecificSources)
            {
                // [TODO]: we need to revisit to second-last argument if we will enable datacollector. 
                Task.Run(() => executionManager.StartTestRun(testRunCriteria.AdapterSourceMap, testRunCriteria.TestRunSettings, executionContext, null, eventHandler));
            }
            else
            {
                // [TODO]: we need to revisit to second-last argument if we will enable datacollector. 
                Task.Run(() => executionManager.StartTestRun(testRunCriteria.Tests, testRunCriteria.TestRunSettings, executionContext, null, eventHandler));
            }

            return 0;
        }

        /// <summary>
        /// Aborts the test operation.
        /// </summary>
        public void Abort()
        {
            Task.Run(() => this.testHostManagerFactory.GetExecutionManager().Abort());
        }

        /// <summary>
        /// Cancels the test run.
        /// </summary>
        public void Cancel()
        {
            Task.Run(() => this.testHostManagerFactory.GetExecutionManager().Cancel());
        }

        /// <summary>
        /// Closes the current test operation.
        /// This function is of no use in this context as we are not creating any testhost
        /// </summary>
        public void Close()
        {
        }
    }
}

