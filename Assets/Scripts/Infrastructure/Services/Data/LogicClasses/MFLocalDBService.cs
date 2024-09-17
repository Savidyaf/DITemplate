using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;


namespace MonsterFactory.Services.DataManagement
{
    public interface ITypeSerializedDBService
    {
        /// <summary>
        /// Fetches data of type T from the runtime database.
        /// </summary>
        /// <param name="typeCode">The code identifying the type of data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <typeparam name="T">The type of data to fetch.</typeparam>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        public UniTask<T> FetchDataFromRuntimeDatabase<T>(string typeCode, CancellationToken cancellationToken) where T : MFData;

        /// <summary>
        /// Writes data of type T to the runtime database.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="typeCode">The code identifying the type of data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <param name="dataInstance">The instance of data to write.</param>
        /// <returns>True : If write operation succeeded, False : Write operation failed </returns>
        public UniTask<bool> WriteDataToRuntimeDatabase<T>(string typeCode, CancellationToken cancellationToken, T dataInstance)
            where T : MFData;

        public UniTask<T> FetchReadOnlyDataFromDB<T>(string dbName, string dataId, bool loadToMemoryIfNotQueued = true) where T : MFData;
    }

    public class MFLocalDBService : IMFService, ITypeSerializedDBService
    {
        private IMFSerializedDBConnection readWriteDBConnection;
        private MFReadOnlyDbDataCache readOnlyDbDataCache;
        private const string AutoLoadDbname =  "AutoLoadDb";
        
        #region Init

        [Inject]
        public MFLocalDBService()
        {
        }
        
        public UniTask[] GetInitializeTasks()
        {
            return new[]
            {
                //InitializeReadOnlyDataSystems(),
                InitializeRuntimeDataSystems()
            };
        }

        private UniTask InitializeRuntimeDataSystems()
        {
            readWriteDBConnection = new MFSqlDBConnection(DataManagerDirectoryHelper.DBFilePathForUserId("TestUser"));
            return readWriteDBConnection.Initialize();
        }
        
        private async UniTask InitializeReadOnlyDataSystems()
        {
            readOnlyDbDataCache = new MFReadOnlyDbDataCache();
            await readOnlyDbDataCache.TryQueue(AutoLoadDbname);
        }

        #endregion

        #region API
        
        public UniTask<T> FetchDataFromRuntimeDatabase<T>(string typeCode, CancellationToken cancellationToken) where T : MFData
        {
            return FetchDataFromDb<T>(typeCode, cancellationToken, readWriteDBConnection);
        }

        private async UniTask<T> FetchDataFromDb<T>(string typeCode, CancellationToken cancellationToken, IMFSerializedDBConnection dbConnection)
            where T : MFData
        {
            try
            {
                DataChunkMap dataChunkMap = await dbConnection.GetChunkUniqueDataFromKey(typeCode)
                    .AttachExternalCancellation(cancellationToken);
                if (dataChunkMap != null)
                {
                    return await TryProcessDataChunk<T>(typeCode).AttachExternalCancellation(cancellationToken);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DB Fetch {typeCode} Unknown Error : {e}");
                return null;
            }

            return null;
        }

        public async UniTask<bool> WriteDataToRuntimeDatabase<T>(string typeCode, CancellationToken cancellationToken,
            T dataInstance) where T : MFData
        {
            try
            {
                return await readWriteDBConnection.WriteSingleDataChunkToId(typeCode, dataInstance?.SerializeDataToBytes())
                    .AttachExternalCancellation(cancellationToken) > 0;
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning($"DB Write {typeCode} Operation Cancelled : {e}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"DB Write {typeCode} Unknown Error : {e}");
                return false;
            }
        }

        public async UniTask<T> FetchReadOnlyDataFromDB<T>(string dbName, string dataId, bool loadToMemoryIfNotQueued = true) where T : MFData
        {
            if (!await readOnlyDbDataCache.TryQueue(dbName))
            {
                return null;
            }
            readOnlyDbDataCache.TryGetValue(dbName, out MFReadOnlyBinaryDataQueue value);
            if (value != null && value.TryDeque(dataId, out byte[] bytes))
            {
                return bytes.ExtractDataObjectOfType<T>();
            }
            return null;
        }

        #endregion


        #region Implementation

        private async UniTask<T> TryProcessDataChunk<T>(string typeCode) where T : MFData
        {
            DataChunkMap dataChunk = await readWriteDBConnection.GetDataChunkById(typeCode);
            MFData var = dataChunk.ExtractDataObjectOfType();
            if (var is T data)
            {
                return data;
            }

            return null;
        }

        #endregion
    }
}