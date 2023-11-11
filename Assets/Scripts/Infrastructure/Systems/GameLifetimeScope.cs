using VContainer;
using VContainer.Unity;
using MessagePipe;
using MonsterFactory.Services;
using MonsterFactory.Services.DataManagement;
using MonsterFactory.Services.SceneManagement;
using MonsterFactory.Events;
using MonsterFactory.Services.Session;
using UnityEngine;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField]private SceneTransitionController _sceneTransitionController;
 
    protected override void Configure(IContainerBuilder builder)
    {
        SetupGlobalMessageBrokers(builder);
        SetupSession(builder);
        SetupServices(builder);
    }

    private void SetupServices(IContainerBuilder builder)
    {
        builder.Register<DataManager>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        //builder.RegisterEntryPoint<MFService>(Lifetime.Transient);
        builder.Register<DataInstanceProvider>(Lifetime.Singleton);
        builder.Register<DataConnector>(Lifetime.Singleton).As<IDataConnector>();
        builder.RegisterInstance(_sceneTransitionController).AsImplementedInterfaces();
        builder.Register<SceneNavigationManager>(Lifetime.Singleton).AsImplementedInterfaces();

        builder.RegisterEntryPoint<GameInitializer>();
    }

    private void SetupSession(IContainerBuilder builder)
    {
        SessionManager.CreateSession();
    }

    private void SetupGlobalMessageBrokers(IContainerBuilder builder)
    {
        MessagePipeOptions options = builder.RegisterMessagePipe();
        //Setup GlobalMessagePipe to enable diagnostics window and global function 
        builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));
        
        builder.RegisterMessageBroker<MFInternalServicesEvent>(options);
        EventRegistrationHelper.RegisterGlobalEventClasses(builder,options);
    }
    
}