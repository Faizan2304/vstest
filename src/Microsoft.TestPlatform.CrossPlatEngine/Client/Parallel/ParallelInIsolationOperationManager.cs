// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    internal abstract class ParallelInIsolationOperationManager<T, U> : IParallelOperationManager, IDisposable
    {
        private IDictionary<T, U> concurrentManagerHandlerMap;

        /// <summary>
        /// Singleton Instance of this class
        /// </summary>
        protected static T instance = default(T);

        /// <summary>
        /// LockObject to iterate our sourceEnumerator in parallel
        /// We can use the sourceEnumerator itself as lockObject, but since its a changing object - it's risky to use it as one
        /// </summary>
        protected object sourceEnumeratorLockObject = new object();

        /// <summary>
        /// Remove and dispose a manager from concurrent list of manager.
        /// </summary>
        /// <param name="manager">Manager to remove</param>
        public void RemoveManager(T manager)
        {
            this.concurrentManagerHandlerMap.Remove(manager);
            this.DisposeInstance(manager);
        }

        /// <summary>
        /// Add a manager in concurrent list of manager.
        /// </summary>
        /// <param name="manager">Manager to add</param>
        /// <param name="handler">eventHandler of the manager</param>
        public void AddManager(T manager, U handler)
        {
            this.concurrentManagerHandlerMap.Add(manager, handler);
        }

        /// <summary>
        /// Update event handler for the manager.
        /// If it is a new manager, add this.
        /// </summary>
        /// <param name="manager">Manager to update</param>
        /// <param name="handler">event handler to update for manager</param>
        public void UpdateHandlerForManager(T manager, U handler)
        {
            if (this.concurrentManagerHandlerMap.ContainsKey(manager))
            {
                this.concurrentManagerHandlerMap[manager] = handler;
            }
            else
            {
                this.AddManager(manager, handler);
            }
        }

        /// <summary>
        /// Get the event handler associated with the manager.
        /// </summary>
        /// <param name="manager">Manager</param>
        public U GetHandlerForGivenManager(T manager)
        {
            return this.concurrentManagerHandlerMap[manager];
        }

        /// <summary>
        /// Get total number of active concurrent manager
        /// </summary>
        public int GetConcurrentManagersCount()
        {
            return this.concurrentManagerHandlerMap.Count;
        }

        /// <summary>
        /// Get instances of all active concurrent manager
        /// </summary>
        public IEnumerable<T> GetConcurrentManagerInstances()
        {
            return this.concurrentManagerHandlerMap.Keys.ToList();
        }

        public void Dispose()
        {
            if (this.concurrentManagerHandlerMap != null)
            {
                foreach (var managerInstance in this.GetConcurrentManagerInstances())
                {
                    this.DisposeInstance(managerInstance);
                }
            }

            instance = default(T);
        }

        /// <summary>
        /// Fetches the next data object for the concurrent executor to work on
        /// </summary>
        /// <param name="source">sourcedata to work on - sourcefile or testCaseList</param>
        /// <returns>True, if data exists. False otherwise</returns>
        protected bool TryFetchNextSource<Y>(IEnumerator enumerator, out Y source)
        {
            source = default(Y);
            var hasNext = false;
            lock (sourceEnumeratorLockObject)
            {
                if (enumerator != null && enumerator.MoveNext())
                {
                    source = (Y)enumerator.Current;
                    hasNext = source != null;
                }
            }

            return hasNext;
        }

        public void UpdateParallelLevel(int parallelLevel)
        {
        }

        #region AbstractMethods

        protected abstract void DisposeInstance(T managerInstance);

        #endregion
    }
}
