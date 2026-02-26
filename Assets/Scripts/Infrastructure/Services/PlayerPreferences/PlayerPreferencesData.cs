using System;
using UnityEngine;

namespace SpiralingStudio.Services.PlayerPreferences
{
    /// <summary>
    /// Data class for player preferences that are stored independently of save slots.
    /// Serialized to JSON in the persistent data path.
    /// </summary>
    [Serializable]
    public class PlayerPreferencesData
    {
        [Header("Display Settings")]
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public bool fullscreen = true;
        public float fieldOfView = 60f;
        
        [Header("Graphics Settings")]
        public int qualityPreset = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
        
        [Header("Audio Settings")]
        [Range(0f, 1f)]
        public float masterVolume = 1.0f;
        [Range(0f, 1f)]
        public float musicVolume = 0.8f;
        [Range(0f, 1f)]
        public float sfxVolume = 1.0f;
        
        [Header("Localization")]
        public string languageCode = "en"; // ISO language code (en, es, fr, etc.)
        
        /// <summary>
        /// Creates default player preferences.
        /// </summary>
        public static PlayerPreferencesData CreateDefault()
        {
            return new PlayerPreferencesData
            {
                resolutionWidth = Screen.currentResolution.width,
                resolutionHeight = Screen.currentResolution.height,
                fullscreen = Screen.fullScreen,
                fieldOfView = 60f,
                qualityPreset = QualitySettings.GetQualityLevel(),
                masterVolume = 1.0f,
                musicVolume = 0.8f,
                sfxVolume = 1.0f,
                languageCode = "en"
            };
        }
        
        /// <summary>
        /// Validates and clamps preference values to acceptable ranges.
        /// </summary>
        public void Validate()
        {
            resolutionWidth = Mathf.Max(640, resolutionWidth);
            resolutionHeight = Mathf.Max(480, resolutionHeight);
            fieldOfView = Mathf.Clamp(fieldOfView, 30f, 120f);
            qualityPreset = Mathf.Clamp(qualityPreset, 0, QualitySettings.names.Length - 1);
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            
            if (string.IsNullOrEmpty(languageCode))
            {
                languageCode = "en";
            }
        }
    }
}



