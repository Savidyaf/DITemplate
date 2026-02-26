using SpiralingStudio.Services.DataManagement;
using VContainer;

namespace SpiralingStudio.Services
{
    public static class RuntimeDataProviderRegistrationHelper
    {
        /// <summary>
        /// Registers the data providers with the container builder.
        /// </summary>
        /// <param name="containerBuilder">The container builder used for registration.</param>
        public static void RegisterDataProviders(IContainerBuilder containerBuilder)
        {
            containerBuilder.Register(typeof(MFLocallyStoredDataInstanceProvider<>), Lifetime.Singleton);
            containerBuilder.Register<IDbLoaderFactory, DbLoaderFactory>(Lifetime.Singleton);
        }
    }
}