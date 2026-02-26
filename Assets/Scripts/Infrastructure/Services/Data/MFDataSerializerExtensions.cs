using MasterMemory;
using MessagePack;
using MessagePack.Resolvers;
using UnityEngine;

namespace SpiralingStudio.Services.DataManagement
{
    public static class MFDataSerializerExtensions
    {
        static bool _serializerRegistered = false;

        public static MFSaveData ExtractDataObjectOfType(this DataChunkMap dataChunk)
        {
            return dataChunk.DataBlob != null
                ? MessagePackSerializer.Deserialize<MFSaveData>(dataChunk.DataBlob)
                : default;
        }
        
        public static T ExtractDataObjectOfType<T>(this byte[] dataChunk) where T : MFSaveData
        {
            return MessagePackSerializer.Deserialize<T>(dataChunk);
        }

        public static byte[] SerializeDataToBytes<T>(this T data) where T : MFSaveData
        {
            return MessagePackSerializer.Serialize<MFSaveData>(data);
        }

        public static MFDataObject GetDataAttribute<T>(out string name)
        {
            var type = typeof(T);
            name = type.Name;
            object[] attributes = type.GetCustomAttributes(typeof(MFDataObject), true);
            foreach (var attribute in attributes)
            {
                if (attribute is MFDataObject dataObjectAttribute)
                {
                    return dataObjectAttribute;
                }
            }

            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (!_serializerRegistered)
            {
                StaticCompositeResolver.Instance.Register(
                   StandardResolver.Instance,
                    StandardResolver.Instance
                    //MasterMemoryResolver.Instance
                    //UnityResolver.Instance
                );

                var option = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);

                MessagePackSerializer.DefaultOptions = option;
                _serializerRegistered = true;
            }
        }

#if UNITY_EDITOR


        [UnityEditor.InitializeOnLoadMethod]
        static void EditorInitialize()
        {
            Initialize();
        }

#endif
    }
}