using Cysharp.Threading.Tasks;
using MessagePipe;
using VContainer;
using VContainer.Unity;

public abstract class LevelLifeTimeScope : LifetimeScope,IAssetLevelScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        RegisterLoaders();
    }
    public abstract void RegisterLoaders();

    public abstract UniTask PreloadAssets();

    public abstract void UnloadAssets();

}

public interface IAssetLevelScope
{
    public UniTask PreloadAssets();
    public void UnloadAssets();
    
    public void RegisterLoaders();
}


public class OpeningLevel : LevelLifeTimeScope
{
    public override void RegisterLoaders()
    {
        throw new System.NotImplementedException();
    }

    public override UniTask PreloadAssets()
    {
        throw new System.NotImplementedException();
    }

    public override void UnloadAssets()
    {
        throw new System.NotImplementedException();
    }
}





