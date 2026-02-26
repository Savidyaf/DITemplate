using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure.Systems;
using SpiralingStudio.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Infrastructure.UIManager
{
    /// <summary>
    /// Manages the lifecycle of different UI Contexts (e.g., loading/unloading scenes, instantiating prefabs).
    /// Acts as a central point for controlling which major UI sections are active.
    /// Uses MFSceneManager for scene operations and searches for UiContext after scene load.
    /// </summary>
    public class UiManager : IMFService
    {
        private readonly IObjectResolver _resolver;
        private readonly MFSceneManager _sceneManager;

        // Track active contexts by scene name
        private readonly Dictionary<string, UiContext> _activeSceneContexts = new Dictionary<string, UiContext>();

        // Track prefab instances created via InstantiateContextFromPrefabAsync
        private readonly List<UiContext> _activePrefabContexts = new List<UiContext>();

        // Define a default layer for UI scenes loaded via UiManager
        // Layer 2 is the UI overlay layer (see MFSceneManager documentation)
        private const int DefaultUiLayer = 2;

        [Inject]
        public UiManager(IObjectResolver resolver, MFSceneManager sceneManager)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            Debug.Log("[UiManager] Initialized.");
        }

        /// <summary>IMFService initialization task.</summary>
        public UniTask[] GetInitializeTasks() => new[] { UiWarmup() };

        private async UniTask UiWarmup()
        {
            Debug.Log("[UiManager] Warming up...");
            await UniTask.DelayFrame(1); // Placeholder
            Debug.Log("[UiManager] Warmup Complete.");
        }

        // --- API for Managing UI Contexts ---

        /// <summary>
        /// Loads a Unity scene containing a UiContext onto the default UI layer.
        /// After scene load, searches for UiContext and initializes it.
        /// </summary>
        /// <param name="sceneName">The Addressable label/name of the scene to load.</param>
        /// <param name="loadAdditive">Load additively or replace existing on layer?</param>
        /// <param name="pushPreviousToStack">Push existing scenes on layer to stack?</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The UiContext instance if successfully loaded and initialized, otherwise null.</returns>
        public async UniTask<UiContext> LoadContextFromSceneAsync(
            string sceneName,
            bool loadAdditive = true,
            bool pushPreviousToStack = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[UiManager] Scene name cannot be null or empty.");
                return null;
            }

            // Check if already tracked
            if (_activeSceneContexts.TryGetValue(sceneName, out var existingContext))
            {
                Debug.LogWarning($"[UiManager] Scene '{sceneName}' is already tracked as active. Returning existing context.");
                return existingContext;
            }

            try
            {
                Debug.Log($"[UiManager] Requesting scene load: '{sceneName}', Additive: {loadAdditive}, Push: {pushPreviousToStack}, Layer: {DefaultUiLayer}");

                // Step 1: Load scene via MFSceneManager
                bool success = await _sceneManager.LoadScene(sceneName, DefaultUiLayer, loadAdditive, pushPreviousToStack, cancellationToken);

                if (!success)
                {
                    Debug.LogError($"[UiManager] Failed to load scene '{sceneName}' via MFSceneManager.");
                    return null;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Step 2: Find UiContext in the loaded scene (deterministic, no waiting)
                UiContext context = FindUiContextInScene(sceneName);

                if (context == null)
                {
                    Debug.LogWarning($"[UiManager] Scene '{sceneName}' loaded but no UiContext found. This scene may not have a UI context.");
                    return null;
                }

                // Step 3: Initialize the context (includes batch localization)
                await context.InitializeContextAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 4: Track the context
                _activeSceneContexts[sceneName] = context;
                Debug.Log($"[UiManager] Scene '{sceneName}' loaded and context initialized successfully.", context);

                return context;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[UiManager] Scene load cancelled for '{sceneName}'.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UiManager] Exception during scene load for '{sceneName}': {ex}");
                throw;
            }
        }

        /// <summary>
        /// Finds a UiContext component in a loaded scene by name.
        /// Searches all root GameObjects in the scene.
        /// Falls back to searching all loaded scenes if exact name match fails.
        /// </summary>
        /// <param name="sceneName">The name of the scene to search in (may be Addressable label or actual scene name).</param>
        /// <returns>The UiContext if found, otherwise null.</returns>
        private UiContext FindUiContextInScene(string sceneName)
        {
            // First, try to find the scene by exact name
            Scene scene = SceneManager.GetSceneByName(sceneName);
            
            if (scene.IsValid() && scene.isLoaded)
            {
                var context = SearchSceneForUiContext(scene);
                if (context != null)
                {
                    Debug.Log($"[UiManager] Found UiContext '{context.GetType().Name}' in scene '{sceneName}'.", context);
                    return context;
                }
            }

            // Fallback: Search all loaded scenes for a UiContext that isn't already tracked
            // This handles cases where the Addressable label differs from the actual scene name
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                
                // Skip scenes we're already tracking
                if (_activeSceneContexts.ContainsKey(scene.name)) continue;
                
                var context = SearchSceneForUiContext(scene);
                if (context != null)
                {
                    Debug.Log($"[UiManager] Found UiContext '{context.GetType().Name}' in scene '{scene.name}' (searched by fallback for '{sceneName}').", context);
                    return context;
                }
            }

            Debug.LogWarning($"[UiManager] No UiContext found in scene '{sceneName}' or any untracked loaded scene.");
            return null;
        }

        /// <summary>
        /// Searches a specific scene for a UiContext component.
        /// </summary>
        private UiContext SearchSceneForUiContext(Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var context = rootObject.GetComponentInChildren<UiContext>(includeInactive: true);
                if (context != null)
                {
                    return context;
                }
            }
            return null;
        }

        /// <summary>
        /// Unloads an additively loaded scene containing a UiContext from the default UI layer.
        /// </summary>
        /// <param name="sceneName">The name of the scene to unload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if unloading was successful or scene wasn't loaded, false otherwise.</returns>
        public async UniTask<bool> UnloadContextSceneAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[UiManager] Scene name cannot be null or empty for unload.");
                return false;
            }

            // Remove from tracking (if present)
            if (_activeSceneContexts.ContainsKey(sceneName))
            {
                _activeSceneContexts.Remove(sceneName);
                Debug.Log($"[UiManager] Removed '{sceneName}' from active contexts.");
            }
            else
            {
                Debug.LogWarning($"[UiManager] Scene '{sceneName}' is not tracked as active. Proceeding with unload anyway.");
            }

            Debug.Log($"[UiManager] Requesting scene unload: '{sceneName}', Layer: {DefaultUiLayer}");

            bool success = await _sceneManager.UnloadScene(sceneName, DefaultUiLayer, cancellationToken);

            if (!success)
            {
                Debug.LogError($"[UiManager] MFSceneManager failed to unload scene: '{sceneName}'.");
            }
            else
            {
                Debug.Log($"[UiManager] Scene '{sceneName}' unloaded successfully via MFSceneManager.");
            }

            return success;
        }

        /// <summary>
        /// Instantiates a UiContext from a prefab under a specified parent.
        /// </summary>
        /// <param name="contextPrefab">The UiContext prefab.</param>
        /// <param name="parent">The parent transform for the instance.</param>
        /// <param name="worldPositionStays">If true, maintain world position.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The instantiated UiContext, or null on failure.</returns>
        public async UniTask<UiContext> InstantiateContextFromPrefabAsync(
            UiContext contextPrefab, 
            Transform parent,
            bool worldPositionStays = false, 
            CancellationToken cancellationToken = default)
        {
            if (contextPrefab == null)
            {
                Debug.LogError("[UiManager] Prefab null.");
                return null;
            }

            if (_resolver == null)
            {
                Debug.LogError("[UiManager] Resolver null.");
                return null;
            }

            Debug.Log($"[UiManager] Instantiating prefab: {contextPrefab.name}");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                UiContext instance = _resolver.Instantiate(contextPrefab, parent, worldPositionStays);
                if (instance != null)
                {
                    _activePrefabContexts.Add(instance);
                    
                    // Initialize the prefab context
                    await instance.InitializeContextAsync(cancellationToken);
                    
                    Debug.Log($"[UiManager] Instantiated and initialized '{instance.GetType().Name}' from prefab.", instance);
                    return instance;
                }
                else
                {
                    Debug.LogError($"[UiManager] Failed to instantiate prefab: {contextPrefab.name}");
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[UiManager] Prefab instantiation cancelled: {contextPrefab.name}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UiManager] Exception instantiating prefab '{contextPrefab.name}': {e}");
                return null;
            }
        }

        /// <summary>
        /// Destroys a UiContext instance that was created from a prefab.
        /// </summary>
        /// <param name="contextInstance">The UiContext instance to destroy.</param>
        /// <returns>True if destroyed successfully or was not tracked, false otherwise.</returns>
        public bool DestroyPrefabContext(UiContext contextInstance)
        {
            if (contextInstance == null)
            {
                Debug.LogWarning("[UiManager] Cannot destroy null instance.");
                return true;
            }

            if (!_activePrefabContexts.Contains(contextInstance))
            {
                Debug.LogWarning($"[UiManager] Instance '{contextInstance.name}' not tracked.", contextInstance);
                return true;
            }

            Debug.Log($"[UiManager] Destroying prefab instance: {contextInstance.name}", contextInstance);
            try
            {
                _activePrefabContexts.Remove(contextInstance);
                UnityEngine.Object.Destroy(contextInstance.gameObject);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UiManager] Exception destroying instance '{contextInstance.name}': {e}", contextInstance);
                return false;
            }
        }

        // --- Getters ---
        
        /// <summary>
        /// Gets the active UiContext for a given scene name.
        /// </summary>
        public UiContext GetActiveContextForScene(string sceneName)
        {
            _activeSceneContexts.TryGetValue(sceneName, out var context);
            return context;
        }

        /// <summary>
        /// Gets a read-only list of all active prefab contexts.
        /// </summary>
        public IReadOnlyList<UiContext> GetActivePrefabContexts() => _activePrefabContexts;

        /// <summary>
        /// Gets a read-only dictionary of all active scene contexts.
        /// </summary>
        public IReadOnlyDictionary<string, UiContext> GetActiveSceneContexts() => _activeSceneContexts;
    }
}
