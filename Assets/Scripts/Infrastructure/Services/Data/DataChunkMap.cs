using SQLite;

namespace SpiralingStudio.Services.DataManagement
{
    [Table("DataChunkMap")]
    public class DataChunkMap
    {
        [PrimaryKey, Unique, MaxLength(64)] public string Id { get; set; }
        
        public byte[] DataBlob { get; set; }
    }
}