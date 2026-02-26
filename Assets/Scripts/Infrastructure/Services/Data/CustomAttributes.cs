namespace SpiralingStudio.Services.DataManagement
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class MFDataObject : System.Attribute
    {
        private readonly string uniqueId;
        private readonly bool autoFetch;
        private readonly bool autoSave;

        public MFDataObject(string uniqueId, bool autoFetch = false, bool autoSave = false)
        {
            this.uniqueId = uniqueId;
            this.autoFetch = autoFetch;
            this.autoSave = autoSave;
        }

        public bool AutoFetch => autoFetch;

        public string UniqueId => uniqueId;

        public bool AutoSave => autoSave;
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public abstract class ReadOnlyDBObject : MFDataObject
    {
        protected ReadOnlyDBObject(string uniqueId, string dBObjectName, bool autoFetch) : base(uniqueId,
            autoFetch, false)
        { 
            DBObjectName = dBObjectName;
        }
        public string DBObjectName { get; }
    }
}