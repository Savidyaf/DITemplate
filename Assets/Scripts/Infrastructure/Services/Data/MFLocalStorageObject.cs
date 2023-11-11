using System;
using System.Collections.Generic;

namespace MonsterFactory.Services.DataManagement
{
    [Serializable]
    public class MFLocalStorageObject
    {
        private readonly Dictionary<Type, MFData> localDataDictionaryByType;


        public MFLocalStorageObject(Dictionary<Type, MFData> localDataDictionaryByType)
        {
            this.localDataDictionaryByType = localDataDictionaryByType;
        }

        /// <summary>
        /// Local Data is stored as a dictionary of MFData objects along with their type
        /// </summary>
        public Dictionary<Type, MFData> LocalDataDictionaryByType => localDataDictionaryByType;
    }
}