using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Infrastructure.Systems;
using VContainer;
using VContainer.Unity;

namespace MonsterFactory.Services
{
    public class ServiceInitializer : IInitializable
    {
        private readonly List<Type> lifetimeScope;
        private readonly IObjectResolver objectResolver;

        [Inject]
        public ServiceInitializer(GameLifetimeScope lifetimeScope, IObjectResolver objectResolver)
        {
            this.lifetimeScope = lifetimeScope.LifetimeServices;
            this.objectResolver = objectResolver;
        }
        
        public List<UniTask> GetInitializationTasks()
        {
            List<UniTask> initializationTasks = new List<UniTask>();
            foreach (Type type in lifetimeScope)
            {
                object instance = objectResolver.Resolve(type);
                if (instance is IMFService mfService)
                {
                    initializationTasks.AddRange(mfService.GetInitializeTasks());
                }
            }
            return initializationTasks;
        }

        public async void Initialize()
        {
            await UniTask.WhenAll(GetInitializationTasks());
        }
    }
}