using Infrastructure.Systems;
using Infrastructure.UIManager;
using SpiralingStudio.Services.DataManagement;
using SpiralingStudio.Services.Localization;
using SpiralingStudio.Services.PlayerPreferences;
using VContainer;

namespace SpiralingStudio.Services
{
    public static class ServiceRegistrationHelper
    {
        private static readonly Lifetime ServicesLifetimeType = Lifetime.Singleton;

        public static void RegisterServices(IContainerBuilder containerBuilder)
        {
            RegisterService<SpiralingStudio.Services.Session.SessionManager>(containerBuilder);

            RegisterService<MFSceneManager>(containerBuilder);

            RegisterService<UiManager>(containerBuilder);

            RegisterService<MFLocalDBService>(containerBuilder)
                .As<ITypeSerializedDBService>();

            RegisterService<SaveSlotManager>(containerBuilder);

            RegisterService<MFLocalizationService>(containerBuilder);

            RegisterService<PlayerPreferencesService>(containerBuilder);
        }

        private static RegistrationBuilder RegisterService<T>(IContainerBuilder containerBuilder) where T : class, IMFService
        {
            return containerBuilder
                .Register<T>(ServicesLifetimeType)
                .As<T>()
                .As<IMFService>();
        }
    }
}
