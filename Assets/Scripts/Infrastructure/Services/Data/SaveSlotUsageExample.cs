using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace SpiralingStudio.Services.DataManagement
{
    /// <summary>
    /// Example demonstrating how to use the multi-slot save system.
    /// This class shows the complete workflow for creating, switching, and managing save slots.
    /// </summary>
    public class SaveSlotUsageExample : MonoBehaviour
    {
        [Inject] private SaveSlotManager _saveSlotManager;
        [Inject] private MFLocalDBService _dbService;
        [Inject] private SpiralingStudio.Services.Session.SessionManager _sessionManager;
        
        /// <summary>
        /// Example workflow demonstrating the save slot system.
        /// </summary>
        public async UniTaskVoid ExampleWorkflow()
        {
            try
            {
                Debug.Log("===== Save Slot System Example Workflow =====");
                
                // 1. Create a new manual save slot
                Debug.Log("\n--- Creating New Save Slot ---");
                int slot1Index = await _saveSlotManager.CreateNewSlot("My First Adventure", CancellationToken.None);
                Debug.Log($"Created slot {slot1Index}");
                
                // 2. Create another save slot
                int slot2Index = await _saveSlotManager.CreateNewSlot("Hard Mode Playthrough", CancellationToken.None);
                Debug.Log($"Created slot {slot2Index}");
                
                // 3. List all available slots
                Debug.Log("\n--- Listing All Slots ---");
                var allSlots = _saveSlotManager.GetAllSlotMetadata();
                foreach (var slot in allSlots)
                {
                    Debug.Log($"  Slot {slot.slotIndex}: {slot.displayName} (Created: {slot.creationDate})");
                }
                
                // 4. Switch to slot 1 and save some data
                Debug.Log("\n--- Switching to Slot 1 and Saving Data ---");
                await _saveSlotManager.SwitchToSlot(slot1Index, CancellationToken.None);
                _sessionManager.SetActiveSlot(slot1Index);
                
                // Create and save test data
                var testData = new TestSaveData 
                { 
                    DataString = "This is data for slot 1" 
                };
                await _dbService.WriteDataToRuntimeDatabase("TestData", CancellationToken.None, testData);
                Debug.Log("Saved data to slot 1");
                
                // 5. Update slot metadata
                var slot1Metadata = _saveSlotManager.GetSlotMetadata(slot1Index);
                slot1Metadata.ProgressIndicator = "Level 5 - Forest Area";
                slot1Metadata.TotalPlaytimeSeconds = 1800; // 30 minutes
                await _saveSlotManager.UpdateSlotMetadata(slot1Index, slot1Metadata, CancellationToken.None);
                Debug.Log("Updated slot 1 metadata");
                
                // 6. Switch to slot 2 and save different data
                Debug.Log("\n--- Switching to Slot 2 and Saving Data ---");
                await _saveSlotManager.SwitchToSlot(slot2Index, CancellationToken.None);
                _sessionManager.SetActiveSlot(slot2Index);
                
                var testData2 = new TestSaveData 
                { 
                    DataString = "This is data for slot 2 - Hard Mode!" 
                };
                await _dbService.WriteDataToRuntimeDatabase("TestData", CancellationToken.None, testData2);
                Debug.Log("Saved data to slot 2");
                
                // 7. Switch back to slot 1 and load data
                Debug.Log("\n--- Switching Back to Slot 1 and Loading Data ---");
                await _saveSlotManager.SwitchToSlot(slot1Index, CancellationToken.None);
                _sessionManager.SetActiveSlot(slot1Index);
                
                var loadedData = await _dbService.FetchDataFromRuntimeDatabase<TestSaveData>("TestData", CancellationToken.None);
                if (loadedData != null)
                {
                    Debug.Log($"Loaded from slot 1: {loadedData.DataString}");
                }
                
                // 8. Create or get auto-save slot
                Debug.Log("\n--- Working with Auto-Save Slot ---");
                var autoSaveSlot = await _saveSlotManager.GetOrCreateAutoSaveSlot(CancellationToken.None);
                Debug.Log($"Auto-save slot: {autoSaveSlot.displayName} (Index: {autoSaveSlot.slotIndex})");
                
                await _saveSlotManager.SwitchToSlot(autoSaveSlot.slotIndex, CancellationToken.None);
                var autoSaveData = new TestSaveData 
                { 
                    DataString = "Auto-saved progress" 
                };
                await _dbService.WriteDataToRuntimeDatabase("TestData", CancellationToken.None, autoSaveData);
                Debug.Log("Saved data to auto-save slot");
                
                // 9. Check slot existence
                Debug.Log("\n--- Checking Slot Existence ---");
                Debug.Log($"Slot 0 exists: {_saveSlotManager.SlotExists(0)}");
                Debug.Log($"Slot 99 exists: {_saveSlotManager.SlotExists(99)}");
                
                // 10. Get available slot indices
                Debug.Log("\n--- Getting Available Slot Indices ---");
                var availableIndices = _saveSlotManager.GetAvailableSlotIndices(5);
                Debug.Log($"Next available slot indices: {string.Join(", ", availableIndices)}");
                
                // 11. Delete a slot (switch away from it first)
                Debug.Log("\n--- Deleting Slot 2 ---");
                await _saveSlotManager.SwitchToSlot(slot1Index, CancellationToken.None);
                await _saveSlotManager.DeleteSlot(slot2Index, CancellationToken.None);
                Debug.Log($"Deleted slot {slot2Index}");
                
                // 12. List slots again to verify deletion
                Debug.Log("\n--- Final Slot List ---");
                allSlots = _saveSlotManager.GetAllSlotMetadata();
                foreach (var slot in allSlots)
                {
                    Debug.Log($"  Slot {slot.slotIndex}: {slot.displayName}");
                }
                
                Debug.Log("\n===== Example Workflow Complete =====");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Example workflow failed: {ex}");
            }
        }
        
        /// <summary>
        /// Example: Simple save to current slot
        /// </summary>
        public async UniTask SaveGameToCurrentSlot()
        {
            if (_saveSlotManager.CurrentSlotIndex == null)
            {
                Debug.LogWarning("No active slot. Switch to a slot first.");
                return;
            }
            
            // Create your game data (replace TestSaveData with your actual save data)
            var gameData = new TestSaveData 
            { 
                DataString = "Player progress data" 
            };
            
            // Save to database
            bool success = await _dbService.WriteDataToRuntimeDatabase("TestData", CancellationToken.None, gameData);
            
            if (success)
            {
                // Update metadata
                var metadata = _saveSlotManager.GetSlotMetadata(_saveSlotManager.CurrentSlotIndex.Value);
                if (metadata != null)
                {
                    metadata.LastPlayedDate = DateTime.UtcNow;
                    metadata.TotalPlaytimeSeconds += 60; // Add playtime
                    metadata.ProgressIndicator = "Your progress description here";
                    await _saveSlotManager.UpdateSlotMetadata(metadata.slotIndex, metadata, CancellationToken.None);
                }
                
                Debug.Log("Game saved successfully!");
            }
        }
        
        /// <summary>
        /// Example: Load game from a specific slot
        /// </summary>
        public async UniTask LoadGameFromSlot(int slotIndex)
        {
            try
            {
                // Switch to the desired slot
                await _saveSlotManager.SwitchToSlot(slotIndex, CancellationToken.None);
                _sessionManager.SetActiveSlot(slotIndex);
                
                // Load your game data
                var gameData = await _dbService.FetchDataFromRuntimeDatabase<TestSaveData>("TestData", CancellationToken.None);
                
                if (gameData != null)
                {
                    Debug.Log($"Game loaded from slot {slotIndex}!");
                    // Apply the loaded data to your game state
                }
                else
                {
                    Debug.Log($"No save data found in slot {slotIndex}. Starting new game.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load game from slot {slotIndex}: {ex}");
            }
        }
        
        /// <summary>
        /// Example: Auto-save implementation
        /// </summary>
        public async UniTask PerformAutoSave()
        {
            try
            {
                // Get or create auto-save slot
                var autoSaveSlot = await _saveSlotManager.GetOrCreateAutoSaveSlot(CancellationToken.None);
                
                // Save current game state to auto-save slot
                int? previousSlot = _saveSlotManager.CurrentSlotIndex;
                
                await _saveSlotManager.SwitchToSlot(autoSaveSlot.slotIndex, CancellationToken.None);
                
                // Save your game data
                var gameData = new TestSaveData 
                { 
                    DataString = "Auto-saved game state" 
                };
                await _dbService.WriteDataToRuntimeDatabase("TestData", CancellationToken.None, gameData);
                
                // Update metadata
                autoSaveSlot.LastPlayedDate = DateTime.UtcNow;
                await _saveSlotManager.UpdateSlotMetadata(autoSaveSlot.slotIndex, autoSaveSlot, CancellationToken.None);
                
                // Switch back to previous slot if there was one
                if (previousSlot.HasValue && previousSlot.Value != autoSaveSlot.slotIndex)
                {
                    await _saveSlotManager.SwitchToSlot(previousSlot.Value, CancellationToken.None);
                }
                
                Debug.Log("Auto-save complete!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Auto-save failed: {ex}");
            }
        }
    }
}

