namespace SpiralingStudio.Utils
{
    public interface IMfPooledObject
    {
        void OnInstanceCreated();
        void OnInstanceGetFromPool();
        void OnReturnedToPool();
        void OnDestroyed();
    }
}