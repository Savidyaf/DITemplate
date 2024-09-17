using Cysharp.Threading.Tasks;


namespace MonsterFactory.Services
{
    public interface IMFService
    {
        public UniTask[] GetInitializeTasks();
    }
}