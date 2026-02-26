using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using SpiralingStudio.Services.DataManagement;
using SpiralingStudio.Services.Localization;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace SpiralingStudio.Services.PlayerPreferences
{
    /// <summary>
    /// Service for managing player preferences that persist independently of save slots.
    /// Preferences are stored as JSON in the persistent data path.
    /// </summary>
    public class PlayerPreferencesService : IMFService
    {
        private readonly MFLocalizationService _localizationService;
        private PlayerPreferencesData _currentPreferences;
        private bool _initialized;
        private readonly object _prefsLock = new object();
        
        /// <summary>
        /// Gets the current player preferences. Returns null if not yet loaded.
        /// </summary>
        public PlayerPreferencesData CurrentPreferences
        {
            get
            {
                lock (_prefsLock)
                {
                    return _currentPreferences;
                }
            }
        }
        
        [Inject]
        public PlayerPreferencesService(MFLocalizationService localizationService)
        {
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        }
        
        public UniTask[] GetInitializeTasks()
        {
            return new[]
            {
                InitializeService()
            };
        }
        
        private async UniTask InitializeService()
        {
            try
            {
                Debug.Log("[PlayerPreferencesService] Initializing...");
                
                // Load preferences from disk (or create defaults)
                await LoadPreferences();
                _initialized = true;
                Debug.Log("[PlayerPreferencesService] Initialization complete.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerPreferencesService] Failed to initialize: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Loads player preferences from disk. Creates default preferences if file doesn't exist.
        /// </summary>
        public async UniTask LoadPreferences(CancellationToken cancellationToken = default)
        {
            string prefsPath = DataManagerDirectoryHelper.GetPlayerPreferencesPath();
            
            try
            {
                if (File.Exists(prefsPath))
                {
                    string json = await File.ReadAllTextAsync(prefsPath, cancellationToken);
                    PlayerPreferencesData loadedPrefs = JsonUtility.FromJson<PlayerPreferencesData>(json);
                    
                    if (loadedPrefs != null)
                    {
                        loadedPrefs.Validate();
                        
                        lock (_prefsLock)
                        {
                            _currentPreferences = loadedPrefs;
                        }
                        
                        Debug.Log("[PlayerPreferencesService] Loaded preferences from disk.");
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerPreferencesService] Failed to deserialize preferences. Using defaults.");
                        CreateDefaultPreferences();
                    }
                }
                else
                {
                    Debug.Log("[PlayerPreferencesService] No existing preferences found. Creating defaults.");
                    CreateDefaultPreferences();
                    _initialized = true;
                    await SavePreferences(cancellationToken); // Save defaults to disk
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerPreferencesService] Error loading preferences: {ex}");
                CreateDefaultPreferences();
            }
        }
        
        /// <summary>
        /// Saves current player preferences to disk.
        /// </summary>
        public async UniTask SavePreferences(CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            
            string prefsPath = DataManagerDirectoryHelper.GetPlayerPreferencesPath();
            
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(prefsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                PlayerPreferencesData prefsToSave;
                lock (_prefsLock)
                {
                    prefsToSave = _currentPreferences;
                }
                
                if (prefsToSave == null)
                {
                    Debug.LogWarning("[PlayerPreferencesService] Cannot save null preferences.");
                    return;
                }
                
                prefsToSave.Validate();
                
                string json = JsonUtility.ToJson(prefsToSave, true);
                await File.WriteAllTextAsync(prefsPath, json, cancellationToken);
                
                Debug.Log($"[PlayerPreferencesService] Saved preferences to {prefsPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerPreferencesService] Error saving preferences: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Applies current preferences to Unity systems (graphics, audio, localization).
        /// </summary>
        public void ApplyPreferences()
        {
            ThrowIfNotInitialized();
            
            PlayerPreferencesData prefs;
            lock (_prefsLock)
            {
                prefs = _currentPreferences;
            }
            
            if (prefs == null)
            {
                Debug.LogWarning("[PlayerPreferencesService] Cannot apply null preferences.");
                return;
            }
            
            try
            {
                Debug.Log("[PlayerPreferencesService] Applying preferences...");
                
                // Apply display settings
                Screen.SetResolution(prefs.resolutionWidth, prefs.resolutionHeight, prefs.fullscreen);
                Debug.Log($"[PlayerPreferencesService] Set resolution: {prefs.resolutionWidth}x{prefs.resolutionHeight}, Fullscreen: {prefs.fullscreen}");
                
                // Apply graphics quality
                QualitySettings.SetQualityLevel(prefs.qualityPreset, true);
                Debug.Log($"[PlayerPreferencesService] Set quality level: {prefs.qualityPreset} ({QualitySettings.names[prefs.qualityPreset]})");
                
                // Apply audio settings
                AudioListener.volume = prefs.masterVolume;
                Debug.Log($"[PlayerPreferencesService] Set master volume: {prefs.masterVolume}");
                
                // Note: Music and SFX volumes would typically be applied via AudioMixer
                // For now, we'll just log them. Implement AudioMixer integration as needed.
                Debug.Log($"[PlayerPreferencesService] Music volume: {prefs.musicVolume}, SFX volume: {prefs.sfxVolume}");
                
                // Apply localization
                if (_localizationService != null)
                {
                    _localizationService.SetLanguage(prefs.languageCode);
                    Debug.Log($"[PlayerPreferencesService] Set language: {prefs.languageCode}");
                }
                
                // Apply FOV (this would typically be set on the camera in-game)
                // For now, just store it - game code will read it when creating cameras
                Debug.Log($"[PlayerPreferencesService] FOV preference: {prefs.fieldOfView}");
                
                Debug.Log("[PlayerPreferencesService] Preferences applied successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerPreferencesService] Error applying preferences: {ex}");
            }
        }
        
        /// <summary>
        /// Updates a specific preference value and optionally saves to disk.
        /// </summary>
        public async UniTask UpdatePreference(Action<PlayerPreferencesData> updateAction, bool saveImmediately = true, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            
            if (updateAction == null)
            {
                throw new ArgumentNullException(nameof(updateAction));
            }
            
            lock (_prefsLock)
            {
                if (_currentPreferences != null)
                {
                    updateAction(_currentPreferences);
                    _currentPreferences.Validate();
                }
            }
            
            if (saveImmediately)
            {
                await SavePreferences(cancellationToken);
            }
        }
        
        private void CreateDefaultPreferences()
        {
            lock (_prefsLock)
            {
                _currentPreferences = PlayerPreferencesData.CreateDefault();
            }
        }
        
        private void ThrowIfNotInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("[PlayerPreferencesService] Service not initialized. Call GetInitializeTasks and await them first.");
            }
        }
    }
}






