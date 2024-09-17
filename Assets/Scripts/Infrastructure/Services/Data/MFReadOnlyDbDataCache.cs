using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MonsterFactory.Services.DataManagement
{
    public class MFReadOnlyDbDataCache : Dictionary<string, MFReadOnlyBinaryDataQueue>
    {
        public async UniTask<bool> TryQueue(string dbFileName)
        {
            try
            {
                if (ContainsKey(dbFileName))
                {
                    return true;
                }
                var conn = new MFSqlDBConnection(DataManagerDirectoryHelper.StreamingDataObjectPath(dbFileName));
                await conn.Initialize();
                var list = await conn.GetAllDataFromTable();
                TryQueueData(dbFileName, list);
                await conn.CloseDbConnection();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        private void TryQueueData(string dbFileName, List<DataChunkMap> rawData)
        {
            MFReadOnlyBinaryDataQueue dataQueue = new MFReadOnlyBinaryDataQueue();
            
            foreach (DataChunkMap variable in rawData)
            {
                dataQueue.Add(variable.Id, variable.DataBlob);
            }
            TryAdd(dbFileName, dataQueue);
        }
        
    }
}