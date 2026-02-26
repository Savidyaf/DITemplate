using System;
using System.Collections.Generic;
using Infrastructure.Core;
using MessagePipe;
using SpiralingStudio.Events;
using SpiralingStudio.Services;
using SpiralingStudio.Services.DataManagement;
using SpiralingStudio.Services.Localization;
using SpiralingStudio.Services.Session;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Infrastructure.Systems
{
    /// <summary>
    /// Root lifetime scope for the game application.
    /// Configures and registers all global services, message brokers, and systems.
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        public CSVTableReferences TableReferences;
        public SaveSlotConfiguration SaveSlotConfiguration;
        public LocalizationConfiguration LocalizationConfiguration;


        protected override void Configure(IContainerBuilder builder)
        {
            MFDataSerializerExtensions.Initialize();
            
            // Register ScriptableObject configurations
            if (SaveSlotConfiguration != null)
            {
                builder.RegisterInstance(SaveSlotConfiguration);
            }
            else
            {
                Debug.LogWarning("[GameLifetimeScope] SaveSlotConfiguration not assigned. Creating default configuration.");
                var defaultConfig = ScriptableObject.CreateInstance<SaveSlotConfiguration>();
                builder.RegisterInstance(defaultConfig);
            }
            
            if (LocalizationConfiguration != null)
            {
                builder.RegisterInstance(LocalizationConfiguration);
            }
            else
            {
                Debug.LogWarning("[GameLifetimeScope] LocalizationConfiguration not assigned. Loading from Resources or creating default.");
                var locConfig = Resources.Load<LocalizationConfiguration>("LocalizationConfiguration");
                if (locConfig != null)
                {
                    builder.RegisterInstance(locConfig);
                }
                else
                {
                    Debug.LogWarning("[GameLifetimeScope] LocalizationConfiguration not found in Resources. Creating default configuration.");
                    var defaultLocConfig = ScriptableObject.CreateInstance<LocalizationConfiguration>();
                    builder.RegisterInstance(defaultLocConfig);
                }
            }
            
            if (TableReferences == null)
            {
                Debug.LogWarning("[GameLifetimeScope] TableReferences not assigned. Creating empty configuration.");
                TableReferences = ScriptableObject.CreateInstance<CSVTableReferences>();
            }
            
            SetupGlobalMessageBrokers(builder);
            SetupServices(builder);
            SetupDataProviders(builder);
            builder.Register(resolver =>
            {
                var factory = resolver.Resolve<IDbLoaderFactory>();
                var reference = TableReferences;
                return factory.Create(reference);
            }, Lifetime.Singleton).As<IDbLoader>();
            
            // Register GameInitializer
            builder.Register<Infrastructure.Core.GameInitializer>(Lifetime.Singleton);
            
            builder.RegisterEntryPoint<Infrastructure.Core.GameManager>(Lifetime.Singleton);
        }

        private void SetupDataProviders(IContainerBuilder builder)
        {
            RuntimeDataProviderRegistrationHelper.RegisterDataProviders(builder);
        }

        private void SetupServices(IContainerBuilder builder)
        {
            ServiceRegistrationHelper.RegisterServices(builder);
            builder.RegisterEntryPoint<ServiceInitializer>();
        }

        private void SetupGlobalMessageBrokers(IContainerBuilder builder)
        {
            MessagePipeOptions options = builder.RegisterMessagePipe();
            options.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
            options.EnableCaptureStackTrace = false;
            options.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore;
            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));
            builder.RegisterMessageBroker<MFInternalServicesEvent>(options);
            EventRegistrationHelper.RegisterGlobalEventClasses(builder,options);
        }
    
    }
}