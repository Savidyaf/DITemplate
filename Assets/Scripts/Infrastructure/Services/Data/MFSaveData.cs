using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MessagePack;


namespace SpiralingStudio.Services.DataManagement
{
    [Union(0, typeof(TestSaveData))]
    [Union(1, typeof(SaveSlotMetadata))]
    public abstract class MFSaveData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    [MessagePackObject][MFDataObject("TestData", true,true)]
    public class TestSaveData : MFSaveData
    {
        [Key(1)]
        public string dataString;
        
        [IgnoreMember]
        public string DataString
        {
            get => dataString;
            set =>  SetField(ref dataString , value);
        }
    }
    
}
