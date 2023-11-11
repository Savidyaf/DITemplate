using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace MonsterFactory.Services.SceneManagement
{
    public interface ISceneNavigationManager
    {
        public UniTask LoadScene(SceneName sceneToNavigate, bool pushCurrentSceneToStack = true,
            SceneTransitionOptions sceneUnloadOption = null, SceneTransitionOptions sceneLoadedOption = null);

        public UniTask UnloadScene(bool goToRoot = false); 
    }

    public class SceneNavigationManager : MFService, ISceneNavigationManager
    {
        private readonly ISceneTransitionController sceneTransitionController;
        private readonly LifetimeScope parentLifetimeScope; 
        private Stack<SceneName> sceneStack = new Stack<SceneName>();
        private SceneName? activeScene = null;

        private bool isNavigating;

        private readonly SceneTransitionOptions _defaultInOption = new SceneTransitionOptions
        {
            fadeColor = default,
            transitionDuration = 1f,
        };

        private readonly SceneTransitionOptions _defaultOutOption = new SceneTransitionOptions
        {
            fadeColor = default,
            transitionDuration = 1f,
        };


        [Inject]
        public SceneNavigationManager(ISceneTransitionController sceneTransitionController, LifetimeScope parentLifetimeScope)
        {
            this.sceneTransitionController = sceneTransitionController;
            this.parentLifetimeScope = parentLifetimeScope; 
        }

        #region API

        public async UniTask LoadScene(SceneName sceneToNavigate, bool pushCurrentSceneToStack = true,
            SceneTransitionOptions sceneUnloadOption = null, SceneTransitionOptions sceneLoadedOption = null)
        {
            await CreateTransitionFadeTask(sceneToNavigate,
                UniTask.WhenAll(new[]
                {
                    CreateUnloadActiveSceneTask(),
                    CreateLoadSceneTask(sceneToNavigate)
                }), 
                sceneUnloadOption, sceneLoadedOption); 

            if (pushCurrentSceneToStack)
            {
                sceneStack.Push(sceneToNavigate);
            }
            activeScene = sceneToNavigate;
        }

        public async UniTask UnloadScene(bool goToRoot = false)
        {
            if (sceneStack.TryPeek(out SceneName lastScene))
            {
                if (lastScene == activeScene)
                {
                    sceneStack.Pop();
                }
            }

            await CreateUnloadActiveSceneTask(); 

            if (!goToRoot)
            {
                if (sceneStack.TryPeek(out SceneName lastActiveScene))
                {
                    await CreateTransitionFadeTask(lastActiveScene, CreateLoadSceneTask(lastActiveScene)); 
                    activeScene = lastActiveScene;
                }
            }
        }

        #endregion


        #region Implementation

        private UniTask CreateLoadSceneTask(SceneName sceneName)
        {
            UniTask loadSceneTask = SceneManager.LoadSceneAsync((int)sceneName, LoadSceneMode.Additive).ToUniTask();
            return loadSceneTask;
        }

        private UniTask CreateUnloadActiveSceneTask()
        {
            if (activeScene != null)
            {
                UniTask unloadSceneTask = SceneManager.UnloadSceneAsync((int)activeScene.Value).ToUniTask();
                activeScene = null; 
                
                return unloadSceneTask;
            }

            return new UniTask();
        }

        private async UniTask CreateTransitionFadeTask(SceneName sceneToNavigate, UniTask loadUnloadTasks,
            SceneTransitionOptions sceneUnloadOption = null, SceneTransitionOptions sceneLoadedOption = null)
        {
            if (isNavigating)
            {
                UniTask.WaitUntil(() => isNavigating == false);
            }

            isNavigating = true;

            await sceneTransitionController.TransitionFadeInTask(sceneLoadedOption ?? _defaultInOption);

            await loadUnloadTasks; 

            await sceneTransitionController.TransitionFadeOutTask(sceneUnloadOption ?? _defaultOutOption, false);

            isNavigating = false;
        }
        #endregion
    }


    public class SceneTransitionOptions
    {
        public Color fadeColor;
        public float transitionDuration;
    }
}