using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Infrastructure.UIManager.MainMenu
{
    /// <summary>
    /// Main menu UI context.
    /// Manages the main menu state and panel visibility.
    /// </summary>
    public class MainMenuContext : UiContext
    {
        [Header("Main Menu Panels")]
        [SerializeField] private LoadGamePanel loadGamePanel;
        
        private bool _loadGamePanelVisible;
        
        protected override void ConfigureContextSpecificServices(IContainerBuilder builder)
        {
            // Register the load game panel if it exists
            if (loadGamePanel != null)
            {
                builder.RegisterComponent(loadGamePanel);
            }
        }
        
        public override void Start()
        {
            base.Start();
            
            // Initially hide the load game panel
            if (loadGamePanel != null)
            {
                loadGamePanel.gameObject.SetActive(false);
            }
            
            Debug.Log("[MainMenuContext] Main menu initialized.");
        }
        
        /// <summary>
        /// Shows the load game panel.
        /// </summary>
        public void ShowLoadGamePanel()
        {
            if (loadGamePanel != null && !_loadGamePanelVisible)
            {
                loadGamePanel.gameObject.SetActive(true);
                loadGamePanel.OnPanelOpened();
                _loadGamePanelVisible = true;
                Debug.Log("[MainMenuContext] Load game panel shown.");
            }
        }
        
        /// <summary>
        /// Hides the load game panel.
        /// </summary>
        public void HideLoadGamePanel()
        {
            if (loadGamePanel != null && _loadGamePanelVisible)
            {
                loadGamePanel.gameObject.SetActive(false);
                _loadGamePanelVisible = false;
                Debug.Log("[MainMenuContext] Load game panel hidden.");
            }
        }
    }
}






