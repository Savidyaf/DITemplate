using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SpiralingStudio.Services.DataManagement
{
    [CreateAssetMenu(menuName = "MasterMemory/CSVTableReferences", fileName = "CSVTableReferences")]
    public class CSVTableReferences : ScriptableObject
    {
        // This is just an example for how you might store the data
        // Replace 'string' with your actual key type if needed, e.g. an enum
        [System.Serializable]
        public class StringAssetReferenceDictionary : SerializedDictionary<string, AssetReference> {}

        [SerializeField]
        private StringAssetReferenceDictionary tableNameToCsvReference
            = new StringAssetReferenceDictionary();

        public IReadOnlyDictionary<string, AssetReference> TableNameToCsvReference
            => tableNameToCsvReference;
    }
}