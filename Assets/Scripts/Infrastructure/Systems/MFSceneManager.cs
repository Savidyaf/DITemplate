using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using SpiralingStudio.Events;
using SpiralingStudio.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Infrastructure.Systems
{
    /// <summary>
    /// Service responsible for loading, unloading, and managing scenes using Addressables,
    /// organized into layers with stacking capabilities.
    /// </summary>
    public class MFSceneManager : IMFService
    {
        // Dependencies
        private readonly IAsyncPublisher<PreSceneLoadEvent> _preSceneLoadEventPublisher;
        private readonly IAsyncPublisher<PostSceneLoadEvent> _postSceneLoadEventPublisher;
        private readonly IAsyncPublisher<PreSceneUnloadEvent> _preSceneUnloadEventPublisher;
        private readonly LifetimeScope _parentLifetimeScope; // Use parent scope for scene instantiation context

        // State
        private readonly List<SceneLayerStack> _layerStacks;
        private const string AddressableSceneLabel = "Scene"; // Default label for scenes
        private const int DefaultLayerCount = 4;

        [Inject]
        public MFSceneManager(
            IAsyncPublisher<PreSceneLoadEvent> preSceneLoadEventPublisher,
            IAsyncPublisher<PostSceneLoadEvent> postSceneLoadEventPublisher,
            IAsyncPublisher<PreSceneUnloadEvent> preSceneUnloadEventPublisher,
            LifetimeScope lifetimeScope) // Inject the scope this manager lives in
        {
            _preSceneLoadEventPublisher = preSceneLoadEventPublisher ??
                                          throw new ArgumentNullException(nameof(preSceneLoadEventPublisher));
            _postSceneLoadEventPublisher = postSceneLoadEventPublisher ??
                                           throw new ArgumentNullException(nameof(postSceneLoadEventPublisher));
            _preSceneUnloadEventPublisher = preSceneUnloadEventPublisher ??
                                            throw new ArgumentNullException(nameof(preSceneUnloadEventPublisher));
            _parentLifetimeScope =
                lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope)); // Store the parent scope

            _layerStacks = new List<SceneLayerStack>(DefaultLayerCount);
            for (int i = 0; i < DefaultLayerCount; i++)
            {
                _layerStacks.Add(new SceneLayerStack());
            }

            Debug.Log("[MFSceneManager] Initialized.");
        }

        /// <summary>
        /// Loads a scene by its Addressable label onto a specified layer.
        /// Handles additive loading, replacing existing scenes, and pushing scenes onto a stack.
        /// Uses a transaction pattern to ensure state consistency on failure.
        /// </summary>
        /// <remarks>
        /// NOTE: All scenes are loaded using Unity's Additive mode internally to preserve the root
        /// GameLifetimeScope scene. The <paramref name="loadAdditive"/> parameter controls whether
        /// existing scenes on the target layer should be unloaded or kept, not Unity's load mode.
        /// </remarks>
        /// <param name="sceneLabel">The Addressable label of the scene to load.</param>
        /// <param name="sceneLayer">The layer index to load the scene onto.</param>
        /// <param name="loadAdditive">If true, keep existing scenes on the layer; if false, unload/replace them (unless pushPreviousToStack is true).</param>
        /// <param name="pushPreviousToStack">If true, existing active scenes on the layer are hidden and pushed onto the stack instead of being unloaded.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if the scene load sequence completed successfully, false otherwise.</returns>
        public async UniTask<bool> LoadScene(string sceneLabel, int sceneLayer, bool loadAdditive,
            bool pushPreviousToStack = false, CancellationToken cancellationToken = default)
        {
            EnsureLayerExists(sceneLayer);
            SceneLayerStack layer = _layerStacks[sceneLayer];
            
            // Phase 0: Prepare transaction (no state changes yet)
            SceneLoadTransaction transaction = PrepareLoadTransaction(layer, loadAdditive, pushPreviousToStack);

            Debug.Log(
                $"[MFSceneManager] Loading scene '{sceneLabel}' onto layer {sceneLayer}. Additive: {loadAdditive}, Push: {pushPreviousToStack}. " +
                $"Unloading {transaction.ScenesToUnload.Count} scenes, pushing {transaction.ScenesToPush.Count} scenes to stack.");

            try
            {
                // --- Phase 1: Pre-Load Events ---
                await _preSceneLoadEventPublisher.PublishAsync(new PreSceneLoadEvent(sceneLabel, sceneLayer),
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // --- Phase 2: Begin Transaction (visual changes only - hide scenes to push) ---
                BeginTransaction(transaction);

                // Start the async load but don't await the *full* load yet
                UniTask<(SceneRecord, AsyncOperationHandle<SceneInstance>)> loadSceneTask =
                    LoadSceneInternalAsync(sceneLabel, loadAdditive, cancellationToken);

                // Allow PreSceneLoadEvent handlers to execute
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cancellationToken);

                // --- Phase 3: Unload Previous Scenes (if any) ---
                if (transaction.ScenesToUnload.Count > 0)
                {
                    await PublishPreSceneUnloadEvents(transaction.ScenesToUnload, sceneLayer, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    await UnloadScenesInternalAsync(transaction.ScenesToUnload, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // --- Phase 4: Finalize Loading and Activate ---
                var (loadedSceneRecord, loadHandle) = await loadSceneTask;
                cancellationToken.ThrowIfCancellationRequested();

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError(
                        $"[MFSceneManager] Failed to load scene '{sceneLabel}'. Status: {loadHandle.Status}, Error: {loadHandle.OperationException}");
                    RollbackTransaction(transaction);
                    return false;
                }

                await ActivateSceneAsync(loadHandle, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // --- Phase 5: Commit Transaction (state changes) and Post-Load Event ---
                CommitTransaction(layer, transaction, loadedSceneRecord);
                Debug.Log(
                    $"[MFSceneManager] Scene '{sceneLabel}' activated and added to active scenes on layer {sceneLayer}.");

                await _postSceneLoadEventPublisher.PublishAsync(new PostSceneLoadEvent(sceneLabel, sceneLayer),
                    cancellationToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[MFSceneManager] Load operation for scene '{sceneLabel}' cancelled. Rolling back...");
                RollbackTransaction(transaction);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MFSceneManager] Exception during load sequence for scene '{sceneLabel}': {e}. Rolling back...");
                RollbackTransaction(transaction);
                return false;
            }
        }


        /// <summary>
        /// Explicitly unloads a specific scene from a given layer.
        /// </summary>
        /// <param name="sceneName">The name (usually Addressable label) of the scene to unload.</param>
        /// <param name="sceneLayer">The layer the scene resides on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the scene was found and unload initiated, false otherwise.</returns>
        public async UniTask<bool> UnloadScene(string sceneName, int sceneLayer,
            CancellationToken cancellationToken = default)
        {
            if (sceneLayer < 0 || sceneLayer >= _layerStacks.Count)
            {
                Debug.LogError($"[MFSceneManager] Invalid scene layer {sceneLayer} for unloading '{sceneName}'.");
                return false;
            }

            SceneLayerStack layer = _layerStacks[sceneLayer];
            SceneRecord? sceneToUnload = layer.FindActiveScene(sceneName);

            if (!sceneToUnload.HasValue)
            {
                Debug.LogWarning(
                    $"[MFSceneManager] Scene '{sceneName}' not found active on layer {sceneLayer}. Cannot unload.");
                return false; // Or true if "not found" means "already unloaded"
            }

            Debug.Log($"[MFSceneManager] Unloading scene '{sceneName}' from layer {sceneLayer}.");
            try
            {
                await PublishPreSceneUnloadEvents(new[] { sceneToUnload.Value }, sceneLayer, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                await UnloadScenesInternalAsync(new[] { sceneToUnload.Value }, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                layer.RemoveActiveScene(sceneName); // Update state *after* successful unload
                Debug.Log($"[MFSceneManager] Scene '{sceneName}' unloaded and removed from layer {sceneLayer}.");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[MFSceneManager] Unload operation for scene '{sceneName}' cancelled.");
                // State might be inconsistent (scene might still be loaded but removed from tracking?)
                // Consider re-adding to tracking if unload fails?
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MFSceneManager] Exception during unload sequence for scene '{sceneName}': {e}");
                // State might be inconsistent
                return false;
            }
        }


        public bool CanNavigateToPreviousScene(int layer)
        {
            EnsureLayerExists(layer);
            return _layerStacks[layer].SceneCountInStack() > 0;
        }

        public async UniTask NavigateToPreviousScene(int sceneLayer, CancellationToken cancellationToken = default)
        {
            EnsureLayerExists(sceneLayer);
            SceneLayerStack layer = _layerStacks[sceneLayer];

            if (layer.SceneCountInStack() == 0)
            {
                Debug.LogWarning($"[MFSceneManager] No previous scene to navigate to on layer {sceneLayer}.");
                return;
            }

            // Capture current state for potential rollback
            List<SceneRecord> scenesToUnload = new List<SceneRecord>(layer.GetActiveScenes());
            SceneRecord sceneToShow = layer.GetNextSceneFromStack(); // Peek at the scene to show
            bool stateCommitted = false;

            Debug.Log(
                $"[MFSceneManager] Navigating back on layer {sceneLayer}. Unloading {scenesToUnload.Count}, showing '{sceneToShow.SceneName}'.");

            try
            {
                // --- Phase 1: Pre-Unload Events ---
                if (scenesToUnload.Count > 0)
                {
                    await PublishPreSceneUnloadEvents(scenesToUnload, sceneLayer, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // --- Phase 2: Pre-Load Event (for the scene being shown) ---
                await _preSceneLoadEventPublisher.PublishAsync(new PreSceneLoadEvent(sceneToShow.SceneName, sceneLayer),
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cancellationToken);

                // --- Phase 3: Unload Current Scenes ---
                if (scenesToUnload.Count > 0)
                {
                    await UnloadScenesInternalAsync(scenesToUnload, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // --- Phase 4: Commit State Changes ---
                // Pop from stack and update active list
                var poppedScene = layer.PopFromStack();
                if (poppedScene.HasValue)
                {
                    layer.ClearActiveScenes();
                    layer.AddActiveScene(poppedScene.Value);
                    ShowSceneRootObjects(poppedScene.Value);
                    stateCommitted = true;
                }

                // --- Phase 5: Post-Load Event ---
                await _postSceneLoadEventPublisher.PublishAsync(
                    new PostSceneLoadEvent(sceneToShow.SceneName, sceneLayer), cancellationToken);
                Debug.Log(
                    $"[MFSceneManager] Navigation back complete on layer {sceneLayer}. '{sceneToShow.SceneName}' is now active.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[MFSceneManager] Navigate back operation on layer {sceneLayer} cancelled.");
                if (!stateCommitted)
                {
                    Debug.Log("[MFSceneManager] State was not committed - no rollback needed for navigate back.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MFSceneManager] Exception during navigate back on layer {sceneLayer}: {e}");
                if (!stateCommitted)
                {
                    Debug.Log("[MFSceneManager] State was not committed - no rollback needed for navigate back.");
                }
            }
        }


        public UniTask[] GetInitializeTasks() => Array.Empty<UniTask>();


        // --- Transaction Pattern for State Consistency ---

        /// <summary>
        /// Represents a scene load operation that can be committed or rolled back.
        /// Captures what needs to happen without modifying state.
        /// </summary>
        private struct SceneLoadTransaction
        {
            /// <summary>Scenes that will be unloaded (not pushed to stack).</summary>
            public List<SceneRecord> ScenesToUnload;
            
            /// <summary>Scenes that will be hidden and pushed to stack (not unloaded).</summary>
            public List<SceneRecord> ScenesToPush;
            
            /// <summary>Whether the active scenes list should be cleared on commit.</summary>
            public bool ClearActiveOnCommit;
            
            /// <summary>Whether the transaction has begun (visual changes applied).</summary>
            public bool HasBegun;
        }

        /// <summary>
        /// Prepares a load transaction by identifying what needs to happen.
        /// Does NOT modify any state - only captures the operation plan.
        /// </summary>
        private SceneLoadTransaction PrepareLoadTransaction(SceneLayerStack layer, bool loadAdditive, bool pushPreviousToStack)
        {
            var transaction = new SceneLoadTransaction
            {
                ScenesToUnload = new List<SceneRecord>(),
                ScenesToPush = new List<SceneRecord>(),
                ClearActiveOnCommit = false,
                HasBegun = false
            };

            if (!loadAdditive) // Replacing scenes on the layer
            {
                if (pushPreviousToStack)
                {
                    // Scenes will be pushed to stack (hidden, not unloaded)
                    transaction.ScenesToPush.AddRange(layer.GetActiveScenes());
                    transaction.ClearActiveOnCommit = true;
                }
                else
                {
                    // Scenes will be unloaded
                    transaction.ScenesToUnload.AddRange(layer.GetActiveScenes());
                    transaction.ClearActiveOnCommit = true;
                }
            }
            else // Additive load
            {
                if (pushPreviousToStack)
                {
                    // Scenes will be pushed to stack (hidden, not unloaded)
                    transaction.ScenesToPush.AddRange(layer.GetActiveScenes());
                    transaction.ClearActiveOnCommit = true;
                }
                // If additive and not pushing, nothing changes for existing scenes
            }

            return transaction;
        }

        /// <summary>
        /// Begins the transaction by applying visual changes (hiding scenes to push).
        /// State is NOT modified yet - this is recoverable via rollback.
        /// </summary>
        private void BeginTransaction(SceneLoadTransaction transaction)
        {
            transaction.HasBegun = true;
            
            // Hide scenes that will be pushed to stack (visual change only)
            foreach (var scene in transaction.ScenesToPush)
            {
                HideSceneRootObjects(scene);
            }
        }

        /// <summary>
        /// Commits the transaction after successful load.
        /// This is where actual state changes happen.
        /// </summary>
        private void CommitTransaction(SceneLayerStack layer, SceneLoadTransaction transaction, SceneRecord newScene)
        {
            // Push scenes to stack (state change)
            foreach (var scene in transaction.ScenesToPush)
            {
                layer.PushSceneToStack(scene);
            }

            // Clear active scenes if needed
            if (transaction.ClearActiveOnCommit)
            {
                layer.ClearActiveScenes();
            }

            // Add the new scene to active
            layer.AddActiveScene(newScene);
        }

        /// <summary>
        /// Rolls back the transaction by undoing visual changes.
        /// Called when an error occurs or operation is cancelled.
        /// </summary>
        private void RollbackTransaction(SceneLoadTransaction transaction)
        {
            if (!transaction.HasBegun)
            {
                // Nothing to rollback - transaction never started
                return;
            }

            Debug.Log("[MFSceneManager] Rolling back transaction - restoring hidden scenes...");

            // Show scenes that were hidden (undo visual changes)
            foreach (var scene in transaction.ScenesToPush)
            {
                ShowSceneRootObjects(scene);
            }
        }

        // --- Scene Visibility Helpers (moved from SceneLayerStack for transaction access) ---

        private void HideSceneRootObjects(SceneRecord scene)
        {
            if (!scene.Handle.IsValid() || !scene.Handle.Result.Scene.IsValid()) return;
            foreach (var go in scene.Handle.Result.Scene.GetRootGameObjects())
            {
                go.SetActive(false);
            }
        }

        private void ShowSceneRootObjects(SceneRecord scene)
        {
            if (!scene.Handle.IsValid() || !scene.Handle.Result.Scene.IsValid()) return;
            foreach (var go in scene.Handle.Result.Scene.GetRootGameObjects())
            {
                go.SetActive(true);
            }
        }

        // --- Other Internal Helper Methods ---

        private async UniTask<(SceneRecord, AsyncOperationHandle<SceneInstance>)> LoadSceneInternalAsync(
            string sceneLabel, bool loadAdditive, CancellationToken cancellationToken)
        {
            // Find Addressable location
            var locationsHandle = Addressables.LoadResourceLocationsAsync(
                new List<string> { AddressableSceneLabel, sceneLabel }, // Use keys
                Addressables.MergeMode.Intersection, // Intersection finds items with ALL keys
                typeof(SceneInstance));

            await locationsHandle.ToUniTask(cancellationToken: cancellationToken);

            if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result == null ||
                locationsHandle.Result.Count == 0)
            {
                Addressables.Release(locationsHandle);
                throw new Exception(
                    $"[MFSceneManager] No Addressable scene location found for label combination: '{AddressableSceneLabel}' AND '{sceneLabel}'");
            }

            var sceneLocation = locationsHandle.Result[0];
            Addressables.Release(locationsHandle); // Release location handle once we have the location

            // IMPORTANT: ALWAYS use LoadSceneMode.Additive!
            // The layer system manages which scenes to unload - we should never use Single mode
            // as it would unload the root GameLifetimeScope scene and destroy all services.
            // The 'loadAdditive' parameter controls whether we unload scenes on the same layer,
            // not whether Unity should unload all other scenes.
            var loadSceneHandle =
                Addressables.LoadSceneAsync(sceneLocation, LoadSceneMode.Additive, activateOnLoad: false);

            // Wait for the scene to load (not activate)
            // NOTE: EnqueueParent is NOT used here because LifetimeScope.Awake() runs during ACTIVATION, not load
            await loadSceneHandle.ToUniTask(cancellationToken: cancellationToken);

            return (new SceneRecord(sceneLabel, loadSceneHandle), loadSceneHandle);
        }

        private async UniTask PublishPreSceneUnloadEvents(IEnumerable<SceneRecord> scenesToUnload, int sceneLayer,
            CancellationToken cancellationToken)
        {
            var tasks = new List<UniTask>();
            foreach (var scene in scenesToUnload)
            {
                tasks.Add(_preSceneUnloadEventPublisher.PublishAsync(
                    new PreSceneUnloadEvent(scene.SceneName, sceneLayer), cancellationToken));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask UnloadScenesInternalAsync(IEnumerable<SceneRecord> scenesToUnload,
            CancellationToken cancellationToken)
        {
            foreach (var sceneRecord in scenesToUnload)
            {
                var asyncOperationHandle = sceneRecord.Handle;
                if (!asyncOperationHandle.IsValid())
                {
                    Debug.LogWarning($"[MFSceneManager] Invalid handle for scene '{sceneRecord.SceneName}'. Skipping.");
                    continue;
                }

                Debug.Log($"[MFSceneManager] Unloading scene '{sceneRecord.SceneName}'...");

                try
                {
                    // Get the scene reference before unloading
                    Scene sceneToUnload = default;
                    if (asyncOperationHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        sceneToUnload = asyncOperationHandle.Result.Scene;
                    }

                    if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
                    {
                        // Safety check: Unity won't unload the last scene
                        if (SceneManager.sceneCount <= 1)
                        {
                            Debug.LogError($"[MFSceneManager] Cannot unload '{sceneRecord.SceneName}' - it's the last scene!");
                            Addressables.Release(asyncOperationHandle);
                            continue;
                        }

                        // Start the unload operation (fire-and-forget style)
                        // We don't await this because it can hang indefinitely in some cases.
                        // Instead, we just start it and release the Addressables handle.
                        SceneManager.UnloadSceneAsync(sceneToUnload);

                        // Give Unity a frame to process the unload request
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                        // Release the Addressables handle
                        if (asyncOperationHandle.IsValid())
                        {
                            Addressables.Release(asyncOperationHandle);
                        }
                    }
                    else
                    {
                        // Scene not valid/loaded, just release the handle
                        if (asyncOperationHandle.IsValid())
                        {
                            Addressables.Release(asyncOperationHandle);
                        }
                    }

                    Debug.Log($"[MFSceneManager] Scene '{sceneRecord.SceneName}' unload initiated.");
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning($"[MFSceneManager] Unload cancelled for scene '{sceneRecord.SceneName}'.");
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MFSceneManager] Exception unloading scene '{sceneRecord.SceneName}': {ex}");
                    // Try to release the handle even if unload failed
                    try { if (asyncOperationHandle.IsValid()) Addressables.Release(asyncOperationHandle); }
                    catch { /* Ignore */ }
                }
            }
        }

        private async UniTask ActivateSceneAsync(AsyncOperationHandle<SceneInstance> handle,
            CancellationToken cancellationToken)
        {
            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[MFSceneManager] Cannot activate scene with invalid or failed handle.");
                return;
            }

            var sceneInstance = handle.Result;

            // Check if scene has valid root objects that are already active - indicates scene is activated
            // NOTE: Scene.isLoaded returns true as soon as scene DATA is loaded, even before activation.
            // We need to check the activation state by examining root object activity.
            if (sceneInstance.Scene.IsValid())
            {
                var rootObjects = sceneInstance.Scene.GetRootGameObjects();
                if (rootObjects.Length > 0 && rootObjects[0].activeInHierarchy)
                {
                    // Scene root objects are already active - scene was likely loaded with activateOnLoad: true
                    // or has already been activated. Skip re-activation.
                    Debug.LogWarning(
                        $"[MFSceneManager] Scene '{sceneInstance.Scene.name}' root objects are already active. Skipping activation.");
                    return;
                }
            }

            Debug.Log($"[MFSceneManager] Activating scene '{sceneInstance.Scene.name}'...");

            // IMPORTANT: Wrap activation with EnqueueParent because LifetimeScope.Awake() runs
            // during scene activation, which is when the VContainer builds the container.
            // This ensures child LifetimeScopes can resolve services from the parent scope.
            using (LifetimeScope.EnqueueParent(parent: _parentLifetimeScope))
            {
                var activation = sceneInstance.ActivateAsync();
                await activation.ToUniTask(cancellationToken: cancellationToken);

                // Wait one more frame AFTER activation for VContainer scopes to fully initialize
                // and run Start() methods within the EnqueueParent scope.
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cancellationToken);
            }

            Debug.Log($"[MFSceneManager] Scene '{sceneInstance.Scene.name}' activated successfully.");
        }

        private void EnsureLayerExists(int layerIndex)
        {
            while (layerIndex >= _layerStacks.Count)
            {
                _layerStacks.Add(new SceneLayerStack());
            }
        }

        // --- Nested Types ---

        /// <summary>Represents a loaded scene instance tracked by the manager.</summary>
        private readonly struct SceneRecord
        {
            public readonly string SceneName; // Usually the Addressable label used to load it
            public readonly AsyncOperationHandle<SceneInstance> Handle;

            public SceneRecord(string sceneName, AsyncOperationHandle<SceneInstance> handle)
            {
                SceneName = sceneName;
                Handle = handle;
            }
        }

        /// <summary>Manages active scenes and a stack of previously active scenes for a single layer.</summary>
        private class SceneLayerStack
        {
            private readonly Stack<SceneRecord> _sceneStack = new();
            private readonly List<SceneRecord> _activeScenes = new();

            public void AddActiveScene(SceneRecord scene) => _activeScenes.Add(scene);
            public IReadOnlyList<SceneRecord> GetActiveScenes() => _activeScenes;
            public void ClearActiveScenes() => _activeScenes.Clear(); // Only clears the list, doesn't unload

            public SceneRecord? FindActiveScene(string sceneName)
            {
                int index = _activeScenes.FindIndex(sr =>
                    sr.SceneName != null && sr.SceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 ? _activeScenes[index] : null;
            }

            public bool RemoveActiveScene(string sceneName)
            {
                int index = _activeScenes.FindIndex(sr =>
                    sr.SceneName != null && sr.SceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase));
                if (index != -1)
                {
                    _activeScenes.RemoveAt(index);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Pushes a single scene to the stack without modifying active scenes or visibility.
            /// Used by the transaction pattern where visibility is managed separately.
            /// </summary>
            public void PushSceneToStack(SceneRecord scene)
            {
                _sceneStack.Push(scene);
            }

            public int SceneCountInStack() => _sceneStack.Count;

            public SceneRecord GetNextSceneFromStack() => _sceneStack.Peek(); // Just look without popping

            /// <summary>
            /// Pops a scene from the stack and makes it active.
            /// Returns the scene that was popped, or null if stack was empty.
            /// Does NOT modify visibility - caller is responsible for that.
            /// </summary>
            public SceneRecord? PopFromStack()
            {
                if (_sceneStack.Count == 0) return null;
                return _sceneStack.Pop();
            }
        }
    }
}