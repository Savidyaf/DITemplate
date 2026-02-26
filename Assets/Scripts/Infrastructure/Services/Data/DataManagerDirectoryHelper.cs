using System;
using System.IO;
using UnityEngine;

namespace SpiralingStudio.Services.DataManagement
{
    public static class DataManagerDirectoryHelper
    {
        private static string UserDataPath = "SpiralingStudioUserData/SaveData";
        private static string UserDataObjectName = "UserData";
        private static string SaveSlotFolderPrefix = "Slot_";
        private static string AutoSaveFolderName = "Slot_Auto";
        private static string SaveGameDBName = "SaveGameDB";
        private static string SlotMetadataFileName = "SlotMetadata.dat";
        
        /// <summary>
        /// Gets the database file path for a specific save slot.
        /// </summary>
        /// <param name="slotIndex">Slot index (-1 for auto-save, 0+ for manual slots)</param>
        /// <returns>Full path to the slot's database file</returns>
        public static string GetSlotDatabasePath(int slotIndex)
        {
            var slotFolder = GetSlotFolderPath(slotIndex);
            
            if (!Directory.Exists(slotFolder))
            {
                Directory.CreateDirectory(slotFolder);
            }
            
            return Path.Combine(slotFolder, SaveGameDBName);
        }
        
        /// <summary>
        /// Gets the folder path for a specific save slot.
        /// </summary>
        /// <param name="slotIndex">Slot index (-1 for auto-save, 0+ for manual slots)</param>
        /// <returns>Full path to the slot's folder</returns>
        public static string GetSlotFolderPath(int slotIndex)
        {
            var rootPath = GetSlotsRootPath();
            var folderName = slotIndex == -1 ? AutoSaveFolderName : $"{SaveSlotFolderPrefix}{slotIndex}";
            return Path.Combine(rootPath, folderName);
        }
        
        /// <summary>
        /// Gets the root path where all save slots are stored.
        /// </summary>
        /// <returns>Full path to the slots root directory</returns>
        public static string GetSlotsRootPath()
        {
            return Path.Combine(Application.persistentDataPath, UserDataPath);
        }
        
        /// <summary>
        /// Gets the path to the master slot metadata file.
        /// </summary>
        /// <returns>Full path to the slot metadata file</returns>
        public static string GetSlotMetadataPath()
        {
            return Path.Combine(GetSlotsRootPath(), SlotMetadataFileName);
        }
        
        /// <summary>
        /// Gets the path to the player preferences file (stored independently of save slots).
        /// </summary>
        /// <returns>Full path to the player preferences JSON file</returns>
        public static string GetPlayerPreferencesPath()
        {
            var userDataRoot = Path.Combine(Application.persistentDataPath, UserDataPath);
            return Path.Combine(userDataRoot, "PlayerPreferences.json");
        }
        
        /// <summary>
        /// Checks if a save slot exists.
        /// </summary>
        /// <param name="slotIndex">Slot index to check</param>
        /// <returns>True if the slot folder and database exist</returns>
        public static bool SlotExists(int slotIndex)
        {
            var slotPath = GetSlotDatabasePath(slotIndex);
            return File.Exists(slotPath);
        }
        
        /// <summary>
        /// Deletes a save slot and all its data.
        /// </summary>
        /// <param name="slotIndex">Slot index to delete</param>
        public static void DeleteSlot(int slotIndex)
        {
            var slotFolder = GetSlotFolderPath(slotIndex);
            
            if (Directory.Exists(slotFolder))
            {
                Directory.Delete(slotFolder, true);
            }
        }
        
        /// <summary>
        /// Legacy method for backward compatibility - gets database path by user ID.
        /// </summary>
        [Obsolete("Use GetSlotDatabasePath instead for multi-slot save system")]
        public static string DBFilePathForUserId(string userID)
        {
            var folderPath = $"{Application.persistentDataPath}/{UserDataPath}/{userID}";

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            var path = $"{folderPath}/{UserDataObjectName}";
            return path;
        }
        
        public static string StreamingDataObjectPath(string readOnlyDbName)
        {
            var folderPath = Path.Combine(Application.streamingAssetsPath, readOnlyDbName);

            if (!Directory.Exists(folderPath))
            {
                throw new Exception("Trying to read from non existent db");
            }
            
            return folderPath;
        }
    }
}