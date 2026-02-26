using System;
using System.ComponentModel;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using SpiralingStudio.Events;
using VContainer;

namespace SpiralingStudio.Services.DataManagement
{
    public class MFLocallyStoredDataInstanceProvider<T> : IDisposable where T : MFSaveData, new()
    {
        private readonly ITypeSerializedDBService dbService;
        private readonly IDisposable eventDisposableBag;
        private readonly string typeCode;
        private readonly bool subscribeToAutoLoadEvent, subscribeToAutoSaveEvent;

        private T dataInstance;
        private bool dataInstanceChanged;

        public ref T DataInstance => ref dataInstance;

        #region Constructor

        [Inject]
        public MFLocallyStoredDataInstanceProvider(
            ITypeSerializedDBService typeSerializedDBService,
            IAsyncSubscriber<DataEventLoadData> loadDataSubscriber,
            IAsyncSubscriber<DataEventSaveData> saveDataSubscriber)
        {
            MFDataObject dataObject = DataProviderTypeResolver.ResolveTypeInfo<T>(out var localTypeCode,
                out var autoLoad, out var autoSave);
            typeCode = localTypeCode;
            subscribeToAutoLoadEvent = autoLoad;
            subscribeToAutoSaveEvent = autoSave;
            if (dataObject == null)
            {
                //It's possible to use MFLocallyStoredDataInstanceProvider to create and manage data instances across scope without using DB.
                //For this to work it has to be a MFData object that does not have the MFDataObject attribute.
                //Constructor runs only once per scope so this shouldn't that much load.
                DataInstance ??= new T();
                return;
            }

            dbService = typeSerializedDBService;
            DisposableBagBuilder disposableBagBuilder = DisposableBag.CreateBuilder();

            if (subscribeToAutoLoadEvent)
            {
                loadDataSubscriber.Subscribe(LoadOrInitializeData).AddTo(disposableBagBuilder);
            }

            if (subscribeToAutoSaveEvent)
            {
                saveDataSubscriber.Subscribe(WriteChangesToDB).AddTo(disposableBagBuilder);
            }

            eventDisposableBag = disposableBagBuilder.Build();
        }

        #endregion

        #region API

        /// <summary>
        /// Tries to load data object from DB.If the data instance is null
        /// User force load flag to replace the active instance with db version.
        /// ##WARNING## Terminate any existing logic using dataInstance before calling this.
        /// </summary>
        /// <param name="canOverwrite">Force Load Flag</param>
        /// <param name="cancellationToken"> CancellationToken </param>
        public async UniTask LoadData(bool canOverwrite, CancellationToken cancellationToken)
        {
            if (DataInstance != null && !canOverwrite)
            {
                return;
            }

            dataInstance = await FetchDataOrCreateNewInstance(cancellationToken);
            if (dataInstance != null) dataInstance.PropertyChanged += DataInstanceOnPropertyChanged;
        }

        /// <summary>
        /// Save the data object if the instance has changed.
        /// Use the force save flag to write serialized version of current DataInstance
        /// </summary>
        /// <param name="canForceSave"> Force Save Flag</param>
        /// <param name="cancellationToken"> CancellationToken </param>
        public async UniTask SaveData(bool canForceSave, CancellationToken cancellationToken)
        {
            if (dataInstanceChanged || canForceSave)
            {
                await dbService.WriteDataToRuntimeDatabase(typeCode, cancellationToken, dataInstance);
            }
        }

        #endregion

        #region Implementation

        private async UniTask LoadOrInitializeData(DataEventLoadData eventDataEventLoadData,
            CancellationToken cancellationToken)
        {
            await LoadData(eventDataEventLoadData.CanOverwrite, cancellationToken);
        }

        private async UniTask<T> FetchDataOrCreateNewInstance(CancellationToken cancellationToken)
        {
            var data = await dbService.FetchDataFromRuntimeDatabase<T>(typeCode, cancellationToken)
                .AttachExternalCancellation(cancellationToken);
            if (data == null)
            {
                data = new T();
                await dbService.WriteDataToRuntimeDatabase(typeCode, cancellationToken, data);
            }

            return data;
        }

        private async UniTask WriteChangesToDB(DataEventSaveData saveDataEvent, CancellationToken cancellationToken)
        {
            await SaveData(saveDataEvent.CanForceSave, cancellationToken);
        }

        private void DataInstanceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!dataInstanceChanged)
            {
                dataInstanceChanged = true;
            }
        }

        #endregion

        public void Dispose()
        {
            eventDisposableBag?.Dispose();
        }
    }
}