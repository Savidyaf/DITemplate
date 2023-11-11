using MessagePipe;
using MonsterFactory.Services.DataManagement;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MonsterFactory.Services
{
    public class GameInitializer : MFService, IInitializable
    {
        [Inject] private DataManager dataManager;
        [Inject] private IDataConnector connector;


        public void Initialize()
        {
            dataManager.ReadAndCacheDataForUser();
            connector.SetUserName("pkya");
            Debug.Log("" + connector.GetUserName());
        }
    }
}