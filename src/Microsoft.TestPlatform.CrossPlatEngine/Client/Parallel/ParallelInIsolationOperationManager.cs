// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    internal abstract class ParallelInIsolationOperationManager<T, U> : IParallelOperationManager, IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void UpdateParallelLevel(int parallelLevel)
        {
            throw new System.NotImplementedException();
        }
    }
}
