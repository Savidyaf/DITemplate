using Cysharp.Threading.Tasks;


namespace SpiralingStudio.Services
{
    /// <summary>
    /// Marker interface for DI-registered services that can contribute async startup tasks.
    /// Return lightweight tasks to warm caches, establish connections, etc.
    /// </summary>
    public interface IMFService
    {
        /// <summary>
        /// Returns a set of initialization tasks to be awaited in parallel at app startup.
        /// </summary>
        UniTask[] GetInitializeTasks();
    }
}