using MasterMemory;
using MessagePack;

namespace SpiralingStudio.Services.DataManagement
{
    /// <summary>
    /// Represents a single localization entry in the MasterMemory database.
    /// Each entry contains a key, language code, translation text, and content file identifier.
    /// </summary>
    [MemoryTable("LocalizationEntry"), MessagePackObject(true)]
    public class LocalizationEntry
    {
        /// <summary>
        /// Compound primary key combining Key and LanguageCode.
        /// Format: "key|languageCode" (e.g., "common_button_text_confirm|en")
        /// </summary>
        [PrimaryKey]
        public string Id { get; set; }

        /// <summary>
        /// Localization key with content file prefix.
        /// Examples: "common_button_text_confirm", "narration_tutorial_text_1"
        /// </summary>
        [SecondaryKey(0)]
        public string Key { get; set; }

        /// <summary>
        /// ISO 639-1 language code (e.g., "en", "es", "fr", "de").
        /// </summary>
        [SecondaryKey(1)]
        public string LanguageCode { get; set; }

        /// <summary>
        /// The translated text for this key in this language.
        /// Supports rich text, parameters, emojis, and special characters.
        /// </summary>
        public string Translation { get; set; }

        /// <summary>
        /// Content file identifier (e.g., "Common", "Narration", "UI").
        /// Extracted from the key prefix before the first underscore.
        /// </summary>
        public string ContentFile { get; set; }

        /// <summary>
        /// Parameterless constructor for MessagePack serialization.
        /// </summary>
        public LocalizationEntry()
        {
        }

        /// <summary>
        /// Constructor for creating a localization entry.
        /// </summary>
        public LocalizationEntry(string key, string languageCode, string translation, string contentFile = null)
        {
            Key = key;
            LanguageCode = languageCode;
            Translation = translation;
            ContentFile = contentFile ?? ExtractContentFileFromKey(key);
            Id = GenerateId(key, languageCode);
        }

        /// <summary>
        /// Generates a compound ID from key and language code.
        /// </summary>
        public static string GenerateId(string key, string languageCode)
        {
            return $"{key}|{languageCode}";
        }

        /// <summary>
        /// Extracts the content file identifier from a key.
        /// Example: "common_button_text_confirm" → "common"
        /// </summary>
        public static string ExtractContentFileFromKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            int underscoreIndex = key.IndexOf('_');
            if (underscoreIndex > 0)
            {
                return key.Substring(0, underscoreIndex);
            }

            return string.Empty;
        }
    }
}




