using UnityEngine;

namespace SpiralingStudio.Services.DataManagement
{
    /// <summary>
    /// Configuration for the save slot system.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveSlotConfiguration", menuName = "SpiralingStudio/Data/Save Slot Configuration")]
    public class SaveSlotConfiguration : ScriptableObject
    {
        [Header("Slot Configuration")]
        [Tooltip("Maximum number of manual save slots allowed (0 for unlimited)")]
        [SerializeField] private int maxManualSlots = 10;
        
        [Header("Auto-Save Configuration")]
        [Tooltip("Enable automatic saving to a dedicated auto-save slot")]
        [SerializeField] private bool enableAutoSave = true;
        
        [Tooltip("Time interval between auto-saves in seconds (0 to disable periodic auto-save)")]
        [SerializeField] private float autoSaveIntervalSeconds = 300f; // 5 minutes default
        
        /// <summary>
        /// Maximum number of manual save slots. Returns 0 for unlimited slots.
        /// </summary>
        public int MaxManualSlots => maxManualSlots;
        
        /// <summary>
        /// Whether auto-save is enabled.
        /// </summary>
        public bool EnableAutoSave => enableAutoSave;
        
        /// <summary>
        /// Auto-save interval in seconds. 0 means no periodic auto-save.
        /// </summary>
        public float AutoSaveIntervalSeconds => autoSaveIntervalSeconds;
        
        /// <summary>
        /// Validates the configuration values.
        /// </summary>
        private void OnValidate()
        {
            if (maxManualSlots < 0)
            {
                maxManualSlots = 0;
            }
            
            if (autoSaveIntervalSeconds < 0)
            {
                autoSaveIntervalSeconds = 0;
            }
        }
    }
}

