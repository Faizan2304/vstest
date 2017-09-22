// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    public interface IUnitTestTelemetryServiceProvider
    {
        /// <summary>
        /// Log Event wrapper
        /// </summary>
        /// <param name="eventName"> Name of the event</param>
        /// <param name="property"> name of the property in that event</param>
        /// <param name="value"> value of property</param>
        void LogEvent(string eventName, string property, object value);

        /// <summary>
        /// Post Event
        /// </summary>
        /// <param name="eventName"> Name of the event</param>
        void PostEvent(string eventName);

        void Dispose();
    }
}
