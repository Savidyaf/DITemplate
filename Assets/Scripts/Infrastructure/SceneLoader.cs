using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;


public class SceneLoader : MonoBehaviour
{
    private IPublisher<SceneLoaderState> onNavigationStateChange;
    private readonly LifetimeScope parentScope;


    private List<AsyncOperationHandle<SceneInstance>> loadedSceneHandles =
        new List<AsyncOperationHandle<SceneInstance>>();

    [Inject]
    public SceneLoader(IPublisher<SceneLoaderState> onNavigationStateChange, LifetimeScope currentScope)
    {
        this.onNavigationStateChange = onNavigationStateChange;
        this.parentScope = currentScope;
    }

    public async UniTask LoadScene(string sceneName)
    {
        using (LifetimeScope.EnqueueParent(parentScope))
        {
            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            await handle.ToUniTask();
            loadedSceneHandles.Add(handle);
        }
    }

    public async UniTask UnloadScene(string sceneName)
    {
        AsyncOperationHandle<SceneInstance> handle = loadedSceneHandles.Find(h => h.Result.Scene.name == sceneName);

        if (handle.IsValid())
        {
            await Addressables.UnloadSceneAsync(handle);
            loadedSceneHandles.Remove(handle);
        }
    }

    public async UniTask UnloadAllScenes()
    {
        foreach (AsyncOperationHandle<SceneInstance> handle in loadedSceneHandles)
        {
            if (handle.IsValid())
            {
                await Addressables.UnloadSceneAsync(handle);
            }
        }

        loadedSceneHandles.Clear();
    }

    public List<string> GetLoadedSceneNames()
    {
        List<string> loadedSceneNames = new List<string>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded)
            {
                loadedSceneNames.Add(scene.name);
            }
        }

        return loadedSceneNames;
    }
}