// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    public interface IMultipleTestRunRequest
    {
        /// <summary>
        /// Start the session
        /// </summary>
        void StartSession();

        /// <summary>
        /// Create a test run request for specific tests 
        /// </summary>
        ITestRunRequest CreateTestRunRequest(IEnumerable<TestCase> tests);

        /// <summary>
        /// Create a test run request for specific sources. 
        /// </summary>
        ITestRunRequest CreateTestRunRequest(IEnumerable<string> sources);

        /// <summary>
        /// End the session
        /// </summary>
        Collection<AttachmentSet> EndSession();

        /// <summary>
        /// Raised when the test message is received.
        /// </summary>
        event EventHandler<TestRunMessageEventArgs> TestRunMessage;

        /// <summary>
        /// Raised when data collection message is received.
        /// </summary>
        event EventHandler<DataCollectionMessageEventArgs> DataCollectionMessage;
    }
}
