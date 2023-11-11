using UnityEngine;

namespace MonsterFactory.Services.DataManagement
{
    public static class DataManagerDirectoryHelper
    {
        private static string UserDataPath = "MonsterFactoryUserData/SaveData";
        private static string UserDataObjectName = "UserData";
        public static string DataObjectPathForUserId(string userID)
        {
            return $"{Application.persistentDataPath}/{UserDataPath}/{userID}/{UserDataObjectName}";
        }
    }
}