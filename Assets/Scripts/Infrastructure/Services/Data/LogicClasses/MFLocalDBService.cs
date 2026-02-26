using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;


namespace SpiralingStudio.Services.DataManagement
{
   public class MFLocalDBService : IMFService, ITypeSerializedDBService, IAsyncDisposable
    {
        private const string DBPathExtension = "SaveGameDB";
        
        private IMFSerializedDBConnection runtimeDbConnection;
        private bool initialized;
        private string runtimeDbPath;
        private int? currentSlotIndex;
        private readonly object _dbLock = new object();

        /// <summary>
        /// Gets the currently active slot index, or null if no slot is active.
        /// </summary>
        public int? CurrentSlotIndex
        {
            get
            {
                lock (_dbLock)
                {
                    return currentSlotIndex;
                }
            }
        }

        [Inject]
        public MFLocalDBService()
        {
            // Constructor
        }

        // This method is from IMFService.
        // We'll provide an array of tasks for any needed initialization.
        public UniTask[] GetInitializeTasks()
        {
            // For multi-slot save system, we don't auto-initialize a database
            // Database will be initialized when a slot is selected via SwitchToSlotDatabase
            return new[]
            {
                InitializeServiceOnly()
            };
        }

        private UniTask InitializeServiceOnly()
        {
            // Mark service as initialized but don't connect to any database yet
            initialized = true;
            Debug.Log("[MFLocalDBService] Service initialized. No database connected yet. Call SwitchToSlotDatabase to load a save slot.");
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Switches the active database connection to a specific save slot.
        /// Closes the current connection if one exists.
        /// </summary>
        /// <param name="slotIndex">Slot index to switch to (-1 for auto-save, 0+ for manual slots)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async UniTask SwitchToSlotDatabase(int slotIndex, CancellationToken cancellationToken = default)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("[MFLocalDBService] Service not initialized. Call GetInitializeTasks and await them first.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Close current connection if exists
                await CloseCurrentDatabase();

                // Get path for the new slot
                runtimeDbPath = DataManagerDirectoryHelper.GetSlotDatabasePath(slotIndex);
                Debug.Log($"[MFLocalDBService] Switching to slot {slotIndex} database at: {runtimeDbPath}");

                // Initialize new connection
                lock (_dbLock)
                {
                    runtimeDbConnection = new MFSqlDBConnection(runtimeDbPath);
                    currentSlotIndex = slotIndex;
                }

                await runtimeDbConnection.Initialize();

                Debug.Log($"[MFLocalDBService] Successfully switched to slot {slotIndex}.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[MFLocalDBService] Slot switch to {slotIndex} was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MFLocalDBService] Failed to switch to slot {slotIndex}: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Closes the current database connection without switching to a new one.
        /// </summary>
        public async UniTask CloseCurrentDatabase()
        {
            IMFSerializedDBConnection connectionToClose = null;

            lock (_dbLock)
            {
                if (runtimeDbConnection != null)
                {
                    connectionToClose = runtimeDbConnection;
                    runtimeDbConnection = null;
                    currentSlotIndex = null;
                }
            }

            if (connectionToClose != null)
            {
                try
                {
                    Debug.Log("[MFLocalDBService] Closing current database connection...");
                    await connectionToClose.CloseDbConnection();
                    Debug.Log("[MFLocalDBService] Database connection closed.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MFLocalDBService] Error closing database connection: {ex}");
                    throw;
                }
            }
        }

        // Implementation of ITypeSerializedDBService:
        public async UniTask<T> FetchDataFromRuntimeDatabase<T>(
            string typeCode,
            CancellationToken cancellationToken)
            where T : MFSaveData
        {
            if (!initialized)
            {
                throw new InvalidOperationException("[MFLocalDBService] Service not initialized. Call GetInitializeTasks and await them first.");
            }

            lock (_dbLock)
            {
                if (runtimeDbConnection == null)
                {
                    throw new InvalidOperationException("[MFLocalDBService] No database connection active. Call SwitchToSlotDatabase to load a save slot first.");
                }
            }

            if (string.IsNullOrEmpty(typeCode))
            {
                throw new ArgumentException("Type code cannot be null or empty.", nameof(typeCode));
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                DataChunkMap chunk = await runtimeDbConnection.GetDataChunkById(typeCode);
                if (chunk == null || chunk.DataBlob == null || chunk.DataBlob.Length == 0)
                {
                    Debug.Log($"[MFLocalDBService] No data found for typeCode '{typeCode}'.");
                    return null; 
                }

                MFSaveData deserialized = chunk.ExtractDataObjectOfType();
                return (T)deserialized;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[MFLocalDBService] Fetch operation cancelled for typeCode '{typeCode}'.");
                throw;
            }
            catch (InvalidCastException ex)
            {
                Debug.LogError($"[MFLocalDBService] Type mismatch when fetching data for typeCode '{typeCode}': {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MFLocalDBService] Failed to fetch data for typeCode '{typeCode}': {ex}");
                return null;
            }
        }

        public async UniTask<bool> WriteDataToRuntimeDatabase<T>(
            string typeCode,
            CancellationToken cancellationToken,
            T dataInstance) where T : MFSaveData
        {
            if (!initialized)
            {
                throw new InvalidOperationException("[MFLocalDBService] Service not initialized. Call GetInitializeTasks and await them first.");
            }

            lock (_dbLock)
            {
                if (runtimeDbConnection == null)
                {
                    throw new InvalidOperationException("[MFLocalDBService] No database connection active. Call SwitchToSlotDatabase to load a save slot first.");
                }
            }

            if (string.IsNullOrEmpty(typeCode))
            {
                throw new ArgumentException("Type code cannot be null or empty.", nameof(typeCode));
            }

            if (dataInstance == null)
            {
                Debug.LogWarning($"[MFLocalDBService] WriteDataToRuntimeDatabase called with null dataInstance for '{typeCode}'");
                return false;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                byte[] bytes = dataInstance.SerializeDataToBytes();
                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogWarning($"[MFLocalDBService] Serialization produced empty data for typeCode '{typeCode}'.");
                    return false;
                }
                
                await runtimeDbConnection.WriteSingleDataChunkToId(typeCode, bytes);
                Debug.Log($"[MFLocalDBService] Successfully wrote {bytes.Length} bytes for typeCode '{typeCode}'.");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[MFLocalDBService] Write operation cancelled for typeCode '{typeCode}'.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MFLocalDBService] Failed to write data for typeCode '{typeCode}': {ex}");
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await CloseCurrentDatabase();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MFLocalDBService] Error during disposal: {ex}");
            }
            finally
            {
                initialized = false;
            }
        }
    }

}