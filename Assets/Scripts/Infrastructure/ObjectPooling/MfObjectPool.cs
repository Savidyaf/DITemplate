using System;
using UnityEngine.Pool;

namespace SpiralingStudio.Utils
{
    public abstract class MfObjectPool<T> where T : class
    {
        private ObjectPool<T> objectPoolInstance;
        private Func<T> createObject;
        private Action<T> getAction;
        private Action<T> releaseAction;
        private Action<T> destroyAction;

        protected void InitializeObjectPool(Func<T> createObject, Action<T> getAction,
            Action<T> releaseAction, Action<T> destroyAction, int maxCapacity, int defaultCapacity = 10,
            bool collectionChecks = true)
        {
            this.createObject = createObject ?? throw new ArgumentNullException(nameof(createObject), "The createObject delegate cannot be null.");
            this.getAction = getAction;
            this.releaseAction = releaseAction;
            this.destroyAction = destroyAction;

            objectPoolInstance = new ObjectPool<T>(CreateObjectInstance, OnInstanceGetAction, OnInstanceReleaseAction,
                OnInstanceDestroyAction, collectionChecks, defaultCapacity, maxCapacity);
        }

        protected virtual T GetObject()
        {
            return objectPoolInstance.Get();
        }

        protected virtual void ReleaseObject(T obj)
        {
            objectPoolInstance.Release(obj);
        }

        protected virtual void OnInstanceDestroyAction(T obj)
        {
            destroyAction?.Invoke(obj);
        }

        protected virtual void OnInstanceReleaseAction(T obj)
        {
            releaseAction?.Invoke(obj);
        }

        protected virtual void OnInstanceGetAction(T obj)
        {
            getAction?.Invoke(obj);
        }

        protected virtual T CreateObjectInstance()
        {
            return createObject.Invoke();
        }
    }
}