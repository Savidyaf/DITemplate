using System;
using System.Security.Cryptography;
using System.Text;

namespace SpiralingStudio.Services.DataManagement
{
    public static class DataProviderTypeResolver
    {

        /// <summary>
        /// Resolve the MFDataObject attribute related data
        /// Sets the save and load flags if attribute is found.
        /// Generates a UID
        /// </summary>
        /// <param name="autoLoad"> out AutoLoad : Sets Autoload flag</param>
        /// <param name="autoSave">out AutoSave : Sets Autosave flag</param>
        /// <param name="typeCode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static MFDataObject ResolveTypeInfo<T>(out string typeCode, out bool autoLoad, out bool autoSave)
        {
            autoLoad = default;
            autoSave = default;

            MFDataObject dataObject = MFDataSerializerExtensions.GetDataAttribute<T>(out string name);
            string seed = dataObject?.UniqueId ?? name ?? typeof(T).FullName;
            if (dataObject != null)
            {
                autoLoad = dataObject.AutoFetch;
                autoSave = dataObject.AutoSave;
            }

            typeCode = ComputeTypeCode(seed);
            return dataObject;
        }
        
        public static MFDataObject ResolveTypeInfo<T>(out string typeCode, out bool autoLoad)
        {
            bool autoSave;
            var result = ResolveTypeInfo<T>(out typeCode, out autoLoad, out autoSave);
            return result;
        }

        private static string ComputeTypeCode(string uniqueId)
        {
            using var sha = SHA256.Create();
            byte[] hashedBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(uniqueId));
            // Hex string is 64 chars for SHA-256
            var sb = new StringBuilder(hashedBytes.Length * 2);
            for (int i = 0; i < hashedBytes.Length; i++)
            {
                sb.Append(hashedBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}