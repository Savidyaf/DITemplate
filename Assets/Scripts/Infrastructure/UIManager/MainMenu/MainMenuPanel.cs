using System;
using Cysharp.Threading.Tasks;
using SpiralingStudio.Services.DataManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Infrastructure.UIManager.MainMenu
{
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("Navigation Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        
        [Header("New Game Settings")]
        [SerializeField] private string defaultSlotNameFormat = "Save {0}";
        
        private MainMenuContext _menuContext;
        private SaveSlotManager _saveSlotManager;
        private bool _isStartingGame;
        
        [Inject]
        public void Construct(SaveSlotManager saveSlotManager)
        {
            _saveSlotManager = saveSlotManager;
        }
        
        private void Start()
        {
            UpdateNewGameButtonState();
        }
        
        private void UpdateNewGameButtonState()
        {
            if (newGameButton == null || _saveSlotManager == null)
                return;
            
            bool canCreate = _saveSlotManager.CanCreateNewSlot();
            newGameButton.interactable = canCreate;
            
            if (!canCreate)
            {
                Debug.Log("[MainMenuPanel] New Game button disabled - save slots at maximum capacity.");
            }
        }
        
        private void Awake()
        {
            _menuContext = GetComponentInParent<MainMenuContext>();
            
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);
            
            if (loadGameButton != null)
                loadGameButton.onClick.AddListener(OnLoadGameClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }
        
        private void OnDestroy()
        {
            if (newGameButton != null)
                newGameButton.onClick.RemoveListener(OnNewGameClicked);
            
            if (loadGameButton != null)
                loadGameButton.onClick.RemoveListener(OnLoadGameClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);
        }
        
        private void OnNewGameClicked()
        {
            Debug.Log("[MainMenuPanel] New Game clicked.");

            if (_isStartingGame)
            {
                Debug.Log("[MainMenuPanel] Already starting a game, ignoring.");
                return;
            }

            _isStartingGame = true;
            SetButtonsInteractable(false);

            string slotName = string.Format(defaultSlotNameFormat, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            Debug.Log($"[MainMenuPanel] Starting new game with slot: {slotName}");

            // TODO: Implement new game start via a GameFlowService or similar orchestration service.
            Debug.Log("[MainMenuPanel] New game flow not yet implemented.");
            _isStartingGame = false;
            SetButtonsInteractable(true);
            UpdateNewGameButtonState();
        }
        
        private void OnLoadGameClicked()
        {
            Debug.Log("[MainMenuPanel] Load Game clicked.");
            
            if (_menuContext != null)
            {
                _menuContext.ShowLoadGamePanel();
            }
            else
            {
                Debug.LogWarning("[MainMenuPanel] Cannot show load game panel - MainMenuContext not found.");
            }
        }
        
        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuPanel] Settings clicked.");
            // TODO: Implement settings UI
        }
        
        private void OnExitClicked()
        {
            Debug.Log("[MainMenuPanel] Exit clicked.");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        private void SetButtonsInteractable(bool interactable)
        {
            if (newGameButton != null) newGameButton.interactable = interactable;
            if (loadGameButton != null) loadGameButton.interactable = interactable;
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (exitButton != null) exitButton.interactable = interactable;
        }
    }
}
