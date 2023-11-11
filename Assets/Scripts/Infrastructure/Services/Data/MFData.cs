using System;
using MessagePipe;
using VContainer;

namespace MonsterFactory.Services.DataManagement
{
    //TODO : #replace with new serializer
    [Serializable]
    public abstract class MFData
    {
        private DateTime lastUpdatedDateTime;
        private string userId;

        [Inject] private IDataManager dataManager;
        /// <summary>
        /// Last time the data object was saved
        /// </summary>
        public DateTime LastUpdatedDateTime
        {
            get => lastUpdatedDateTime;
            set => UpdateDataIfNeeded(ref lastUpdatedDateTime,value);
        }

        //Will be needed when we do multiplayer
        public string UserId
        {
            get => userId;
            set => userId = value;
        }


        protected void UpdateDataIfNeeded<T>(ref T oldValue, T newValue)
        {
            if (oldValue == null || !oldValue.Equals(newValue))
            {
                oldValue = newValue;
                if (IsLocallyStored())
                {
                    StoreData();
                }
            }
        }

        public abstract Type GetDataType();

        public abstract bool IsLocallyStored();

        protected void StoreData()
        {
            dataManager.UpdateDataObject(GetDataType());
        }
    }
}