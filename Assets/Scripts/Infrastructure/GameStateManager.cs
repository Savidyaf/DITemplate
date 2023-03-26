using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public class GameStateManager : MonoBehaviour
{
    private SceneLoader _sceneLoader;

    [Inject]
    public void InitializeGameManager(PreLoadedGameSettings preLoadedGameSettings, SceneLoader sceneLoader)
    {
        this._sceneLoader = sceneLoader;
        this._sceneLoader.LoadScene(preLoadedGameSettings.StartingSceneName).Forget();
    }
}