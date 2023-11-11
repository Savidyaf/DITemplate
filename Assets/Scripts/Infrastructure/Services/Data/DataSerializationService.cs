using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public static class DataSerializationService 
{
    public static string SerializeDataToJson(object ObjectToSerialize)
    {
        return JsonConvert.SerializeObject(ObjectToSerialize, Formatting.None);
    }

    public static object ReadSerializedData(string jsonString)
    {
        return JsonConvert.DeserializeObject(jsonString);
    }
}
