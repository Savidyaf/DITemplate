using VContainer;

namespace MonsterFactory.Services.DataManagement
{
    public class DataInstanceProvider
    {
        private readonly IObjectResolver objectResolver;
        private readonly IDataManager dataManager;

        [Inject]
        public DataInstanceProvider(IObjectResolver objectResolver)
        {
            this.objectResolver = objectResolver; 
            dataManager = objectResolver.Resolve<IDataManager>();
        }

        public TtypedDataObject GetDataObjectOfType<TtypedDataObject>() where TtypedDataObject : MFData, new()
        {
            if (dataManager.GetIfDataExists(typeof(TtypedDataObject)) is TtypedDataObject data)
            {
                return data;
            }

            return CreateNewDataInstance<TtypedDataObject>();
        }

        private TtypedDataObject CreateNewDataInstance<TtypedDataObject>() where TtypedDataObject : new()
        {
            TtypedDataObject obj = new TtypedDataObject();
            objectResolver.Inject(obj);
            if (obj is MFData mfData)
            {
                dataManager.CacheData(typeof(TtypedDataObject), mfData, mfData.IsLocallyStored());
            }
            return obj;
        }
    }
}