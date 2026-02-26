using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using SpiralingStudio.Events;
using SpiralingStudio.Services.Localization;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Infrastructure.UIManager
{
    /// <summary>
    /// Context state for tracking initialization progress.
    /// </summary>
    public enum ContextState
    {
        Uninitialized,
        Initializing,
        Ready
    }

    /// <summary>
    /// Abstract base for a top-level UI context. Inherits from LifetimeScope for
    /// VContainer usage. Manages visibility and interaction states via global events.
    /// Initialization is controlled by UiManager after scene load.
    /// </summary>
    public abstract class UiContext : LifetimeScope, IUiContext
    {
        [Header("Child UI Contexts")]
        [Tooltip("Optional list of SubUiContexts that are direct children in the hierarchy and part of this scope.")]
        [SerializeField] private List<SubUiContext> subUiContextsList = new List<SubUiContext>();

        // Dependencies resolved from the container
        private IAsyncSubscriber<ToggleGlobalUiVisibility> _globalVisibilitySubscriber;
        private IAsyncSubscriber<ToggleGlobalUiInteractions> _globalInteractionsSubscriber;
        private IObjectResolver _objectResolver;
        private MFLocalizationService _localizationService;

        /// <summary>
        /// The localization service resolved from the container.
        /// Available for subclasses that need direct access to localization.
        /// </summary>
        protected MFLocalizationService LocalizationService => _localizationService;

        // State
        private bool _isContextHidden = false;
        private IDisposable _messagePipeSubscriptions;
        private ContextState _state = ContextState.Uninitialized;

        // Cached localized components for batch refresh
        private LocalizedTextTMP[] _localizedComponents;

        /// <summary>
        /// Current initialization state of this context.
        /// </summary>
        public ContextState State => _state;

        /// <summary>Configures VContainer dependencies.</summary>
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            // Register pre-defined sub-contexts
            if (subUiContextsList != null)
            {
                foreach (var subUiContext in subUiContextsList)
                {
                    if (subUiContext != null)
                    {
                        builder.RegisterComponent(subUiContext).AsSelf();
                    }
                    else
                    {
                        Debug.LogWarning($"Null SubUiContext found in {gameObject.name}'s list.", this);
                    }
                }
            }

            builder.RegisterBuildCallback(OnContainerBuild);
            ConfigureContextSpecificServices(builder);
        }

        /// <summary>Override to register context-specific services.</summary>
        protected virtual void ConfigureContextSpecificServices(IContainerBuilder builder) { }

        /// <summary>Called after the container is built.</summary>
        private void OnContainerBuild(IObjectResolver resolver)
        {
            _objectResolver = resolver;

            // Resolve dependencies
            _localizationService = resolver.Resolve<MFLocalizationService>();
            _globalVisibilitySubscriber = resolver.Resolve<IAsyncSubscriber<ToggleGlobalUiVisibility>>();
            _globalInteractionsSubscriber = resolver.Resolve<IAsyncSubscriber<ToggleGlobalUiInteractions>>();

            // Subscribe to global events
            this.RegisterContextSubscriptions(_globalVisibilitySubscriber, _globalInteractionsSubscriber);

            Debug.Log($"[{GetType().Name} - {gameObject.name}] Container built.", this);
        }

        /// <summary>
        /// Initializes the context after scene load. Called by UiManager.
        /// Performs batch localization and signals readiness.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public async UniTask InitializeContextAsync(CancellationToken cancellationToken = default)
        {
            if (_state != ContextState.Uninitialized)
            {
                Debug.LogWarning($"[{GetType().Name} - {gameObject.name}] InitializeContextAsync called but state is {_state}. Skipping.", this);
                return;
            }

            _state = ContextState.Initializing;
            Debug.Log($"[{GetType().Name} - {gameObject.name}] Initializing context...", this);

            try
            {
                // Step 1: Gather and initialize all LocalizedTextTMP components
                await InitializeLocalizedComponentsAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 2: Allow subclasses to perform custom initialization
                await OnContextInitializedAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                _state = ContextState.Ready;
                Debug.Log($"[{GetType().Name} - {gameObject.name}] Context ready.", this);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[{GetType().Name} - {gameObject.name}] Context initialization cancelled.", this);
                _state = ContextState.Uninitialized;
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name} - {gameObject.name}] Context initialization failed: {ex}", this);
                _state = ContextState.Uninitialized;
                throw;
            }
        }

        /// <summary>
        /// Gathers all LocalizedTextTMP components in the hierarchy and initializes them.
        /// This ensures all localization happens in a single controlled pass on the main thread.
        /// </summary>
        private async UniTask InitializeLocalizedComponentsAsync(CancellationToken cancellationToken)
        {
            // Gather all LocalizedTextTMP components in this context's hierarchy
            _localizedComponents = GetComponentsInChildren<LocalizedTextTMP>(includeInactive: true);

            if (_localizedComponents.Length == 0)
            {
                Debug.Log($"[{GetType().Name} - {gameObject.name}] No LocalizedTextTMP components found.", this);
                return;
            }

            Debug.Log($"[{GetType().Name} - {gameObject.name}] Initializing {_localizedComponents.Length} localized components.", this);

            // Initialize all components with the localization service
            foreach (var component in _localizedComponents)
            {
                if (component != null)
                {
                    component.InitializeFromContext(_localizationService);
                }
            }

            // Yield to ensure UI updates are processed
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cancellationToken);
        }

        /// <summary>
        /// Called after localization initialization completes.
        /// Override this method to perform custom context-specific initialization.
        /// </summary>
        protected virtual UniTask OnContextInitializedAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Called by Unity after the component is enabled.
        /// Override this method in subclasses for custom initialization logic.
        /// Note: Localization initialization is handled by InitializeContextAsync, not Start.
        /// </summary>
        public virtual void Start()
        {
            // Subclasses can override for custom initialization
        }

        /// <summary>
        /// Refreshes all localized components in this context.
        /// Can be called manually if needed (e.g., after dynamic content changes).
        /// </summary>
        public void RefreshLocalization()
        {
            if (_localizedComponents == null)
            {
                // Gather components if not already cached
                _localizedComponents = GetComponentsInChildren<LocalizedTextTMP>(includeInactive: true);
            }

            foreach (var component in _localizedComponents)
            {
                if (component != null && component.IsInitialized)
                {
                    component.Localize();
                }
            }
        }

        /// <summary>Instantiates a SubUiContext prefab within this scope.</summary>
        public SubUiContext InstantiateSubUiContext(SubUiContext prefab, Transform parentTransform, bool worldPositionStays = false)
        {
            if (prefab == null) 
            { 
                Debug.LogError($"[{GetType().Name} - {gameObject.name}] Null prefab.", this); 
                return null; 
            }
            if (_objectResolver == null) 
            { 
                Debug.LogError($"[{GetType().Name} - {gameObject.name}] Resolver null.", this); 
                return null; 
            }

            SubUiContext instance = _objectResolver.Instantiate(prefab, parentTransform, worldPositionStays);
            if (instance != null)
            {
                if (_isContextHidden) 
                { 
                    instance.HideGameObject(); 
                }

                // Initialize any localized components in the newly instantiated prefab
                var localizedComponents = instance.GetComponentsInChildren<LocalizedTextTMP>(includeInactive: true);
                foreach (var component in localizedComponents)
                {
                    if (component != null && !component.IsInitialized)
                    {
                        component.InitializeFromContext(_localizationService);
                    }
                }
            }
            else 
            { 
                Debug.LogError($"[{GetType().Name} - {gameObject.name}] Failed to instantiate {prefab.name}.", this); 
            }
            return instance;
        }

        /// <summary>Instantiates a SubUiContext prefab within this scope with typed return.</summary>
        public T InstantiateSubUiContext<T>(T prefab, Transform parentTransform, bool worldPositionStays = false) where T : SubUiContext
        {
            var instance = InstantiateSubUiContext((SubUiContext)prefab, parentTransform, worldPositionStays);
            return instance as T;
        }

        // --- IUiComponent Implementation ---
        public void HideGameObject()
        {
            if (!_isContextHidden) 
            { 
                _isContextHidden = true; 
                gameObject.SetActive(false); 
                Debug.Log($"[{GetType().Name} - {gameObject.name}] Hidden.", this); 
            }
        }

        public void ShowGameObject()
        {
            if (_isContextHidden) 
            { 
                _isContextHidden = false; 
                gameObject.SetActive(true); 
                Debug.Log($"[{GetType().Name} - {gameObject.name}] Shown.", this); 
            }
        }

        public ref IDisposable GetDisposableReference() => ref _messagePipeSubscriptions;

        // --- Event Handlers ---
        /// <summary>Handles global visibility events.</summary>
        public virtual async UniTask OnUiVisibilityToggled(ToggleUIVisibilityEvent globalVisibilityEvent, CancellationToken cancellationToken)
        {
            if (globalVisibilityEvent.IsVisible)
            {
                ShowGameObject();
            }
            else
            {
                HideGameObject();
            }
            await UniTask.CompletedTask;
        }

        /// <summary>Handles global interaction events.</summary>
        public virtual async UniTask OnUiInteractionsToggled(ToggleUiInteractionsEvent globalInteractionEvent, CancellationToken cancellationToken)
        {
            // Subclasses can override to handle interaction state changes
            await UniTask.CompletedTask;
        }

        /// <summary>Cleans up subscriptions.</summary>
        protected override void OnDestroy()
        {
            // Dispose subscriptions BEFORE base.OnDestroy() to prevent issues with container disposal
            _messagePipeSubscriptions?.Dispose();
            _localizedComponents = null;

            base.OnDestroy();
            Debug.Log($"[{GetType().Name} - {gameObject.name}] Destroyed.", this);
        }
    }
}
