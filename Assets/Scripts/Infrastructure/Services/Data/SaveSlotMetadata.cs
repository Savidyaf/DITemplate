using System;
using MessagePack;

namespace SpiralingStudio.Services.DataManagement
{
    /// <summary>
    /// Metadata for a save slot, including display information and timestamps.
    /// Stored separately from the actual save data for quick slot list population.
    /// </summary>
    [MessagePackObject]
    [MFDataObject("SaveSlotMetadata", false, false)]
    public class SaveSlotMetadata : MFSaveData
    {
        /// <summary>
        /// The slot index. -1 for auto-save, 0+ for manual slots.
        /// </summary>
        [Key(0)]
        public int slotIndex;
        
        /// <summary>
        /// Display name given by the player.
        /// </summary>
        [Key(1)]
        public string displayName;
        
        /// <summary>
        /// When this save slot was created.
        /// </summary>
        [Key(2)]
        public DateTime creationDate;
        
        /// <summary>
        /// When this save slot was last played/saved.
        /// </summary>
        [Key(3)]
        public DateTime lastPlayedDate;
        
        /// <summary>
        /// Total playtime in seconds for this save slot.
        /// </summary>
        [Key(4)]
        public long totalPlaytimeSeconds;
        
        /// <summary>
        /// Game-specific progress indicator (e.g., "Level 5", "World 2", "50% Complete").
        /// </summary>
        [Key(5)]
        public string progressIndicator;
        
        /// <summary>
        /// Parameterless constructor for MessagePack deserialization.
        /// </summary>
        public SaveSlotMetadata()
        {
            slotIndex = 0;
            displayName = string.Empty;
            creationDate = DateTime.UtcNow;
            lastPlayedDate = DateTime.UtcNow;
            totalPlaytimeSeconds = 0;
            progressIndicator = string.Empty;
        }
        
        /// <summary>
        /// Creates a new save slot metadata instance.
        /// </summary>
        [SerializationConstructor]
        public SaveSlotMetadata(int slotIndex, string displayName, DateTime creationDate, 
            DateTime lastPlayedDate, long totalPlaytimeSeconds, string progressIndicator)
        {
            this.slotIndex = slotIndex;
            this.displayName = displayName ?? string.Empty;
            this.creationDate = creationDate;
            this.lastPlayedDate = lastPlayedDate;
            this.totalPlaytimeSeconds = totalPlaytimeSeconds;
            this.progressIndicator = progressIndicator ?? string.Empty;
        }
        
        /// <summary>
        /// Property accessors with INotifyPropertyChanged support.
        /// </summary>
        [IgnoreMember]
        public int SlotIndex
        {
            get => slotIndex;
            set => SetField(ref slotIndex, value);
        }
        
        [IgnoreMember]
        public string DisplayName
        {
            get => displayName;
            set => SetField(ref displayName, value);
        }
        
        [IgnoreMember]
        public DateTime CreationDate
        {
            get => creationDate;
            set => SetField(ref creationDate, value);
        }
        
        [IgnoreMember]
        public DateTime LastPlayedDate
        {
            get => lastPlayedDate;
            set => SetField(ref lastPlayedDate, value);
        }
        
        [IgnoreMember]
        public long TotalPlaytimeSeconds
        {
            get => totalPlaytimeSeconds;
            set => SetField(ref totalPlaytimeSeconds, value);
        }
        
        [IgnoreMember]
        public string ProgressIndicator
        {
            get => progressIndicator;
            set => SetField(ref progressIndicator, value);
        }
        
        /// <summary>
        /// Checks if this is an auto-save slot.
        /// </summary>
        [IgnoreMember]
        public bool IsAutoSave => slotIndex == -1;
        
        public override string ToString()
        {
            return $"Slot {(IsAutoSave ? "Auto" : slotIndex.ToString())}: {displayName} (Last Played: {lastPlayedDate})";
        }
    }
}

