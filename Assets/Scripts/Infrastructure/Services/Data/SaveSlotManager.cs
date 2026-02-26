using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePack;
using MessagePipe;
using SpiralingStudio.Events;
using UnityEngine;
using VContainer;

namespace SpiralingStudio.Services.DataManagement
{
    /// <summary>
    /// Manages save slots including creation, deletion, switching, and metadata tracking.
    /// </summary>
    public class SaveSlotManager : IMFService
    {
        private readonly MFLocalDBService _dbService;
        private readonly IPublisher<SaveSlotCreatedEvent> _slotCreatedPublisher;
        private readonly IPublisher<SaveSlotDeletedEvent> _slotDeletedPublisher;
        private readonly IPublisher<SaveSlotChangedEvent> _slotChangedPublisher;
        private readonly SaveSlotConfiguration _configuration;
        
        private readonly object _metadataLock = new object();
        private Dictionary<int, SaveSlotMetadata> _slotMetadata;
        private int? _currentSlotIndex;
        private bool _initialized;
        
        // Auto-save slot uses index -1 by convention
        private const int AutoSaveSlotIndex = -1;
        
        /// <summary>
        /// Gets the currently active slot index, or null if no slot is active.
        /// </summary>
        public int? CurrentSlotIndex
        {
            get
            {
                lock (_metadataLock)
                {
                    return _currentSlotIndex;
                }
            }
        }
        
        [Inject]
        public SaveSlotManager(
            MFLocalDBService dbService,
            SaveSlotConfiguration configuration,
            IPublisher<SaveSlotCreatedEvent> slotCreatedPublisher,
            IPublisher<SaveSlotDeletedEvent> slotDeletedPublisher,
            IPublisher<SaveSlotChangedEvent> slotChangedPublisher)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _slotCreatedPublisher = slotCreatedPublisher ?? throw new ArgumentNullException(nameof(slotCreatedPublisher));
            _slotDeletedPublisher = slotDeletedPublisher ?? throw new ArgumentNullException(nameof(slotDeletedPublisher));
            _slotChangedPublisher = slotChangedPublisher ?? throw new ArgumentNullException(nameof(slotChangedPublisher));
            _slotMetadata = new Dictionary<int, SaveSlotMetadata>();
        }
        
        public UniTask[] GetInitializeTasks()
        {
            return new[]
            {
                InitializeSlotSystem()
            };
        }
        
        private async UniTask InitializeSlotSystem()
        {
            try
            {
                Debug.Log("[SaveSlotManager] Initializing save slot system...");
                
                await LoadSlotMetadata();
                
                _initialized = true;
                
                Debug.Log($"[SaveSlotManager] Initialization complete. Found {_slotMetadata.Count} existing slot(s).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSlotManager] Failed to initialize: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Loads slot metadata from disk. Creates a new metadata file if none exists.
        /// </summary>
        private async UniTask LoadSlotMetadata()
        {
            var metadataPath = DataManagerDirectoryHelper.GetSlotMetadataPath();
            
            lock (_metadataLock)
            {
                _slotMetadata.Clear();
            }
            
            if (File.Exists(metadataPath))
            {
                try
                {
                    byte[] data = await File.ReadAllBytesAsync(metadataPath);
                    var metadataList = MessagePackSerializer.Deserialize<List<SaveSlotMetadata>>(data);
                    
                    lock (_metadataLock)
                    {
                        foreach (var metadata in metadataList)
                        {
                            _slotMetadata[metadata.slotIndex] = metadata;
                        }
                    }
                    
                    Debug.Log($"[SaveSlotManager] Loaded metadata for {metadataList.Count} slot(s).");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveSlotManager] Failed to load slot metadata: {ex}");
                    // Continue with empty metadata if load fails
                }
            }
            else
            {
                Debug.Log("[SaveSlotManager] No existing slot metadata found. Starting fresh.");
            }
        }
        
        /// <summary>
        /// Saves slot metadata to disk.
        /// </summary>
        private async UniTask SaveSlotMetadata()
        {
            var metadataPath = DataManagerDirectoryHelper.GetSlotMetadataPath();
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(metadataPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            List<SaveSlotMetadata> metadataList;
            lock (_metadataLock)
            {
                metadataList = _slotMetadata.Values.ToList();
            }
            
            try
            {
                byte[] data = MessagePackSerializer.Serialize(metadataList);
                await File.WriteAllBytesAsync(metadataPath, data);
                Debug.Log($"[SaveSlotManager] Saved metadata for {metadataList.Count} slot(s).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSlotManager] Failed to save slot metadata: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets metadata for all existing save slots.
        /// </summary>
        /// <returns>Read-only list of slot metadata</returns>
        public IReadOnlyList<SaveSlotMetadata> GetAllSlotMetadata()
        {
            ThrowIfNotInitialized();
            
            lock (_metadataLock)
            {
                return _slotMetadata.Values.OrderBy(m => m.slotIndex).ToList();
            }
        }
        
        /// <summary>
        /// Gets metadata for a specific slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to retrieve</param>
        /// <returns>Slot metadata, or null if slot doesn't exist</returns>
        public SaveSlotMetadata GetSlotMetadata(int slotIndex)
        {
            ThrowIfNotInitialized();
            
            lock (_metadataLock)
            {
                return _slotMetadata.TryGetValue(slotIndex, out var metadata) ? metadata : null;
            }
        }
        
        /// <summary>
        /// Creates a new manual save slot.
        /// </summary>
        /// <param name="displayName">Player-provided display name for the slot</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The index of the newly created slot</returns>
        public async UniTask<int> CreateNewSlot(string displayName, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be null or empty.", nameof(displayName));
            }
            
            // Find next available slot index
            int newSlotIndex = GetNextAvailableSlotIndex();
            
            // Check slot limit
            if (_configuration.MaxManualSlots > 0)
            {
                int manualSlotCount = GetManualSlotCount();
                if (manualSlotCount >= _configuration.MaxManualSlots)
                {
                    throw new InvalidOperationException($"Cannot create new slot. Maximum of {_configuration.MaxManualSlots} manual slots reached.");
                }
            }
            
            // Create metadata
            var metadata = new SaveSlotMetadata(
                slotIndex: newSlotIndex,
                displayName: displayName,
                creationDate: DateTime.UtcNow,
                lastPlayedDate: DateTime.UtcNow,
                totalPlaytimeSeconds: 0,
                progressIndicator: "New Game"
            );
            
            lock (_metadataLock)
            {
                _slotMetadata[newSlotIndex] = metadata;
            }
            
            // Save metadata to disk
            await SaveSlotMetadata();
            
            // Publish event
            _slotCreatedPublisher.Publish(new SaveSlotCreatedEvent(newSlotIndex, displayName));
            
            Debug.Log($"[SaveSlotManager] Created new slot {newSlotIndex}: '{displayName}'");
            
            return newSlotIndex;
        }
        
        /// <summary>
        /// Gets or creates the auto-save slot.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Auto-save slot metadata</returns>
        public async UniTask<SaveSlotMetadata> GetOrCreateAutoSaveSlot(CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            
            if (!_configuration.EnableAutoSave)
            {
                throw new InvalidOperationException("Auto-save is disabled in configuration.");
            }
            
            lock (_metadataLock)
            {
                if (_slotMetadata.TryGetValue(AutoSaveSlotIndex, out var existing))
                {
                    return existing;
                }
            }
            
            // Create auto-save slot
            var metadata = new SaveSlotMetadata(
                slotIndex: AutoSaveSlotIndex,
                displayName: "Auto Save",
                creationDate: DateTime.UtcNow,
                lastPlayedDate: DateTime.UtcNow,
                totalPlaytimeSeconds: 0,
                progressIndicator: "New Game"
            );
            
            lock (_metadataLock)
            {
                _slotMetadata[AutoSaveSlotIndex] = metadata;
            }
            
            await SaveSlotMetadata();
            
            Debug.Log("[SaveSlotManager] Created auto-save slot.");
            
            return metadata;
        }
        
        /// <summary>
        /// Switches to a different save slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to switch to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async UniTask SwitchToSlot(int slotIndex, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            
            // Verify slot exists in metadata (database file is created lazily by MFLocalDBService.SwitchToSlotDatabase)
            bool slotExistsInMetadata;
            lock (_metadataLock)
            {
                slotExistsInMetadata = _slotMetadata.ContainsKey(slotIndex);
            }
            
            if (!slotExistsInMetadata)
            {
                throw new ArgumentException($"Save slot {slotIndex} does not exist.", nameof(slotIndex));
            }
            
            int? oldSlotIndex;
            lock (_metadataLock)
            {
                oldSlotIndex = _currentSlotIndex;
                
                if (oldSlotIndex == slotIndex)
                {
                    Debug.Log($"[SaveSlotManager] Already on slot {slotIndex}. No switch needed.");
                    return;
                }
            }
            
            // Switch database
            await _dbService.SwitchToSlotDatabase(slotIndex, cancellationToken);
            
            lock (_metadataLock)
            {
                _currentSlotIndex = slotIndex;
                
                // Update last played date
                if (_slotMetadata.TryGetValue(slotIndex, out var metadata))
                {
                    metadata.LastPlayedDate = DateTime.UtcNow;
                }
            }
            
            // Save updated metadata
            await SaveSlotMetadata();
            
            // Publish event
            _slotChangedPublisher.Publish(new SaveSlotChangedEvent(oldSlotIndex ?? -999, slotIndex));
            
            Debug.Log($"[SaveSlotManager] Switched from slot {oldSlotIndex?.ToString() ?? "none"} to slot {slotIndex}.");
        }
        
        /// <summary>
        /// Deletes a save slot and all its data.
        /// </summary>
        /// <param name="slotIndex">Slot index to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async UniTask DeleteSlot(int slotIndex, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            
            // Cannot delete currently active slot
            lock (_metadataLock)
            {
                if (_currentSlotIndex == slotIndex)
                {
                    throw new InvalidOperationException($"Cannot delete slot {slotIndex} while it is active. Switch to a different slot first.");
                }
                
                if (!_slotMetadata.ContainsKey(slotIndex))
                {
                    Debug.LogWarning($"[SaveSlotManager] Slot {slotIndex} does not exist. Nothing to delete.");
                    return;
                }
                
                _slotMetadata.Remove(slotIndex);
            }
            
            // Delete slot folder and database
            DataManagerDirectoryHelper.DeleteSlot(slotIndex);
            
            // Save updated metadata
            await SaveSlotMetadata();
            
            // Publish event
            _slotDeletedPublisher.Publish(new SaveSlotDeletedEvent(slotIndex));
            
            Debug.Log($"[SaveSlotManager] Deleted slot {slotIndex}.");
        }
        
        /// <summary>
        /// Updates metadata for a specific slot.
        /// </summary>
        /// <param name="slotIndex">Slot index to update</param>
        /// <param name="metadata">Updated metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async UniTask UpdateSlotMetadata(int slotIndex, SaveSlotMetadata metadata, CancellationToken cancellationToken = default)
        {
            ThrowIfNotInitialized();
            
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }
            
            if (metadata.slotIndex != slotIndex)
            {
                throw new ArgumentException("Metadata slot index does not match provided slot index.");
            }
            
            lock (_metadataLock)
            {
                if (!_slotMetadata.ContainsKey(slotIndex))
                {
                    throw new ArgumentException($"Slot {slotIndex} does not exist.");
                }
                
                _slotMetadata[slotIndex] = metadata;
            }
            
            await SaveSlotMetadata();
            
            Debug.Log($"[SaveSlotManager] Updated metadata for slot {slotIndex}.");
        }
        
        /// <summary>
        /// Gets a list of available slot indices (slots that don't exist yet).
        /// </summary>
        /// <param name="maxToReturn">Maximum number of available indices to return</param>
        /// <returns>List of available slot indices</returns>
        public List<int> GetAvailableSlotIndices(int maxToReturn = 10)
        {
            ThrowIfNotInitialized();
            
            var available = new List<int>();
            int index = 0;
            
            lock (_metadataLock)
            {
                while (available.Count < maxToReturn)
                {
                    if (!_slotMetadata.ContainsKey(index))
                    {
                        available.Add(index);
                    }
                    index++;
                }
            }
            
            return available;
        }
        
        /// <summary>
        /// Checks if a slot exists.
        /// </summary>
        /// <param name="slotIndex">Slot index to check</param>
        /// <returns>True if the slot exists</returns>
        public bool SlotExists(int slotIndex)
        {
            ThrowIfNotInitialized();
            
            lock (_metadataLock)
            {
                return _slotMetadata.ContainsKey(slotIndex);
            }
        }
        
        /// <summary>
        /// Checks if a new manual save slot can be created.
        /// </summary>
        /// <returns>True if slots are available or unlimited, false if at maximum capacity</returns>
        public bool CanCreateNewSlot()
        {
            ThrowIfNotInitialized();
            
            if (_configuration.MaxManualSlots <= 0)
            {
                return true; // Unlimited slots
            }
            
            return GetManualSlotCount() < _configuration.MaxManualSlots;
        }
        
        private int GetNextAvailableSlotIndex()
        {
            lock (_metadataLock)
            {
                int index = 0;
                while (_slotMetadata.ContainsKey(index))
                {
                    index++;
                }
                return index;
            }
        }
        
        private int GetManualSlotCount()
        {
            lock (_metadataLock)
            {
                return _slotMetadata.Count(kvp => kvp.Key >= 0);
            }
        }
        
        private void ThrowIfNotInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("[SaveSlotManager] Service not initialized. Call GetInitializeTasks and await them first.");
            }
        }
    }
}

