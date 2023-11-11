using System;

namespace MonsterFactory.Services.DataManagement
{
    class LocallyStoredDataObject : Attribute
    {
        public string UniqueDataID { get; private set; }

        public LocallyStoredDataObject(string uniqueDataID)
        {
            UniqueDataID = uniqueDataID;
        }
    }
}