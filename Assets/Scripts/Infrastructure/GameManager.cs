using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure.Systems;
using Infrastructure.UIManager;
using SpiralingStudio.Services.DataManagement;
using SpiralingStudio.Services.Session;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Infrastructure.Core
{
    /// <summary>
    /// Main game manager responsible for orchestrating the startup flow.
    /// Loads the loading screen, waits for essential services, then delegates to GameInitializer.
    /// </summary>
    public class GameManager : IAsyncStartable
    {
        private readonly MFSceneManager _sceneManager;
        private readonly IDbLoader _dbLoader;
        private readonly SessionManager _sessionManager;
        private readonly UiManager _uiManager;
        private readonly GameInitializer _gameInitializer;

        // Scene configuration constants
        private const string LoadingScreenName = "LoadingScreen";

        [Inject]
        public GameManager(
            MFSceneManager sceneManager, 
            IDbLoader dbLoader, 
            SessionManager sessionManager,
            UiManager uiManager,
            GameInitializer gameInitializer)
        {
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            _dbLoader = dbLoader ?? throw new ArgumentNullException(nameof(dbLoader));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _gameInitializer = gameInitializer ?? throw new ArgumentNullException(nameof(gameInitializer));
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            try
            {
                Debug.Log("[GameManager] Starting application...");

                // Initialize session with a default user (in production, this would come from authentication)
                _sessionManager.CreateSession("DefaultUser");
                
                // Load loading screen immediately (before service initialization completes)
                Debug.Log($"[GameManager] Loading {LoadingScreenName}...");
                var loadingScreen = await _uiManager.LoadContextFromSceneAsync(
                    LoadingScreenName, 
                    loadAdditive: true, 
                    pushPreviousToStack: false, 
                    cancellation);

                if (loadingScreen == null)
                {
                    Debug.LogWarning($"[GameManager] Failed to load {LoadingScreenName}. Continuing without loading screen.");
                }

                // Note: At this point, ServiceInitializer has already completed (it's an IAsyncStartable too)
                // The loading screen is shown during the tail end of service initialization
                
                Debug.Log("[GameManager] Essential services loaded. Starting game initialization...");
                
                // Unload loading screen via UiManager to maintain proper tracking
                if (loadingScreen != null)
                {
                    await _uiManager.UnloadContextSceneAsync(LoadingScreenName, cancellation);
                }
                
                // Delegate to GameInitializer for game-specific initialization
                await _gameInitializer.InitializeGameAsync(cancellation);

                Debug.Log("[GameManager] Application startup completed successfully.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[GameManager] Application startup was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameManager] Critical error during application startup: {ex}");
                throw;
            }
        }
    }
}