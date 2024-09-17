using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using MonsterFactory.Events;
using VContainer;

namespace MonsterFactory.Services.DataManagement
{
    public class MFSerializedReadOnlyDataInstanceProvider<T> : IDisposable where T : MFData, new()
    {
        private readonly ITypeSerializedDBService typeSerializedDBService;
        private readonly IAsyncSubscriber<DataEventLoadData> loadDataSubscriber;
        private readonly IDisposable eventDisposableBag;
        private readonly bool subscribeToAutoLoadEvent;
        private readonly string typeCode;
        private readonly string dbFile;

        private T dataInstance;
        public ref T DataInstance => ref dataInstance;
        

        [Inject]
        public MFSerializedReadOnlyDataInstanceProvider(ITypeSerializedDBService typeSerializedDBService,IAsyncSubscriber<DataEventLoadData> loadDataSubscriber)
        {
            this.typeSerializedDBService = typeSerializedDBService;
            this.loadDataSubscriber = loadDataSubscriber;
          
            MFDataObject dataObject = DataProviderTypeResolver.ResolveTypeInfo<T>(ref typeCode, ref subscribeToAutoLoadEvent);
            if (dataObject is not ReadOnlyDBObject readOnlyDBObject)
            {
                throw new Exception("Invalid Data Type for ReadOnly Data");
            }
            dbFile = readOnlyDBObject.DBObjectName;
            DisposableBagBuilder disposableBagBuilder = DisposableBag.CreateBuilder();
            
            if (subscribeToAutoLoadEvent)
            {
                loadDataSubscriber.Subscribe(LoadReadOnlyData).AddTo(disposableBagBuilder);
            }
            eventDisposableBag = disposableBagBuilder.Build();
        }

        private UniTask LoadReadOnlyData(DataEventLoadData loadDataEvent, CancellationToken cancellationToken)
        {
            LoadDataFromCache();
            return default;
        }

        private async void LoadDataFromCache()
        {
            dataInstance = await typeSerializedDBService.FetchReadOnlyDataFromDB<T>(dbFile, typeCode);
        }
        
        
        public void Dispose()
        {
            eventDisposableBag?.Dispose();
        }
    }
}