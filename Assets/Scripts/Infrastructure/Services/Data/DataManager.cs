using System;
using System.Collections.Generic;
using System.IO;
using MonsterFactory.Services.Session;
using UnityEngine;
using VContainer;


namespace MonsterFactory.Services.DataManagement
{
    public interface IDataManager
    {
        public void UpdateDataObject(Type dataType);
        public MFData GetIfDataExists(Type type);

        public void CacheData(Type type, MFData data, bool isLocallyStored);
    }

    public class DataManager : MFService, IDataManager
    {
        private static SessionData sessionData;


        //Runtime Variables
        private Dictionary<Type, MFData> locallyStoredDataCache;
        private Dictionary<Type, MFData> runtimeStoredDataCache = new();

        [Inject]
        public DataManager()
        {
            sessionData = SessionManager.sessionData;
        }

        #region LifeCycle

        #endregion

        #region API

        public void ReadAndCacheDataForUser()
        {
            if (sessionData == null) return;
            locallyStoredDataCache = ReadPersistantData() ?? new Dictionary<Type, MFData>();
            Debug.Log("Data Fetched");
        }

        public MFData GetIfDataExists(Type type)
        {
            return runtimeStoredDataCache.TryGetValue(type, out var existingData) ? existingData :
                locallyStoredDataCache.TryGetValue(type, out var dataExists) ? dataExists : null;
        }

        public void CacheData(Type type, MFData data, bool isLocallyStored)
        {
            if (isLocallyStored)
            {
                locallyStoredDataCache?.Add(type, data);
            }
            else
            {
                runtimeStoredDataCache?.Add(type, data);
            }
        }

        #endregion

        #region Implementation

        private Dictionary<Type, MFData> ReadPersistantData()
        {
            string path = DataManagerDirectoryHelper.DataObjectPathForUserId(sessionData.userID);
            if (File.Exists(path))
            {
                MFLocalStorageObject localStorageObject = GetLocalStorageObject(path);
                if (localStorageObject != null)
                {
                    return localStorageObject.LocalDataDictionaryByType;
                }
            }

            return null;
        }

        //Returns MFLocalstorage object if present 
        private MFLocalStorageObject GetLocalStorageObject(string path)
        {
            object localStorageObject = ReadFromPath(path);

            if (localStorageObject is MFLocalStorageObject mfLocalStorageObject)
            {
                return mfLocalStorageObject;
            }

            return null;
        }

        private string SerializeLocalStorage()
        {
            //Handle queue here later 
            MFLocalStorageObject obj = new MFLocalStorageObject(locallyStoredDataCache);
            return DataSerializationService.SerializeDataToJson(obj);
        }

        //Read Data Object from given path as plain text
        private object ReadFromPath(string path)
        {
            try
            {
                string jsonString = File.ReadAllText(path);
                if (string.IsNullOrEmpty(jsonString)) return null;

                return DataSerializationService.ReadSerializedData(jsonString);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        private static void WriteToLocalStorage(string jsonString, string path)
        {
            try
            {
                FileInfo file = new FileInfo(path);
                if (file.Directory != null) file.Directory.Create();
                if (string.IsNullOrEmpty(jsonString)) return;
                File.WriteAllText(path, jsonString);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed writing data" + e);
                return;
            }
        }

        public void UpdateDataObject(Type dataType)
        {
            //TODO : Add support to map individual lines to local storage object
            WriteToLocalStorage(SerializeLocalStorage(),
                DataManagerDirectoryHelper.DataObjectPathForUserId(sessionData.userID));
        }

        #endregion
    }
}