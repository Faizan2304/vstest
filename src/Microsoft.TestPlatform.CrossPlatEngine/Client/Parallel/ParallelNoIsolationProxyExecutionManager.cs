using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    internal class ParallelNoIsolationProxyExecutionManager: ParallelInIsolationOperationManager<IProxyExecutionManager, ITestRunEventsHandler>, IParallelProxyExecutionManager
    {
        private int availableTestSources = -1;
        private TestRunCriteria actualTestRunCriteria;

        private IEnumerator<string> sourceEnumerator;

        private IEnumerator testCaseListEnumerator;

        private bool hasSpecificTestsRun = false;
        public bool IsInitialized { get; private set; } = false;

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
            throw new NotImplementedException();
        }

        public int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler)
        {
            this.hasSpecificTestsRun = testRunCriteria.HasSpecificTests;
            this.actualTestRunCriteria = testRunCriteria;

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
            return this.StartTestRunPrivate(eventHandler);
        }

        private int StartTestRunPrivate(ITestRunEventsHandler runEventsHandler)
        {
            return 1;
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
