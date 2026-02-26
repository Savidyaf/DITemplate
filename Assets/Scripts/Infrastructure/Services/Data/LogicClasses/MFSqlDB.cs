using System.Collections.Generic;
using SQLite;
using Cysharp.Threading.Tasks;


namespace SpiralingStudio.Services.DataManagement
{
    public interface IMFSerializedDBConnection
    {
        public UniTask Initialize();
        public UniTask<DataChunkMap> GetDataChunkById(string dataChunkId);

        public UniTask<int> WriteSingleDataChunkToId(string dataChunkId, byte[] dataBlob);
        
        public UniTask<DataChunkMap> GetChunkUniqueDataFromKey(string key);

        public UniTask<List<DataChunkMap>> GetAllDataFromTable();

        public UniTask<int> AddNewDataInstance(DataChunkMap data);

        public UniTask CloseDbConnection();
    }

    public class MFSqlDBConnection : IMFSerializedDBConnection
    {
        private readonly string dbPath;
        private SQLiteAsyncConnection dbConnection;

        public MFSqlDBConnection(string dbFilePath)
        {
            dbPath = dbFilePath;
        }

        public UniTask Initialize()
        {
            if (dbConnection != null)
            {
                return default;
            }
            dbConnection = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
            return CreateDataChunkTable();
        }

        public async UniTask<DataChunkMap> GetDataChunkById(string dataChunkId)
        {
            try
            {
                return await dbConnection.GetAsync<DataChunkMap>(dataChunkId);
            }
            catch
            {
                // not found
                return null;
            }
        }

        public UniTask<int> WriteSingleDataChunkToId(string typeCode, byte[] dataBlob)
        {
            return dbConnection.InsertOrReplaceAsync(new DataChunkMap() { DataBlob = dataBlob, Id = typeCode }, typeof(DataChunkMap)).AsUniTask();
        }

        public async UniTask<DataChunkMap> GetChunkUniqueDataFromKey(string key)
        {
            try
            {
                return await dbConnection.GetAsync<DataChunkMap>(key);
            }
            catch
            {
                // not found
                return null;
            }
        }

        public async UniTask<List<DataChunkMap>> GetAllDataFromTable()
        {
            return await dbConnection.Table<DataChunkMap>().ToListAsync();
        }

        public UniTask<int> AddNewDataInstance(DataChunkMap data)
        {
            return dbConnection.InsertAsync(data, typeof(DataChunkMap)).AsUniTask();
        }

        public UniTask CloseDbConnection()
        {
            return dbConnection != null ? dbConnection.CloseAsync().AsUniTask() : default;
        }
        
        private UniTask CreateDataChunkTable()
        {
            return dbConnection.CreateTablesAsync(CreateFlags.None, typeof(DataChunkMap)).AsUniTask();
        }
    }
}