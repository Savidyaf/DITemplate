using System;

namespace SpiralingStudio.Utils
{
    public class MfManagedObjectPool<T> : MfObjectPool<T> where T : class, IMfPooledObject
    {
        public new void InitializeObjectPool(Func<T> createObject, Action<T> getAction,
            Action<T> releaseAction, Action<T> destroyAction, int maxCapacity,
            int defaultCapacity = 10, bool collectionChecks = true)
        {
            base.InitializeObjectPool(createObject, getAction, releaseAction, destroyAction,
                maxCapacity, defaultCapacity, collectionChecks);
        }

        protected override void OnInstanceGetAction(T obj)
        {
            base.OnInstanceGetAction(obj);
            obj.OnInstanceGetFromPool();
        }

        protected override T CreateObjectInstance()
        {
            T objectInstance = base.CreateObjectInstance();
            objectInstance.OnInstanceCreated();
            return objectInstance;
        }

        protected override void OnInstanceReleaseAction(T obj)
        {
            base.OnInstanceReleaseAction(obj);
            obj.OnReturnedToPool();
        }

        protected override void OnInstanceDestroyAction(T obj)
        {
            base.OnInstanceDestroyAction(obj);
            obj.OnDestroyed();
        }
    }
}