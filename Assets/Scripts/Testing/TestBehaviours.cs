using System;
using System.Collections;
using System.Collections.Generic;
using MonsterFactory.Services.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class TestBehaviours : MonoBehaviour
{
    #region Service Interfaces

    private ISceneNavigationManager _sceneNavigationManager; 

    #endregion

    #region Scene Navigation Test Properties

    [SerializeField] private Button navigateSceneButton;
    [SerializeField] private Button unloadSceneButton;
    [SerializeField] private SceneName _sceneName; 

    #endregion

    private void Start()
    {
        var lifetimeScope = LifetimeScope.Find<GameLifetimeScope>();
        _sceneNavigationManager =  lifetimeScope.Container.Resolve<ISceneNavigationManager>(); 

        navigateSceneButton?.onClick.AddListener(NavigateToSceneOnClick); 
        unloadSceneButton?.onClick.AddListener(UnloadScene);
    }

    #region Scene Navigation 

    private void NavigateToSceneOnClick()
    {
        _sceneNavigationManager.LoadScene(_sceneName);
    }

    private void UnloadScene()
    {
        _sceneNavigationManager.UnloadScene();
    }

    // [Inject]
    // public void GetSceneNavigationManager(ISceneNavigationManager sceneNavigationManager)
    // {
    //     _sceneNavigationManager = sceneNavigationManager; 
    // }

    #endregion

    
}
