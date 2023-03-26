using System;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class NetworkStateManager : IStartable
{
    private readonly IPublisher<NetworkManagerState> onNetworkStateChange;
    private readonly ISubscriber<NetworkManagerState?> updateNetworkStateSubscriber;
    private readonly IDisposable disposable;

    private NetworkManagerState State;

    [Inject]
    public NetworkStateManager(IPublisher<NetworkManagerState> onNetworkStateChange,
        ISubscriber<NetworkManagerState?> updateNetworkStateSubscriber,
        INetworkDetectionService networkDetectionService, PreLoadedGameSettings preLoadedGameSettings)
    {
        this.onNetworkStateChange = onNetworkStateChange;
        this.updateNetworkStateSubscriber = updateNetworkStateSubscriber;
        networkDetectionService.SetupService(preLoadedGameSettings.NetworkDetectionInterval,
            updateNetworkStateSubscriber);
    }


    #region Lifetime

    public void Start()
    {
        InitializeState();
        updateNetworkStateSubscriber.Subscribe(UpdateState);
    }

    private void InitializeState()
    {
        ChangeState(NetworkManagerState.Initializing);
    }

    private void ChangeState(NetworkManagerState state)
    {
        State = state;
        onNetworkStateChange?.Publish(State);
    }

    private void UpdateState(NetworkManagerState? networkManagerState)
    {
        switch (networkManagerState)
        {
            case null:
                ChangeState(state: Application.internetReachability == NetworkReachability.NotReachable
                    ? NetworkManagerState.NotConnected
                    : NetworkManagerState.Ready);
                break;
            case NetworkManagerState.Initializing:
            case NetworkManagerState.Ready:
            case NetworkManagerState.Busy:
                ChangeState(state: Application.internetReachability == NetworkReachability.NotReachable
                    ? NetworkManagerState.NotConnected
                    : networkManagerState.Value);
                break;
            case NetworkManagerState.NotConnected:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(networkManagerState), networkManagerState, null);
        }
    }

    #endregion
}

public interface INetworkDetectionService
{
    public void SetupService(double detectionInterval,
        ISubscriber<NetworkManagerState?> networkStateChangeEventSubscriber);
}

public class NetworkDetectionService : INetworkDetectionService
{
    public void SetupService(double detectionInterval, ISubscriber<NetworkManagerState?> NetworkStateChangeEvent)
    {
        //TODO : UniRX 
        Debug.Log("Network Detection Service : Setup Complete");
    }
}