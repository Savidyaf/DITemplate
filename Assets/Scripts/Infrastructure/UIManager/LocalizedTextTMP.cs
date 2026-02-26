using System;
using System.Linq;
using MessagePipe;
using SpiralingStudio.Events;
using SpiralingStudio.Services.Localization;
using TMPro;
using UnityEngine;
using VContainer;

namespace Infrastructure.UIManager
{
    /// <summary>
    /// Component that localizes TextMeshProUGUI text based on a localization key.
    /// Localization is controlled by UiContext during scene initialization (batch refresh).
    /// Subscribes to language change events for runtime language switching.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedTextTMP : MonoBehaviour
    {
        [Header("Localization Settings")]
        [SerializeField]
        [Tooltip("Localization key (e.g., 'common_button_text_confirm')")]
        private string localizationKey = string.Empty;

        [SerializeField]
        [Tooltip("Use parameters for text replacement (e.g., 'Hello, {0}!')")]
        private bool useParameters = false;

        [SerializeField]
        [Tooltip("Parameter values for text replacement")]
        private string[] parameterValues = Array.Empty<string>();

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Show the localization key in the text field when in editor (for debugging)")]
        private bool showKeyInEditor = false;

        // Dependencies (injected or provided by UiContext)
        private MFLocalizationService _localizationService;
        private ISubscriber<RefreshLanguageEvent> _languageEventSubscriber;
        private IDisposable _languageEventSubscription;

        // Component references
        private TextMeshProUGUI _textComponent;
        private bool _isInitialized;
        private string _lastTranslation;

        /// <summary>
        /// Public property to access/change the localization key.
        /// </summary>
        public string LocalizationKey
        {
            get => localizationKey;
            set
            {
                if (localizationKey != value)
                {
                    localizationKey = value;
                    if (_isInitialized && !string.IsNullOrEmpty(value))
                    {
                        Localize();
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if this component has been initialized by UiContext.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// VContainer injection point - only injects event subscriber for language changes.
        /// Localization service is provided separately during context initialization.
        /// </summary>
        [Inject]
        public void Construct(ISubscriber<RefreshLanguageEvent> languageEventSubscriber)
        {
            _languageEventSubscriber = languageEventSubscriber;
            
            // Subscribe after injection (Awake runs before injection)
            SubscribeToLanguageEvents();
        }

        private void Awake()
        {
            // Get TextMeshProUGUI component
            _textComponent = GetComponent<TextMeshProUGUI>();
            if (_textComponent == null)
            {
                Debug.LogError($"[LocalizedTextTMP] TextMeshProUGUI component not found on {gameObject.name}");
                enabled = false;
                return;
            }
        }

        private void SubscribeToLanguageEvents()
        {
            if (_languageEventSubscriber != null && _languageEventSubscription == null)
            {
                var bag = DisposableBag.CreateBuilder();
                _languageEventSubscriber.Subscribe(OnRefreshLanguage).AddTo(bag);
                _languageEventSubscription = bag.Build();
            }
        }

        private void OnDestroy()
        {
            // Clean up event subscription
            _languageEventSubscription?.Dispose();
            _languageEventSubscription = null;
        }

        /// <summary>
        /// Called by UiContext during batch localization initialization.
        /// This is the primary entry point for initializing this component.
        /// </summary>
        /// <param name="localizationService">The localization service to use.</param>
        public void InitializeFromContext(MFLocalizationService localizationService)
        {
            if (localizationService == null)
            {
                Debug.LogWarning($"[LocalizedTextTMP] InitializeFromContext called with null service on {gameObject.name}");
                return;
            }

            _localizationService = localizationService;
            _isInitialized = true;

            // Perform initial localization
            if (!string.IsNullOrEmpty(localizationKey))
            {
                Localize();
            }
        }

        /// <summary>
        /// Handler for language change events.
        /// Only processes if already initialized.
        /// </summary>
        private void OnRefreshLanguage(RefreshLanguageEvent evt)
        {
            if (_isInitialized && !string.IsNullOrEmpty(localizationKey) && gameObject.activeInHierarchy)
            {
                Localize();
            }
        }

        /// <summary>
        /// Localizes the text component using the current localization key.
        /// Safe to call multiple times (idempotent if translation unchanged).
        /// </summary>
        public void Localize()
        {
            if (_textComponent == null)
            {
                Debug.LogError($"[LocalizedTextTMP] Cannot localize - TextMeshProUGUI component is null on {gameObject.name}");
                return;
            }

            if (_localizationService == null)
            {
                Debug.LogWarning($"[LocalizedTextTMP] Cannot localize - MFLocalizationService is null on {gameObject.name}. Call InitializeFromContext first.");
                return;
            }

            if (string.IsNullOrEmpty(localizationKey))
            {
                _textComponent.text = string.Empty;
                _lastTranslation = string.Empty;
                return;
            }

            try
            {
                string translation;

                if (useParameters && parameterValues != null && parameterValues.Length > 0)
                {
                    translation = _localizationService.GetTranslation(localizationKey, parameterValues.Cast<object>().ToArray());
                }
                else
                {
                    translation = _localizationService.GetTranslation(localizationKey);
                }

                // Only update if translation changed to avoid unnecessary updates
                if (_lastTranslation != translation)
                {
                    _textComponent.text = translation;
                    _lastTranslation = translation;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizedTextTMP] Error localizing key '{localizationKey}' on {gameObject.name}: {ex}");
                _textComponent.text = $"[ERROR: {localizationKey}]";
            }
        }

        /// <summary>
        /// Localizes the text with custom parameters.
        /// </summary>
        public void LocalizeWithParameters(params object[] values)
        {
            if (_textComponent == null || _localizationService == null || string.IsNullOrEmpty(localizationKey))
                return;

            try
            {
                string translation = _localizationService.GetTranslation(localizationKey, values);
                _textComponent.text = translation;
                _lastTranslation = translation;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizedTextTMP] Error localizing with parameters for key '{localizationKey}' on {gameObject.name}: {ex}");
                _textComponent.text = $"[ERROR: {localizationKey}]";
            }
        }

        /// <summary>
        /// Sets a new localization key and immediately localizes if initialized.
        /// </summary>
        public void SetKey(string newKey)
        {
            localizationKey = newKey;
            if (_isInitialized && !string.IsNullOrEmpty(newKey))
            {
                Localize();
            }
        }

        /// <summary>
        /// Sets parameter values and re-localizes if initialized.
        /// </summary>
        public void SetParameters(params string[] values)
        {
            parameterValues = values;
            useParameters = values != null && values.Length > 0;
            if (_isInitialized)
            {
                Localize();
            }
        }

        /// <summary>
        /// Clears the localization and text.
        /// </summary>
        public void Clear()
        {
            localizationKey = string.Empty;
            if (_textComponent != null)
            {
                _textComponent.text = string.Empty;
            }
            _lastTranslation = string.Empty;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Validates the component in edit mode.
        /// </summary>
        private void OnValidate()
        {
            if (_textComponent == null)
            {
                _textComponent = GetComponent<TextMeshProUGUI>();
            }

            // In editor, optionally show the key
            if (showKeyInEditor && !Application.isPlaying && _textComponent != null)
            {
                _textComponent.text = $"[KEY: {localizationKey}]";
            }
        }
#endif
    }
}
