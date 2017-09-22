// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;

    public interface ITestPlatform : IDisposable
    {
        /// <summary>
        /// Update the extensions to be used by the test service
        /// </summary>
        /// <param name="pathToAdditionalExtensions">
        /// Specifies the path to unit test extensions. 
        /// If no additional extension is available, then specify null or empty list.
        /// </param>
        /// <param name="loadOnlyWellKnownExtensions">Specifies whether only well known extensions should be loaded.</param>
        void UpdateExtensions(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions);

        /// <summary>
        /// Clear the extensions
        /// </summary>
        void ClearExtensions();

        /// <summary>
        /// Creates a discovery request
        /// </summary>
        /// <param name="requestData">Providing common services and data for Discovery</param>
        /// <param name="discoveryCriteria">Specifies the discovery parameters</param>
        /// <returns>DiscoveryRequest object</returns>
        IDiscoveryRequest CreateDiscoveryRequest(IRequestData requestData, DiscoveryCriteria discoveryCriteria);

        /// <summary>
        /// Creates a test run request.
        /// </summary>
        /// <param name="requestData">Providing common services and data for Execution</param>
        /// <param name="testRunCriteria">Specifies the test run criteria</param>
        /// <returns>RunRequest object</returns>
        ITestRunRequest CreateTestRunRequest(IRequestData requestData, TestRunCriteria testRunCriteria);

        //TPV1
        /// <summary>
        /// Creates a discovery request
        /// </summary>
        /// <param name="discoveryCriteria">Specifies the discovery parameters</param>
        /// <returns></returns>
        IDiscoveryRequest CreateDiscoveryRequest(DiscoveryCriteria discoveryCriteria);


        //TPV1
        /// <summary>
        /// Creates a discovery request
        /// </summary>
        /// <param name="discoveryCriteria">Specifies the discovery parameters</param>
        /// <returns></returns>
        IDiscoveryRequest CreateDiscoveryRequest2(DiscoveryCriteria discoveryCriteria);

        //TPv1
        /// <summary>
        /// Create a multi-test run request
        /// </summary>
        /// <param name="baseTestRunCriteria">Specifies the base test run criteria</param>
        IMultipleTestRunRequest CreateMultipleTestRunRequest(BaseTestRunCriteria baseTestRunCriteria);

        //TPv1
        /// <summary>
        /// Initialize the test platform with the path to additional unit test extensions. 
        /// If no additional extension is available, then specify null or empty list. 
        /// </summary>
        /// <param name="additionalUnitTestExtensions">Specifies the path to unit test extensions.</param>
        /// <param name="loadOnlyWellKnownExtensions">Specifies whether only well known extensions should be loaded.</param>
        /// <param name="forceX86Discoverer">Forces test discovery in x86 Discoverer process.</param>
        void Initialize(IEnumerable<string> pathToAdditionalExtensions, bool loadOnlyWellKnownExtensions, bool forceX86Discoverer);

        //TPv1
        /// <summary>
        /// Starts preparing for the first test run with the parameter settings. 
        /// </summary>
        /// <param name="testRunSettings">Specifies the settings with which framework should initialize.</param>
        /// <param name="testExecutorLauncher">Custom test executor launcher. If null then default will be used.</param>
        IAsyncResult StartPreparingForFirstTestRunRequest(string testRunSettings, ITestExecutorLauncher testExecutorLauncher);

        //tpv1
        /// <summary>
        /// Creates a test run request.
        /// </summary>
        /// <param name="testRunCriteria">Specifies the test run criteria</param>
        /// <returns></returns>
        ITestRunRequest CreateTestRunRequest(TestRunCriteria testRunCriteria);


    }
}
