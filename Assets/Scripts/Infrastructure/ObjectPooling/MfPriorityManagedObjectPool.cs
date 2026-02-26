using System;
using System.Collections.Generic;

namespace SpiralingStudio.Utils
{
    public class MfPriorityManagedObjectPool<T> : MfManagedObjectPool<T> where T : class, IMfPooledObject
    {
        private readonly Dictionary<T, PriorityValueReference> activeInstances;
        private readonly int maxCapacity;
        private readonly object lockObj = new object();

        public MfPriorityManagedObjectPool(Func<T> createObject, Action<T> getAction, Action<T> releaseAction,
            Action<T> destroyAction, int maxCapacity, int defaultCapacity = 10, bool collectionChecks = true)
        {
            this.maxCapacity = maxCapacity;
            activeInstances = new Dictionary<T, PriorityValueReference>(ReferenceEqualityComparer<T>.Instance);
            base.InitializeObjectPool(createObject, getAction, releaseAction, destroyAction, maxCapacity,
                defaultCapacity, collectionChecks);
        }

        public bool RequestInstance(out T value, PriorityValueReference requestPriority)
        {
            if (requestPriority == null)
            {
                throw new ArgumentNullException(nameof(requestPriority));
            }

            T instanceToRemove = null;
            bool shouldRemoveInstance = false;
            int requestedPriority = requestPriority.GetPriority();

            lock (lockObj)
            {
                // Fast path: capacity available
                if (activeInstances.Count < maxCapacity)
                {
                    value = GetObject();
                    activeInstances[value] = requestPriority;
                    return true;
                }

                // Find lowest priority instance if at capacity
                int lowestPriorityValue = int.MaxValue;
                foreach (var kvp in activeInstances)
                {
                    int priority = kvp.Value.GetPriority();
                    if (priority < lowestPriorityValue)
                    {
                        lowestPriorityValue = priority;
                        instanceToRemove = kvp.Key;
                    }
                }

                // Determine if we should evict the lowest priority instance
                if (instanceToRemove != null && requestedPriority > lowestPriorityValue)
                {
                    shouldRemoveInstance = true;
                    activeInstances.Remove(instanceToRemove); // Remove from tracking before releasing lock
                }
                else
                {
                    // Request priority too low, reject
                    value = null;
                    return false;
                }
            }

            // Release the instance outside of lock to minimize lock hold time
            if (shouldRemoveInstance && instanceToRemove != null)
            {
                ReleaseObject(instanceToRemove);
            }

            // Get new object outside of lock
            value = GetObject();
            
            // Re-acquire lock only to add to active instances
            lock (lockObj)
            {
                activeInstances[value] = requestPriority;
            }

            return true;
        }

        protected override void OnInstanceReleaseAction(T obj)
        {
            base.OnInstanceReleaseAction(obj);

            lock (lockObj)
            {
                activeInstances.Remove(obj);
            }
        }
        
        /// <summary>
        /// Gets the current number of active instances in the pool.
        /// </summary>
        public int ActiveCount
        {
            get
            {
                lock (lockObj)
                {
                    return activeInstances.Count;
                }
            }
        }
    }
}