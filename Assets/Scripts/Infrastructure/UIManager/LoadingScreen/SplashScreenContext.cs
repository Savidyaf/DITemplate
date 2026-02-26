using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Infrastructure.UIManager.LoadingScreen
{
    public class SplashScreenContext : UiContext
    {
        
        public override void Start()
        {
            base.Start();
            Debug.Log("[LoadingScreenContext] Loading screen displayed.");
        }
    }
}



