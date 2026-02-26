#if I2_LOCALIZATION
using System;
using System.Collections.Generic;
using System.Linq;
using I2.Loc;
using UnityEngine;

namespace SpiralingStudio.Services.Localization
{
    /// <summary>
    /// Bridge class that integrates our MasterMemory-based localization data with I2 Localization.
    /// Populates I2's LanguageSourceData at runtime to leverage I2's advanced features (RTL, parameters, etc.)
    /// </summary>
    internal static class LocalizationI2Bridge
    {
        private static LanguageSourceData _customLanguageSource;
        private const string CustomSourceName = "SSLocalizationSource";

        /// <summary>
        /// Populates I2 Localization with data from our localization system.
        /// Creates a custom LanguageSourceData and registers it with I2's LocalizationManager.
        /// </summary>
        /// <param name="localizationData">Dictionary mapping keys to language-translation pairs</param>
        /// <param name="languages">List of language names to populate</param>
        /// <param name="languageCodeMap">Dictionary mapping language names to ISO codes</param>
        public static void PopulateI2LanguageSource(
            Dictionary<string, Dictionary<string, string>> localizationData,
            List<string> languages,
            Dictionary<string, string> languageCodeMap)
        {
            try
            {
                // Create or get existing custom language source
                if (_customLanguageSource == null)
                {
                    _customLanguageSource = new LanguageSourceData();
                    _customLanguageSource.mIsGlobalSource = true;
                }
                else
                {
                    // Clear existing data
                    _customLanguageSource.mLanguages.Clear();
                    _customLanguageSource.mTerms.Clear();
                    _customLanguageSource.mDictionary.Clear();
                }

                // Populate languages
                foreach (var languageName in languages)
                {
                    var languageData = new LanguageData();
                    languageData.Name = languageName;
                    
                    // Set language code if available
                    if (languageCodeMap.TryGetValue(languageName, out string code))
                    {
                        languageData.Code = code;
                    }

                    languageData.Flags = 0; // 0 means enabled
                    _customLanguageSource.mLanguages.Add(languageData);
                }

                // Populate terms
                foreach (var kvp in localizationData)
                {
                    string key = kvp.Key;
                    Dictionary<string, string> translations = kvp.Value;

                    var termData = new TermData();
                    termData.Term = key;
                    termData.TermType = eTermType.Text;
                    termData.Languages = new string[_customLanguageSource.mLanguages.Count];
                    termData.Flags = new byte[_customLanguageSource.mLanguages.Count];

                    // Fill translations for each language
                    for (int i = 0; i < _customLanguageSource.mLanguages.Count; i++)
                    {
                        string languageName = _customLanguageSource.mLanguages[i].Name;
                        if (translations.TryGetValue(languageName, out string translation))
                        {
                            termData.Languages[i] = translation;
                            termData.Flags[i] = 0; // 0 means translated
                        }
                        else
                        {
                            termData.Languages[i] = string.Empty;
                            termData.Flags[i] = 1; // 1 means missing translation
                        }
                    }

                    _customLanguageSource.mTerms.Add(termData);
                }

                // Update internal dictionary for fast lookups
                _customLanguageSource.UpdateDictionary(force: true);

                // Register with I2 LocalizationManager if not already registered
                if (!LocalizationManager.Sources.Contains(_customLanguageSource))
                {
                    LocalizationManager.AddSource(_customLanguageSource);
                }

                Debug.Log($"[LocalizationI2Bridge] Populated I2 with {_customLanguageSource.mTerms.Count} terms and {_customLanguageSource.mLanguages.Count} languages.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationI2Bridge] Failed to populate I2 LanguageSource: {ex}");
            }
        }

        /// <summary>
        /// Synchronizes the current language with I2 Localization.
        /// </summary>
        /// <param name="languageName">Language name to set as current in I2</param>
        public static void SyncLanguageToI2(string languageName)
        {
            try
            {
                if (!string.IsNullOrEmpty(languageName) && LocalizationManager.HasLanguage(languageName))
                {
                    LocalizationManager.CurrentLanguage = languageName;
                    Debug.Log($"[LocalizationI2Bridge] Set I2 current language to: {languageName}");
                }
                else
                {
                    Debug.LogWarning($"[LocalizationI2Bridge] Language '{languageName}' not found in I2 LocalizationManager.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationI2Bridge] Failed to sync language to I2: {ex}");
            }
        }

        /// <summary>
        /// Gets the current language from I2.
        /// </summary>
        public static string GetI2CurrentLanguage()
        {
            return LocalizationManager.CurrentLanguage;
        }

        /// <summary>
        /// Checks if I2 has been initialized with our custom source.
        /// </summary>
        public static bool IsI2Initialized()
        {
            return _customLanguageSource != null && LocalizationManager.Sources.Contains(_customLanguageSource);
        }

        /// <summary>
        /// Gets a translation from I2 with parameter support.
        /// This leverages I2's parameter replacement system.
        /// </summary>
        /// <param name="key">Localization key</param>
        /// <param name="parameters">Optional parameters for replacement</param>
        /// <returns>Translated and parameterized text</returns>
        public static string GetTranslationFromI2(string key, params object[] parameters)
        {
            try
            {
                string translation = LocalizationManager.GetTranslation(key);
                
                if (!string.IsNullOrEmpty(translation) && parameters != null && parameters.Length > 0)
                {
                    // Support simple indexed parameters: {0}, {1}, etc.
                    translation = string.Format(translation, parameters);
                }

                return translation;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationI2Bridge] Error getting translation for key '{key}': {ex}");
                return key;
            }
        }

        /// <summary>
        /// Clears the custom language source (useful for cleanup or reinitialization).
        /// </summary>
        public static void ClearI2Source()
        {
            if (_customLanguageSource != null)
            {
                LocalizationManager.Sources.Remove(_customLanguageSource);
                _customLanguageSource = null;
                Debug.Log("[LocalizationI2Bridge] Cleared custom I2 language source.");
            }
        }
    }
}

#else

using System.Collections.Generic;
using UnityEngine;

namespace SpiralingStudio.Services.Localization
{
    /// <summary>
    /// Stub replacement for LocalizationI2Bridge when I2 Localization package is not installed.
    /// Define the scripting symbol I2_LOCALIZATION to enable the real implementation.
    /// </summary>
    internal static class LocalizationI2Bridge
    {
        public static void PopulateI2LanguageSource(
            Dictionary<string, Dictionary<string, string>> localizationData,
            List<string> languages,
            Dictionary<string, string> languageCodeMap)
        {
            Debug.Log($"[LocalizationI2Bridge] Stub: PopulateI2LanguageSource called with {localizationData?.Count ?? 0} terms, {languages?.Count ?? 0} languages. I2 Localization not installed.");
        }

        public static void SyncLanguageToI2(string languageName)
        {
            Debug.Log($"[LocalizationI2Bridge] Stub: SyncLanguageToI2('{languageName}'). I2 Localization not installed.");
        }

        public static string GetI2CurrentLanguage()
        {
            return string.Empty;
        }

        public static bool IsI2Initialized()
        {
            return false;
        }

        public static string GetTranslationFromI2(string key, params object[] parameters)
        {
            return key;
        }

        public static void ClearI2Source()
        {
        }
    }
}

#endif
