using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using SpiralingStudio.Events;
using SpiralingStudio.Services.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Infrastructure.UIManager.GameInitialization
{
    /// <summary>
    /// Game initialization screen with progress bar.
    /// Subscribes to GameInitializationProgressEvent to update the UI.
    /// </summary>
    public class GameInitializationContext : UiContext
    {
        [Header("Progress UI Elements")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI stepDescriptionText;
        
        private IDisposable _progressSubscription;
        
        protected override void ConfigureContextSpecificServices(IContainerBuilder builder)
        {
            // No specific services needed
        }
        
        public override void Start()
        {
            base.Start();
            
            // Subscribe to progress events
            var progressSubscriber = Container.Resolve<ISubscriber<GameInitializationProgressEvent>>();
            _progressSubscription = progressSubscriber.Subscribe(OnProgressUpdate);
            
            // Initialize progress bar
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }
            
            Debug.Log("[GameInitializationContext] Initialization screen ready.");
        }
        
        /// <summary>
        /// Handles progress update events from GameInitializer.
        /// </summary>
        private void OnProgressUpdate(GameInitializationProgressEvent evt)
        {
            if (progressBar != null)
            {
                progressBar.value = evt.Progress;
            }
            
            if (stepDescriptionText != null && LocalizationService != null)
            {
                string localizedDescription = LocalizationService.GetTranslation(evt.StepDescriptionKey);
                stepDescriptionText.text = localizedDescription;
            }
            
            Debug.Log($"[GameInitializationContext] Progress: {evt.Progress * 100}% - {evt.StepDescriptionKey}");
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _progressSubscription?.Dispose();
        }
    }
}






