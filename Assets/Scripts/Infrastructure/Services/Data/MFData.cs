using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MessagePack;


namespace MonsterFactory.Services.DataManagement
{
    
    [Union(0, typeof(TestData))]
    public abstract class MFData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }
            field = value;
            PropertyChanged?.Invoke(null, null);
        }
    }

    [MessagePackObject][MFDataObject("TestData", true,true)]
    public class TestData : MFData
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

    [AutoLoadDbObjects(uniqueId:"TestReadOnlyData")]
    public class TestReadOnlyData : MFData
    {
    }
}