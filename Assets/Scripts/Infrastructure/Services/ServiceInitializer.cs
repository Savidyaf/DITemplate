using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SpiralingStudio.Services
{
    public class ServiceInitializer : IAsyncStartable
    {
        private readonly IReadOnlyList<IMFService> mfServices;

        [Inject]
        public ServiceInitializer(IEnumerable<IMFService> mfServices)
        {
            this.mfServices = mfServices?.ToArray() ?? Array.Empty<IMFService>();
        }

        private List<UniTask> GetInitializationTasks()
        {
            var initializationTasks = new List<UniTask>(capacity: mfServices.Count);
            for (int i = 0; i < mfServices.Count; i++)
            {
                IMFService mfService = mfServices[i];
                var tasks = mfService?.GetInitializeTasks();
                if (tasks == null || tasks.Length == 0) continue;
                initializationTasks.AddRange(tasks);
            }
            return initializationTasks;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            try
            {
                var tasks = GetInitializationTasks();
                if (tasks.Count == 0)
                {
                    Debug.Log("[ServiceInitializer] No initialization tasks to execute.");
                    return;
                }

                Debug.Log($"[ServiceInitializer] Starting initialization of {tasks.Count} tasks...");
                await UniTask.WhenAll(tasks).AttachExternalCancellation(cancellation);
                Debug.Log("[ServiceInitializer] All services initialized successfully.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[ServiceInitializer] Service initialization was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServiceInitializer] Service initialization failed: {ex}");
                throw;
            }
        }
    }
}