
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure.Systems;
using Infrastructure.UIManager;
using MessagePipe;
using SpiralingStudio.Events;
using SpiralingStudio.Services.Localization;
using SpiralingStudio.Services.PlayerPreferences;
using UnityEngine;
using VContainer;

namespace Infrastructure.Core
{
    /// <summary>
    /// Handles game-specific initialization after infrastructure services have been loaded.
    /// Shows a progress screen and executes initialization steps sequentially.
    /// </summary>
    public class GameInitializer
    {
        private readonly PlayerPreferencesService _preferencesService;
        private readonly MFLocalizationService _localizationService;
        private readonly UiManager _uiManager;
        private readonly MFSceneManager _sceneManager;
        private readonly IPublisher<GameInitializationProgressEvent> _progressPublisher;
        
        private const string GameInitScreenName = "GameInitializationScreen";
        private const string MainMenuSceneName = "MainMenu";
        private const int MainMenuLayer = 1; // WorldScene layer - main menu replaces world
        
        [Inject]
        public GameInitializer(
            PlayerPreferencesService preferencesService,
            MFLocalizationService localizationService,
            UiManager uiManager,
            MFSceneManager sceneManager,
            IPublisher<GameInitializationProgressEvent> progressPublisher)
        {
            _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
            _progressPublisher = progressPublisher ?? throw new ArgumentNullException(nameof(progressPublisher));
        }
        
        /// <summary>
        /// Executes the game initialization flow with progress tracking.
        /// </summary>
        public async UniTask InitializeGameAsync(CancellationToken cancellation = default)
        {
            try
            {
                Debug.Log("[GameInitializer] Starting game initialization...");
                
                // Load the game initialization screen (with progress bar)
                var initScreen = await _uiManager.LoadContextFromSceneAsync(
                    GameInitScreenName,
                    loadAdditive: true,
                    pushPreviousToStack: false,
                    cancellation);
                
                if (initScreen == null)
                {
                    Debug.LogWarning("[GameInitializer] Failed to load initialization screen. Continuing without progress UI.");
                }
                
                // Step 1: Load Player Preferences (already loaded during service init, but we report it here)
                #region Step1
                await PublishProgress(0.25f, "ui_init_step_loadprefs", cancellation);
                Debug.Log("[GameInitializer] Step 1/4: Player preferences loaded.");
                await UniTask.Delay(TimeSpan.FromSeconds(0.1), cancellationToken: cancellation); // Brief delay for visual feedback
                #endregion
                
                // Step 2: Apply Player Preferences
                #region Step2
                await PublishProgress(0.50f, "ui_init_step_applyprefs", cancellation);
                _preferencesService.ApplyPreferences();
                Debug.Log("[GameInitializer] Step 2/4: Player preferences applied.");
                await UniTask.Delay(TimeSpan.FromSeconds(0.1), cancellationToken: cancellation);
                #endregion
                
                // Step 3: Load Essential Main Menu Assets (placeholder - extend as needed)
                #region Step3
                await PublishProgress(0.75f, "ui_init_step_loadassets", cancellation);
                await LoadEssentialAssets(cancellation);
                Debug.Log("[GameInitializer] Step 3/4: Essential assets loaded.");
                await UniTask.Delay(TimeSpan.FromSeconds(0.1), cancellationToken: cancellation);
                #endregion
                
                // Step 4: Load Main Menu Scene\
                #region Step4
                await PublishProgress(1.0f, "ui_init_step_loadmenu", cancellation);
                bool mainMenuLoaded = await _sceneManager.LoadScene(
                    MainMenuSceneName,
                    MainMenuLayer,
                    loadAdditive: false, // Replace WorldScene with MainMenu
                    pushPreviousToStack: false,
                    cancellation);
                if (!mainMenuLoaded)
                {
                    throw new Exception("Failed to load Main Menu scene.");
                }
                
                Debug.Log("[GameInitializer] Step 4/4: Main Menu loaded.");
                await UniTask.Delay(TimeSpan.FromSeconds(0.5), cancellationToken: cancellation);
                #endregion
                
                // Unload the initialization screen via UiManager to maintain proper tracking
                if (initScreen != null)
                {
                    await _uiManager.UnloadContextSceneAsync(GameInitScreenName, cancellation);
                }
                
                Debug.Log("[GameInitializer] Game initialization completed successfully.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[GameInitializer] Game initialization was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameInitializer] Critical error during game initialization: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Loads essential assets needed for the main menu.
        /// Placeholder for future asset loading logic (Addressables, etc.).
        /// </summary>
        private async UniTask LoadEssentialAssets(CancellationToken cancellation)
        {
            // Placeholder: In a real implementation, you would load:
            // - Main menu UI assets
            // - Audio clips
            // - Textures/sprites
            // - Other required resources
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.3), cancellationToken: cancellation);
            Debug.Log("[GameInitializer] Essential assets loaded (placeholder).");
        }
        
        /// <summary>
        /// Publishes a progress update event.
        /// </summary>
        private async UniTask PublishProgress(float progress, string stepKey, CancellationToken cancellation)
        {
            _progressPublisher.Publish(new GameInitializationProgressEvent(progress, stepKey));
            await UniTask.Yield(cancellation);
        }
    }
}

