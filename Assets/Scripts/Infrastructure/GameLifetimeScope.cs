using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private GameSettings GameSettingsScriptableObject;

    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private GameStateManager gameStateManager;

    protected override void Configure(IContainerBuilder builder)
    {
        RegisterMessagePipe(builder);
        RegisterGameManagers(builder);
    }

    #region RegisterClasses

    private void RegisterMessagePipe(IContainerBuilder builder)
    {
        var options = builder.RegisterMessagePipe();

        // Setup GlobalMessagePipe to enable diagnostics window and global function
        builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

        // RegisterMessageBroker: Register for IPublisher<T>/ISubscriber<T>, includes async and buffered.
        builder.RegisterMessageBroker<int>(options);
        builder.RegisterMessageBroker<NetworkManagerState?>(options);
        builder.RegisterMessageBroker<NetworkManagerState>(options);
        builder.RegisterMessageBroker<SceneLoaderState>(options);
        //RegisterMessageBroker<TKey, TMessage>, RegisterRequestHandler, RegisterAsyncRequestHandler

        // RegisterMessageHandlerFilter: Register for filter, also exists RegisterAsyncMessageHandlerFilter, Register(Async)RequestHandlerFilter
        builder.RegisterMessageHandlerFilter<MyFilter<int>>();
    }

    private void RegisterGameManagers(IContainerBuilder builder)
    {
        builder.RegisterInstance(GameSettingsScriptableObject.DefaultGameSettings);
        builder.Register<INetworkDetectionService, NetworkDetectionService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<NetworkStateManager>();
        builder.RegisterInstance(sceneLoader);
        builder.RegisterInstance(gameStateManager);
    }

    #endregion
}