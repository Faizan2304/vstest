using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    internal class ParallelNoIsolationProxyExecutionManager : ParallelInIsolationOperationManager<IProxyExecutionManager, ITestRunEventsHandler>, IParallelProxyExecutionManager
    {
        private int parallelLevel = 1;
        private int availableTestSources = -1;
        private int runCompletedClients = 0;
        private int runStartedClients = 0;

        private TestRunCriteria actualTestRunCriteria;

        private IEnumerator<string> sourceEnumerator;

        private IEnumerator testCaseListEnumerator;

        private bool hasSpecificTestsRun = false;
        private ITestRunEventsHandler currentRunEventsHandler;


        private ParallelRunDataAggregator currentRunDataAggregator;
        public bool IsInitialized { get; private set; } = false;

        public ParallelNoIsolationProxyExecutionManager(int parallelLevel)
        {
            if (parallelLevel > 0)
            {
                this.parallelLevel = parallelLevel;
            }
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public bool HandlePartialRunComplete(IProxyExecutionManager proxyExecutionManager, TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            // No action
        }

        public int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            this.hasSpecificTestsRun = testRunCriteria.HasSpecificTests;
            this.actualTestRunCriteria = testRunCriteria;
            this.currentRunEventsHandler = eventHandler;

            // Reset the runcomplete data
            this.runCompletedClients = 0;

            if (this.hasSpecificTestsRun)
            {
                var testCasesBySource = new Dictionary<string, List<TestCase>>();
                foreach (var test in testRunCriteria.Tests)
                {
                    if (!testCasesBySource.ContainsKey(test.Source))
                    {
                        testCasesBySource.Add(test.Source, new List<TestCase>());
                    }

                    testCasesBySource[test.Source].Add(test);
                }

                // Do not use "Dictionary.ValueCollection.Enumerator" - it becomes undetermenstic once we go out of scope of this method
                // Use "ToArray" to copy ValueColleciton to a simple array and use it's enumerator
                // Set the enumerator for parallel yielding of testCases
                // Whenever a concurrent executor becomes free, it picks up the next set of testCases using this enumerator
                var testCaseLists = testCasesBySource.Values.ToArray();
                this.testCaseListEnumerator = testCaseLists.GetEnumerator();
                this.availableTestSources = testCaseLists.Length;
            }
            else
            {
                // Set the enumerator for parallel yielding of sources
                // Whenever a concurrent executor becomes free, it picks up the next source using this enumerator
                this.sourceEnumerator = testRunCriteria.Sources.GetEnumerator();
                this.availableTestSources = testRunCriteria.Sources.Count();
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("ParallelProxyExecutionManager: Start execution. Total sources: " + this.availableTestSources);
            }


            // One data aggregator per parallel run
            this.currentRunDataAggregator = new ParallelRunDataAggregator();

            for (int i = 0; i < this.parallelLevel; i++)
            {
                this.StartTestRunOnConcurrentManager();
            }

            return 1;
        }


        /// <summary>
        /// Triggers the execution for the next data object on the concurrent executor
        /// Each concurrent executor calls this method, once its completed working on previous data
        /// </summary>
        /// <param name="proxyExecutionManager">Proxy execution manager instance.</param>
        /// <returns>True, if execution triggered</returns>
        private void StartTestRunOnConcurrentManager()
        {
            TestRunCriteria testRunCriteria = null;
            string testSource = null;
            if (!this.hasSpecificTestsRun)
            {
                if (this.TryFetchNextSource(this.sourceEnumerator, out string nextSource))
                {
                    testSource = nextSource;
                    EqtTrace.Info("ProxyParallelExecutionManager: Triggering test run for next source: {0}", nextSource);

                    testRunCriteria = new TestRunCriteria(
                                          new[] { nextSource },
                                          this.actualTestRunCriteria.FrequencyOfRunStatsChangeEvent,
                                          this.actualTestRunCriteria.KeepAlive,
                                          this.actualTestRunCriteria.TestRunSettings,
                                          this.actualTestRunCriteria.RunStatsChangeEventTimeout,
                                          this.actualTestRunCriteria.TestHostLauncher)
                    {
                        TestCaseFilter = this.actualTestRunCriteria.TestCaseFilter
                    };
                }
            }
            else
            {
                if (this.TryFetchNextSource(this.testCaseListEnumerator, out List<TestCase> nextSetOfTests))
                {
                    testSource = nextSetOfTests?.FirstOrDefault()?.Source;
                    EqtTrace.Info("ProxyParallelExecutionManager: Triggering test run for next source: {0}", nextSetOfTests?.FirstOrDefault()?.Source);

                    testRunCriteria = new TestRunCriteria(
                                          nextSetOfTests,
                                          this.actualTestRunCriteria.FrequencyOfRunStatsChangeEvent,
                                          this.actualTestRunCriteria.KeepAlive,
                                          this.actualTestRunCriteria.TestRunSettings,
                                          this.actualTestRunCriteria.RunStatsChangeEventTimeout,
                                          this.actualTestRunCriteria.TestHostLauncher);
                }
            }

            if (testRunCriteria != null)
            {
                var concurrentProxyExecutionManager = new AppDomainExecutionManagerInvoker(testSource);
                var parallelEventHandler = new ParallelRunEventsHandler(
                                                concurrentProxyExecutionManager,
                                                this.currentRunEventsHandler,
                                                this,
                                                this.currentRunDataAggregator);

                this.AddManager(concurrentProxyExecutionManager, parallelEventHandler);

                if (!concurrentProxyExecutionManager.IsInitialized)
                {
                    concurrentProxyExecutionManager.Initialize();
                }

                Task.Run(() =>
                {
                    Interlocked.Increment(ref this.runStartedClients);
                    if (EqtTrace.IsVerboseEnabled)
                    {
                        EqtTrace.Verbose("ParallelProxyExecutionManager: Execution started. Started clients: " + this.runStartedClients);
                    }

                    concurrentProxyExecutionManager.StartTestRun(testRunCriteria, parallelEventHandler);
                })
                .ContinueWith(t =>
                {
                    // Just in case, the actual execution couldn't start for an instance. Ensure that
                    // we call execution complete since we have already fetched a source. Otherwise
                    // execution will not terminate
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("ParallelProxyExecutionManager: Failed to trigger execution. Exception: " + t.Exception);
                    }

                    // Send a run complete to caller. Similar logic is also used in ProxyExecutionManager.StartTestRun
                    // Differences:
                    // Aborted is sent to allow the current execution manager replaced with another instance
                    // Ensure that the test run aggregator in parallel run events handler doesn't add these statistics
                    // (since the test run didn't even start)
                    var completeArgs = new TestRunCompleteEventArgs(null, false, true, null, new Collection<AttachmentSet>(), TimeSpan.Zero);
                    this.GetHandlerForGivenManager(concurrentProxyExecutionManager).HandleTestRunComplete(completeArgs, null, null, null);
                },
                TaskContinuationOptions.OnlyOnFaulted);
            }

            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("ProxyParallelExecutionManager: No sources available for execution.");
            }
        }
        #region ParallelOperationManager Methods

        protected override void DisposeInstance(IProxyExecutionManager managerInstance)
        {
            if (managerInstance != null)
            {
                try
                {
                    managerInstance.Close();
                }
                catch (Exception ex)
                {
                    // ignore any exceptions
                    EqtTrace.Error("ParallelProxyExecutionManager: Failed to dispose execution manager. Exception: " + ex);
                }
            }
        }

        #endregion
    }
}
