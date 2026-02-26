using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace Infrastructure.Systems
{
    /// <summary>
    /// Represents the initialization state of a scene context.
    /// </summary>
    public enum SceneState
    {
        /// <summary>Scene has not been initialized yet.</summary>
        Uninitialized,
        /// <summary>Scene initialization is in progress.</summary>
        Initializing,
        /// <summary>Scene is fully initialized and ready.</summary>
        Ready,
        /// <summary>Scene initialization failed.</summary>
        Failed
    }

    /// <summary>
    /// Base class for non-UI gameplay scenes with managed initialization lifecycle.
    /// Extends LifetimeScope for VContainer DI support.
    /// 
    /// Unlike UiContext (which handles UI-specific concerns like localization batch init
    /// and visibility toggles), SceneContext is designed for gameplay scenes that need
    /// structured initialization but not UI management.
    /// 
    /// Initialization is triggered externally by GameFlowService after scene load,
    /// NOT by VContainer's IAsyncStartable pattern (which causes issues with LifetimeScope).
    /// </summary>
    public abstract class SceneContext : LifetimeScope
    {
        [Header("Debug")]
        [Tooltip("Enable debug logging for scene lifecycle.")]
        [SerializeField] private bool enableDebugLogs = true;

        private SceneState _state = SceneState.Uninitialized;
        private readonly object _stateLock = new object();

        /// <summary>
        /// Gets the current initialization state of this scene context.
        /// </summary>
        public SceneState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// Gets whether this scene context has been successfully initialized.
        /// </summary>
        public bool IsReady => State == SceneState.Ready;

        /// <summary>
        /// Initializes the scene context. Called by GameFlowService after scene load.
        /// This method is idempotent - calling it multiple times has no effect after
        /// the first successful initialization.
        /// </summary>
        /// <param name="ct">Cancellation token for the initialization operation.</param>
        /// <exception cref="OperationCanceledException">Thrown if initialization is cancelled.</exception>
        /// <exception cref="Exception">Thrown if initialization fails.</exception>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            lock (_stateLock)
            {
                if (_state != SceneState.Uninitialized)
                {
                    LogDebug($"InitializeAsync called but state is {_state}. Skipping.");
                    return;
                }
                _state = SceneState.Initializing;
            }

            LogDebug("Initializing scene context...");

            try
            {
                ct.ThrowIfCancellationRequested();

                await OnSceneInitializeAsync(ct);

                lock (_stateLock)
                {
                    _state = SceneState.Ready;
                }

                LogDebug("Scene context initialized successfully.");
            }
            catch (OperationCanceledException)
            {
                lock (_stateLock)
                {
                    _state = SceneState.Uninitialized;
                }
                LogDebug("Scene initialization was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                lock (_stateLock)
                {
                    _state = SceneState.Failed;
                }
                Debug.LogError($"[{GetType().Name}] Scene initialization failed: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Override this method to perform scene-specific initialization logic.
        /// Called after the scene is loaded and the DI container is built.
        /// </summary>
        /// <param name="ct">Cancellation token for the initialization operation.</param>
        protected abstract UniTask OnSceneInitializeAsync(CancellationToken ct);

        /// <summary>
        /// Logs a debug message if debug logging is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[{GetType().Name}] {message}");
            }
        }
    }
}
