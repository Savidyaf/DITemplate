# Multi-Slot Save System Documentation

## Overview

The multi-slot save system provides a robust infrastructure for managing multiple save files in your game. Each save slot has its own isolated database, and players can create, delete, and switch between save slots seamlessly.

## Key Features

- **Multiple Save Slots**: Support for unlimited or configurable number of manual save slots
- **Auto-Save Slot**: Dedicated auto-save slot (index -1) that doesn't count toward manual slot limits
- **Slot Metadata**: Each slot stores display name, creation date, last played date, playtime, and custom progress indicators
- **Database Isolation**: Each slot has its own SQLite database in a separate folder
- **Thread-Safe**: All operations are thread-safe for concurrent access
- **Event-Driven**: Publishes events for slot creation, deletion, and switching

## Architecture Components

### 1. SaveSlotConfiguration (ScriptableObject)
Configuration asset that defines system behavior:
- `MaxManualSlots`: Maximum number of manual save slots (0 for unlimited)
- `EnableAutoSave`: Whether auto-save functionality is enabled
- `AutoSaveIntervalSeconds`: Time between auto-saves (0 to disable periodic auto-save)

**Setup**: Create in Unity via `Assets > Create > SpiralingStudio > Data > Save Slot Configuration`

### 2. SaveSlotMetadata
Stores information about each save slot:
```csharp
- slotIndex (int): -1 for auto-save, 0+ for manual slots
- displayName (string): Player-provided name
- creationDate (DateTime): When the slot was created
- lastPlayedDate (DateTime): Last time the slot was used
- totalPlaytimeSeconds (long): Total playtime in seconds
- progressIndicator (string): Game-specific progress (e.g., "Level 5", "50% Complete")
```

### 3. SaveSlotManager (Service)
Central manager for all slot operations:
- `CreateNewSlot(displayName)`: Creates a new manual save slot
- `GetOrCreateAutoSaveSlot()`: Gets/creates the auto-save slot
- `SwitchToSlot(slotIndex)`: Changes the active database connection
- `DeleteSlot(slotIndex)`: Removes a slot and its data
- `GetAllSlotMetadata()`: Lists all existing slots
- `UpdateSlotMetadata(slotIndex, metadata)`: Updates slot information
- `SlotExists(slotIndex)`: Checks if a slot exists
- `GetAvailableSlotIndices(maxToReturn)`: Gets available slot numbers

### 4. MFLocalDBService (Enhanced)
Database service with multi-slot support:
- `SwitchToSlotDatabase(slotIndex)`: Connects to a specific slot's database
- `CloseCurrentDatabase()`: Closes current connection
- `CurrentSlotIndex`: Gets the active slot index (nullable)

### 5. SessionManager (Enhanced)
Session management with slot tracking:
- `CreateSession(userId, activeSlotIndex)`: Creates session with optional slot
- `SetActiveSlot(slotIndex)`: Updates session's active slot
- `ClearActiveSlot()`: Removes active slot from session

### 6. Events
Published during slot operations:
- `SaveSlotCreatedEvent`: When a new slot is created
- `SaveSlotDeletedEvent`: When a slot is deleted
- `SaveSlotChangedEvent`: When switching between slots

## File System Structure

```
persistentDataPath/
  SpiralingStudioUserData/
    SaveData/
      SlotMetadata.dat              # Master metadata file (MessagePack serialized)
      Slot_Auto/                    # Auto-save slot folder
        SaveGameDB                  # SQLite database for auto-save
      Slot_0/                       # Manual slot 0
        SaveGameDB                  # SQLite database for slot 0
      Slot_1/                       # Manual slot 1
        SaveGameDB
      Slot_2/
        SaveGameDB
      ...
```

## Usage Examples

### Basic Setup in GameLifetimeScope

1. Create a `SaveSlotConfiguration` asset in Unity
2. Assign it to the `GameLifetimeScope` component's `SaveSlotConfiguration` field
3. The system will automatically initialize during game startup

### Creating a New Save Slot

```csharp
[Inject] private SaveSlotManager _saveSlotManager;

public async UniTask CreateNewGame()
{
    // Create a new save slot with player's chosen name
    int slotIndex = await _saveSlotManager.CreateNewSlot("My Adventure", CancellationToken.None);
    
    // Switch to the new slot
    await _saveSlotManager.SwitchToSlot(slotIndex, CancellationToken.None);
    
    // Update session
    _sessionManager.SetActiveSlot(slotIndex);
    
    // Now ready to save game data
}
```

### Saving Game Data

```csharp
[Inject] private MFLocalDBService _dbService;
[Inject] private SaveSlotManager _saveSlotManager;

public async UniTask SaveGame()
{
    // Ensure a slot is active
    if (_saveSlotManager.CurrentSlotIndex == null)
    {
        Debug.LogError("No active slot!");
        return;
    }
    
    // Create your save data (extend MFSaveData)
    var playerData = new PlayerSaveData 
    {
        PlayerName = "Hero",
        Level = 10,
        Gold = 5000
    };
    
    // Save to database
    await _dbService.WriteDataToRuntimeDatabase("PlayerData", CancellationToken.None, playerData);
    
    // Update slot metadata
    var metadata = _saveSlotManager.GetSlotMetadata(_saveSlotManager.CurrentSlotIndex.Value);
    metadata.LastPlayedDate = DateTime.UtcNow;
    metadata.ProgressIndicator = $"Level {playerData.Level}";
    metadata.TotalPlaytimeSeconds += GetSessionPlaytime();
    
    await _saveSlotManager.UpdateSlotMetadata(metadata.slotIndex, metadata, CancellationToken.None);
}
```

### Loading a Save Slot

```csharp
public async UniTask LoadGame(int slotIndex)
{
    // Switch to the desired slot
    await _saveSlotManager.SwitchToSlot(slotIndex, CancellationToken.None);
    _sessionManager.SetActiveSlot(slotIndex);
    
    // Load game data
    var playerData = await _dbService.FetchDataFromRuntimeDatabase<PlayerSaveData>("PlayerData", CancellationToken.None);
    
    if (playerData != null)
    {
        // Apply loaded data to game state
        ApplyPlayerData(playerData);
    }
    else
    {
        // New game - no data exists yet
        StartNewGameInSlot(slotIndex);
    }
}
```

### Displaying Save Slot List (UI)

```csharp
public void DisplaySaveSlots()
{
    var slots = _saveSlotManager.GetAllSlotMetadata();
    
    foreach (var slot in slots)
    {
        string slotName = slot.IsAutoSave ? "[AUTO SAVE]" : slot.displayName;
        string lastPlayed = slot.lastPlayedDate.ToString("g");
        string playtime = FormatPlaytime(slot.totalPlaytimeSeconds);
        string progress = slot.progressIndicator;
        
        // Create UI element showing:
        // - Slot name
        // - Last played date
        // - Total playtime
        // - Progress indicator
        Debug.Log($"{slotName} | {progress} | {playtime} | Last: {lastPlayed}");
    }
}
```

### Auto-Save Implementation

```csharp
public async UniTask PerformAutoSave()
{
    var autoSaveSlot = await _saveSlotManager.GetOrCreateAutoSaveSlot(CancellationToken.None);
    
    // Store current slot
    int? previousSlot = _saveSlotManager.CurrentSlotIndex;
    
    // Switch to auto-save slot temporarily
    await _saveSlotManager.SwitchToSlot(autoSaveSlot.slotIndex, CancellationToken.None);
    
    // Save current game state
    await SaveCurrentGameState();
    
    // Update auto-save metadata
    autoSaveSlot.LastPlayedDate = DateTime.UtcNow;
    await _saveSlotManager.UpdateSlotMetadata(autoSaveSlot.slotIndex, autoSaveSlot, CancellationToken.None);
    
    // Switch back to previous slot
    if (previousSlot.HasValue && previousSlot.Value != autoSaveSlot.slotIndex)
    {
        await _saveSlotManager.SwitchToSlot(previousSlot.Value, CancellationToken.None);
    }
}
```

### Periodic Auto-Save with UniTask

```csharp
public class AutoSaveController : MonoBehaviour
{
    [Inject] private SaveSlotConfiguration _config;
    [Inject] private SaveSlotManager _saveSlotManager;
    
    private CancellationTokenSource _autoSaveCts;
    
    private void Start()
    {
        if (_config.EnableAutoSave && _config.AutoSaveIntervalSeconds > 0)
        {
            _autoSaveCts = new CancellationTokenSource();
            PeriodicAutoSave(_autoSaveCts.Token).Forget();
        }
    }
    
    private async UniTaskVoid PeriodicAutoSave(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_config.AutoSaveIntervalSeconds), cancellationToken: cancellationToken);
            
            try
            {
                await PerformAutoSave();
                Debug.Log("Auto-save complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Auto-save failed: {ex}");
            }
        }
    }
    
    private void OnDestroy()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
    }
}
```

### Deleting a Save Slot

```csharp
public async UniTask DeleteSaveSlot(int slotIndex)
{
    // Cannot delete the currently active slot
    if (_saveSlotManager.CurrentSlotIndex == slotIndex)
    {
        Debug.LogError("Cannot delete active slot. Switch to another slot first.");
        return;
    }
    
    // Delete the slot and all its data
    await _saveSlotManager.DeleteSlot(slotIndex, CancellationToken.None);
    
    Debug.Log($"Slot {slotIndex} deleted successfully");
}
```

### Subscribing to Slot Events

```csharp
public class SaveSlotEventHandler : MonoBehaviour
{
    [Inject] private ISubscriber<SaveSlotCreatedEvent> _slotCreatedSub;
    [Inject] private ISubscriber<SaveSlotDeletedEvent> _slotDeletedSub;
    [Inject] private ISubscriber<SaveSlotChangedEvent> _slotChangedSub;
    
    private DisposableBagBuilder _disposables;
    
    private void Start()
    {
        _disposables = DisposableBag.CreateBuilder();
        
        _slotCreatedSub.Subscribe(OnSlotCreated).AddTo(_disposables);
        _slotDeletedSub.Subscribe(OnSlotDeleted).AddTo(_disposables);
        _slotChangedSub.Subscribe(OnSlotChanged).AddTo(_disposables);
    }
    
    private void OnSlotCreated(SaveSlotCreatedEvent evt)
    {
        Debug.Log($"New slot created: {evt.SlotIndex} - {evt.DisplayName}");
        RefreshSaveSlotUI();
    }
    
    private void OnSlotDeleted(SaveSlotDeletedEvent evt)
    {
        Debug.Log($"Slot deleted: {evt.SlotIndex}");
        RefreshSaveSlotUI();
    }
    
    private void OnSlotChanged(SaveSlotChangedEvent evt)
    {
        Debug.Log($"Switched from slot {evt.OldSlotIndex} to slot {evt.NewSlotIndex}");
        LoadNewSlotData();
    }
    
    private void OnDestroy()
    {
        _disposables?.Build()?.Dispose();
    }
}
```

## Best Practices

### 1. Always Check for Active Slot
Before saving or loading, verify that a slot is active:
```csharp
if (_saveSlotManager.CurrentSlotIndex == null)
{
    // Handle no active slot case
    return;
}
```

### 2. Update Metadata After Saves
Keep metadata up-to-date for accurate UI display:
```csharp
var metadata = _saveSlotManager.GetSlotMetadata(currentSlot);
metadata.LastPlayedDate = DateTime.UtcNow;
metadata.TotalPlaytimeSeconds += sessionTime;
await _saveSlotManager.UpdateSlotMetadata(currentSlot, metadata);
```

### 3. Handle Slot Switching Gracefully
Save current state before switching slots to avoid data loss:
```csharp
public async UniTask SwitchSlot(int newSlotIndex)
{
    // Save current slot before switching
    if (_saveSlotManager.CurrentSlotIndex.HasValue)
    {
        await SaveGame();
    }
    
    // Switch to new slot
    await _saveSlotManager.SwitchToSlot(newSlotIndex, CancellationToken.None);
    
    // Load new slot's data
    await LoadGame(newSlotIndex);
}
```

### 4. Use Try-Catch for Error Handling
Slot operations can fail due to file system issues:
```csharp
try
{
    await _saveSlotManager.CreateNewSlot(displayName);
}
catch (Exception ex)
{
    Debug.LogError($"Failed to create slot: {ex}");
    ShowErrorToPlayer("Could not create save slot");
}
```

### 5. Validate Slot Existence Before Loading
Check if a slot exists before attempting to load:
```csharp
if (!_saveSlotManager.SlotExists(slotIndex))
{
    Debug.LogWarning($"Slot {slotIndex} does not exist");
    return;
}
```

## Migration from Old System

If you have existing save data from the old single-database system, you'll need to migrate it:

1. The old database was at: `Application.persistentDataPath/SaveGameDB`
2. New slot databases are at: `Application.persistentDataPath/SpiralingStudioUserData/SaveData/Slot_X/SaveGameDB`

You can create a migration script to copy the old database to Slot_0 if needed.

## Troubleshooting

### "No database connection active" Error
- Make sure to call `SwitchToSlotDatabase()` before trying to save/load data
- Verify that a slot has been created and selected

### "Service not initialized" Error
- Ensure `SaveSlotManager` is registered in `ServiceRegistrationHelper`
- Check that `SaveSlotConfiguration` is assigned in `GameLifetimeScope`
- Wait for all service initialization tasks to complete

### Slots Not Appearing in List
- Check that `SlotMetadata.dat` file exists and is not corrupted
- Verify that slot folders exist in the file system
- Call `GetAllSlotMetadata()` after initialization completes

### Auto-Save Not Working
- Verify `EnableAutoSave` is true in configuration
- Check that `AutoSaveIntervalSeconds` is greater than 0
- Ensure auto-save coroutine/task is running

## Performance Considerations

- Switching slots closes and reopens database connections (slight delay)
- Metadata updates write to disk (use sparingly)
- Consider batching multiple data saves before updating metadata
- Auto-save frequency should balance player convenience with performance

## Thread Safety

All public methods in `SaveSlotManager` and `MFLocalDBService` are thread-safe and can be called from any thread. However, Unity API calls (like Debug.Log) should only be made on the main thread.

