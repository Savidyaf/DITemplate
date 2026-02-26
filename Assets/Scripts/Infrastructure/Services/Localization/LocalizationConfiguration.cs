using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiralingStudio.Services.Localization
{
    /// <summary>
    /// Configuration for the localization system.
    /// Defines supported languages, fallback behavior, and content file references.
    /// </summary>
    [CreateAssetMenu(menuName = "SpiralingStudio/Localization/LocalizationConfiguration", fileName = "LocalizationConfiguration")]
    public class LocalizationConfiguration : ScriptableObject
    {
        [Header("Language Settings")]
        [Tooltip("Default fallback language when translation is missing")]
        public string defaultFallbackLanguage = "English";
        
        [Tooltip("List of supported languages with their locale codes")]
        public List<LanguageInfo> supportedLanguages = new List<LanguageInfo>()
        {
            new LanguageInfo { languageName = "English", languageCode = "en" },
            new LanguageInfo { languageName = "Spanish", languageCode = "es" },
            new LanguageInfo { languageName = "French", languageCode = "fr" }
        };

        [Header("Content Files")]
        [Tooltip("List of content file identifiers (e.g., 'Common', 'Narration', 'UI')")]
        public List<string> contentFiles = new List<string>()
        {
            "Common",
            "Narration",
            "UI"
        };

        [Header("Loading Behavior")]
        [Tooltip("Preload all languages at startup (true) or load on-demand (false)")]
        public bool preloadAllLanguages = false;
        
        [Tooltip("Use device locale as primary language on first launch")]
        public bool useDeviceLocale = true;

        [Header("Debug Settings")]
        [Tooltip("Show warnings for missing translations in console")]
        public bool showMissingTranslationWarnings = true;
        
        [Tooltip("Log all translation key lookups (verbose)")]
        public bool logKeyLookups = false;
        
        [Tooltip("Show localization keys instead of translations (for debugging)")]
        public bool showKeysInsteadOfTranslations = false;
        
        [Tooltip("Prefix to display before missing translation keys")]
        public string missingKeyPrefix = "[MISSING] ";

        [Header("Fallback Behavior")]
        [Tooltip("When a translation is missing, use fallback language (true) or show key (false)")]
        public bool useFallbackForMissingTranslations = true;
        
        [Tooltip("Maximum depth for circular fallback prevention")]
        public int maxFallbackDepth = 3;

        /// <summary>
        /// Gets the language code for a given language name.
        /// </summary>
        public string GetLanguageCode(string languageName)
        {
            var lang = supportedLanguages.Find(l => l.languageName.Equals(languageName, StringComparison.OrdinalIgnoreCase));
            return lang?.languageCode ?? string.Empty;
        }

        /// <summary>
        /// Gets the language name for a given language code.
        /// </summary>
        public string GetLanguageName(string languageCode)
        {
            var lang = supportedLanguages.Find(l => l.languageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
            return lang?.languageName ?? string.Empty;
        }

        /// <summary>
        /// Checks if a language is supported.
        /// </summary>
        public bool IsLanguageSupported(string languageCodeOrName)
        {
            return supportedLanguages.Exists(l => 
                l.languageCode.Equals(languageCodeOrName, StringComparison.OrdinalIgnoreCase) ||
                l.languageName.Equals(languageCodeOrName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the fallback language code.
        /// </summary>
        public string GetFallbackLanguageCode()
        {
            return GetLanguageCode(defaultFallbackLanguage);
        }
    }

    /// <summary>
    /// Represents a supported language with its name and locale code.
    /// </summary>
    [Serializable]
    public class LanguageInfo
    {
        [Tooltip("Display name of the language (e.g., 'English', 'Spanish')")]
        public string languageName;
        
        [Tooltip("ISO 639-1 language code (e.g., 'en', 'es', 'fr')")]
        public string languageCode;
        
        [Tooltip("Is this a right-to-left language (e.g., Arabic, Hebrew)")]
        public bool isRightToLeft = false;
    }
}




