using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MessagePipe;
using SpiralingStudio.Events;
using SpiralingStudio.Services.DataManagement;
using UnityEngine;
using VContainer;

namespace SpiralingStudio.Services.Localization
{
    /// <summary>
    /// Central localization service that manages translations, language switching, and I2 integration.
    /// Loads translation data from MasterMemory CSV files and provides fallback support.
    /// </summary>
    public class MFLocalizationService : IMFService
    {
        private readonly LocalizationConfiguration _config;
        private readonly IPublisher<RefreshLanguageEvent> _languageEventPublisher;
        private readonly IDbLoader _dbLoader;

        // Localization data storage
        private Dictionary<string, Dictionary<string, string>> _localizationData; // [key][languageName] = translation
        private string _currentPrimaryLanguage;
        private string _currentFallbackLanguage;
        private bool _isInitialized;
        private int _fallbackDepthCounter;

        // Language mapping
        private Dictionary<string, string> _languageNameToCodeMap; // languageName -> languageCode
        private Dictionary<string, string> _languageCodeToNameMap; // languageCode -> languageName

        /// <summary>
        /// Constructor for dependency injection.
        /// </summary>
        [Inject]
        public MFLocalizationService(
            LocalizationConfiguration config,
            IPublisher<RefreshLanguageEvent> languageEventPublisher,
            IDbLoader dbLoader)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _languageEventPublisher = languageEventPublisher ?? throw new ArgumentNullException(nameof(languageEventPublisher));
            _dbLoader = dbLoader ?? throw new ArgumentNullException(nameof(dbLoader));

            _localizationData = new Dictionary<string, Dictionary<string, string>>();
            _languageNameToCodeMap = new Dictionary<string, string>();
            _languageCodeToNameMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// Returns initialization tasks for the service.
        /// </summary>
        public UniTask[] GetInitializeTasks()
        {
            return new[] { InitializeAsync() };
        }

        /// <summary>
        /// Initializes the localization system.
        /// </summary>
        private async UniTask InitializeAsync()
        {
            try
            {
                Debug.Log("[MFLocalizationService] Initializing localization system...");

                // Build language mappings
                BuildLanguageMappings();

                // Load localization data from CSV (via MasterMemory)
                await LoadLocalizationDataAsync();

                // Determine initial languages
                DetermineInitialLanguages();

                // Populate I2 Localization with our data
                PopulateI2Localization();

                // Sync current language to I2
                LocalizationI2Bridge.SyncLanguageToI2(_currentPrimaryLanguage);

                _isInitialized = true;
                Debug.Log($"[MFLocalizationService] Initialization complete. Primary: {_currentPrimaryLanguage}, Fallback: {_currentFallbackLanguage}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MFLocalizationService] Initialization failed: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Builds mappings between language names and codes.
        /// </summary>
        private void BuildLanguageMappings()
        {
            _languageNameToCodeMap.Clear();
            _languageCodeToNameMap.Clear();

            foreach (var langInfo in _config.supportedLanguages)
            {
                if (!string.IsNullOrEmpty(langInfo.languageName) && !string.IsNullOrEmpty(langInfo.languageCode))
                {
                    _languageNameToCodeMap[langInfo.languageName] = langInfo.languageCode;
                    _languageCodeToNameMap[langInfo.languageCode] = langInfo.languageName;
                }
            }
        }

        /// <summary>
        /// Loads localization data from CSV files via MasterMemory.
        /// Note: This is a placeholder. Actual MasterMemory loading would require
        /// the CSV data to be processed through the DbLoader and MemoryDatabase.
        /// For now, we'll create sample data structure.
        /// </summary>
        private async UniTask LoadLocalizationDataAsync()
        {
            // TODO: Once MasterMemory is fully set up with CSV files, load data like this:
            // var database = _dbLoader.Database;
            // var entries = database.LocalizationEntryTable.All;

            // For now, populate with sample data to demonstrate structure
            _localizationData.Clear();

            // Sample data - this would come from MasterMemory in production
            AddSampleTranslation("common_button_text_confirm", "English", "Confirm");
            AddSampleTranslation("common_button_text_confirm", "Spanish", "Confirmar");
            AddSampleTranslation("common_button_text_confirm", "French", "Confirmer");

            AddSampleTranslation("common_button_text_cancel", "English", "Cancel");
            AddSampleTranslation("common_button_text_cancel", "Spanish", "Cancelar");
            AddSampleTranslation("common_button_text_cancel", "French", "Annuler");

            AddSampleTranslation("common_text_hello", "English", "Hello, {0}!");
            AddSampleTranslation("common_text_hello", "Spanish", "¡Hola, {0}!");
            AddSampleTranslation("common_text_hello", "French", "Bonjour, {0}!");

            await UniTask.Yield(); // Simulate async operation

            Debug.Log($"[MFLocalizationService] Loaded {_localizationData.Count} localization keys.");
        }

        /// <summary>
        /// Helper method to add sample translations (remove in production).
        /// </summary>
        private void AddSampleTranslation(string key, string language, string translation)
        {
            if (!_localizationData.ContainsKey(key))
            {
                _localizationData[key] = new Dictionary<string, string>();
            }
            _localizationData[key][language] = translation;
        }

        /// <summary>
        /// Determines the initial primary and fallback languages based on configuration and device locale.
        /// </summary>
        private void DetermineInitialLanguages()
        {
            // Set fallback language
            _currentFallbackLanguage = _config.defaultFallbackLanguage;

            // Determine primary language
            if (_config.useDeviceLocale)
            {
                string deviceLanguage = GetDeviceLanguage();
                if (!string.IsNullOrEmpty(deviceLanguage) && _config.IsLanguageSupported(deviceLanguage))
                {
                    _currentPrimaryLanguage = _config.GetLanguageName(deviceLanguage);
                }
            }

            // Fallback to default if no device language found
            if (string.IsNullOrEmpty(_currentPrimaryLanguage))
            {
                _currentPrimaryLanguage = _config.defaultFallbackLanguage;
            }

            Debug.Log($"[MFLocalizationService] Initial languages set - Primary: {_currentPrimaryLanguage}, Fallback: {_currentFallbackLanguage}");
        }

        /// <summary>
        /// Gets the device's language code.
        /// </summary>
        private string GetDeviceLanguage()
        {
            try
            {
                // Unity's Application.systemLanguage returns a SystemLanguage enum
                SystemLanguage systemLanguage = Application.systemLanguage;
                
                // Map common system languages to ISO codes
                switch (systemLanguage)
                {
                    case SystemLanguage.English: return "en";
                    case SystemLanguage.Spanish: return "es";
                    case SystemLanguage.French: return "fr";
                    case SystemLanguage.German: return "de";
                    case SystemLanguage.Italian: return "it";
                    case SystemLanguage.Portuguese: return "pt";
                    case SystemLanguage.Russian: return "ru";
                    case SystemLanguage.Chinese:
                    case SystemLanguage.ChineseSimplified: return "zh";
                    case SystemLanguage.Japanese: return "ja";
                    case SystemLanguage.Korean: return "ko";
                    case SystemLanguage.Arabic: return "ar";
                    default: return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MFLocalizationService] Failed to get device language: {ex}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Populates I2 Localization with our loaded data.
        /// </summary>
        private void PopulateI2Localization()
        {
            var languageNames = _config.supportedLanguages.Select(l => l.languageName).ToList();
            LocalizationI2Bridge.PopulateI2LanguageSource(_localizationData, languageNames, _languageNameToCodeMap);
        }

        /// <summary>
        /// Gets a translation for the specified key in the current language with fallback support.
        /// </summary>
        public string GetTranslation(string key)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[MFLocalizationService] Service not initialized. Returning key.");
                return key;
            }

            if (string.IsNullOrEmpty(key))
            {
                if (_config.showMissingTranslationWarnings)
                    Debug.LogWarning("[MFLocalizationService] Attempted to get translation for null/empty key.");
                return string.Empty;
            }

            if (_config.showKeysInsteadOfTranslations)
            {
                return key; // Debug mode
            }

            _fallbackDepthCounter = 0;
            return GetTranslationInternal(key, _currentPrimaryLanguage);
        }

        /// <summary>
        /// Internal method to get translation with recursive fallback support.
        /// </summary>
        private string GetTranslationInternal(string key, string language)
        {
            // Prevent infinite recursion
            _fallbackDepthCounter++;
            if (_fallbackDepthCounter > _config.maxFallbackDepth)
            {
                Debug.LogError($"[MFLocalizationService] Max fallback depth reached for key '{key}'.");
                return _config.missingKeyPrefix + key;
            }

            // Check if key exists
            if (!_localizationData.ContainsKey(key))
            {
                if (_config.showMissingTranslationWarnings)
                    Debug.LogWarning($"[MFLocalizationService] Key not found: '{key}'");
                return _config.missingKeyPrefix + key;
            }

            var translations = _localizationData[key];

            // Try to get translation for current language
            if (translations.TryGetValue(language, out string translation) && !string.IsNullOrEmpty(translation))
            {
                if (_config.logKeyLookups)
                    Debug.Log($"[MFLocalizationService] '{key}' [{language}] = '{translation}'");
                return translation;
            }

            // Fallback to fallback language
            if (_config.useFallbackForMissingTranslations && language != _currentFallbackLanguage)
            {
                if (_config.showMissingTranslationWarnings)
                    Debug.LogWarning($"[MFLocalizationService] Translation missing for key '{key}' in language '{language}'. Using fallback.");
                return GetTranslationInternal(key, _currentFallbackLanguage);
            }

            // No translation found
            if (_config.showMissingTranslationWarnings)
                Debug.LogWarning($"[MFLocalizationService] No translation found for key '{key}' in any language.");
            return _config.missingKeyPrefix + key;
        }

        /// <summary>
        /// Gets a translation with parameter replacement.
        /// </summary>
        public string GetTranslation(string key, params object[] parameters)
        {
            string translation = GetTranslation(key);

            if (parameters != null && parameters.Length > 0)
            {
                try
                {
                    translation = string.Format(translation, parameters);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MFLocalizationService] Failed to format translation for key '{key}': {ex}");
                }
            }

            return translation;
        }

        /// <summary>
        /// Sets the current primary language and publishes a language change event.
        /// </summary>
        public void SetLanguage(string languageCodeOrName)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[MFLocalizationService] Cannot set language before initialization.");
                return;
            }

            if (string.IsNullOrEmpty(languageCodeOrName))
            {
                Debug.LogWarning("[MFLocalizationService] Attempted to set null/empty language.");
                return;
            }

            // Determine if input is code or name
            string languageName = _config.GetLanguageName(languageCodeOrName);
            if (string.IsNullOrEmpty(languageName))
            {
                languageName = languageCodeOrName; // Assume it's already a name
            }

            // Validate language
            if (!_config.IsLanguageSupported(languageName))
            {
                Debug.LogWarning($"[MFLocalizationService] Language '{languageCodeOrName}' is not supported. Falling back to default.");
                languageName = _config.defaultFallbackLanguage;
            }

            string previousLanguage = _currentPrimaryLanguage;
            _currentPrimaryLanguage = languageName;

            // Sync with I2
            LocalizationI2Bridge.SyncLanguageToI2(languageName);

            // Publish event
            var evt = new RefreshLanguageEvent(previousLanguage, _currentPrimaryLanguage);
            _languageEventPublisher.Publish(evt);

            Debug.Log($"[MFLocalizationService] Language changed from '{previousLanguage}' to '{_currentPrimaryLanguage}'");
        }

        /// <summary>
        /// Gets the current primary language name.
        /// </summary>
        public string GetCurrentLanguage()
        {
            return _currentPrimaryLanguage;
        }

        /// <summary>
        /// Gets the current primary language code.
        /// </summary>
        public string GetCurrentLanguageCode()
        {
            return _config.GetLanguageCode(_currentPrimaryLanguage);
        }

        /// <summary>
        /// Gets the fallback language name.
        /// </summary>
        public string GetFallbackLanguage()
        {
            return _currentFallbackLanguage;
        }

        /// <summary>
        /// Checks if a language is supported.
        /// </summary>
        public bool IsLanguageSupported(string languageCodeOrName)
        {
            return _config.IsLanguageSupported(languageCodeOrName);
        }

        /// <summary>
        /// Gets all supported language names.
        /// </summary>
        public List<string> GetSupportedLanguages()
        {
            return _config.supportedLanguages.Select(l => l.languageName).ToList();
        }

        /// <summary>
        /// Checks if the service is initialized.
        /// </summary>
        public bool IsInitialized()
        {
            return _isInitialized;
        }

        public string GetLocalizedString(string requestTitleLocKey)
        {
           return GetTranslation(requestTitleLocKey);
        }
    }
}




