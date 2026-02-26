using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SpiralingStudio.Services.DataManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Infrastructure.UIManager.MainMenu
{
    public class LoadGamePanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private SaveSlotListItem slotItemPrefab;
        [SerializeField] private Button backButton;
        
        [Header("Empty State")]
        [SerializeField] private GameObject emptyStatePanel;
        
        [Header("Loading State")]
        [SerializeField] private GameObject loadingPanel;
        
        private SaveSlotManager _saveSlotManager;
        private MainMenuContext _menuContext;
        private List<SaveSlotListItem> _instantiatedItems = new List<SaveSlotListItem>();
        private bool _isLoadingGame;
        
        [Inject]
        public void Construct(SaveSlotManager saveSlotManager)
        {
            _saveSlotManager = saveSlotManager ?? throw new ArgumentNullException(nameof(saveSlotManager));
        }
        
        private void Awake()
        {
            _menuContext = GetComponentInParent<MainMenuContext>();
            
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
            
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }
        }
        
        public void OnPanelOpened()
        {
            PopulateSlotList();
        }
        
        private void PopulateSlotList()
        {
            ClearSlotList();
            
            if (_saveSlotManager == null)
            {
                Debug.LogWarning("[LoadGamePanel] SaveSlotManager is null. Cannot populate slot list.");
                ShowEmptyState(true);
                return;
            }
            
            try
            {
                var slots = _saveSlotManager.GetAllSlotMetadata();
                
                if (slots == null || slots.Count == 0)
                {
                    Debug.Log("[LoadGamePanel] No save slots found.");
                    ShowEmptyState(true);
                    return;
                }
                
                ShowEmptyState(false);
                
                foreach (var slotMetadata in slots)
                {
                    if (slotItemPrefab != null && contentContainer != null)
                    {
                        SaveSlotListItem item = Instantiate(slotItemPrefab, contentContainer);
                        item.Initialize(slotMetadata, this);
                        _instantiatedItems.Add(item);
                    }
                }
                
                Debug.Log($"[LoadGamePanel] Populated {_instantiatedItems.Count} save slot(s).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadGamePanel] Error populating slot list: {ex}");
                ShowEmptyState(true);
            }
        }
        
        private void ClearSlotList()
        {
            foreach (var item in _instantiatedItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _instantiatedItems.Clear();
        }
        
        private void ShowEmptyState(bool show)
        {
            if (emptyStatePanel != null)
            {
                emptyStatePanel.SetActive(show);
            }
        }
        
        private void ShowLoadingState(bool show)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(show);
            }
            
            foreach (var item in _instantiatedItems)
            {
                if (item != null)
                {
                    item.SetInteractable(!show);
                }
            }
            
            if (backButton != null)
            {
                backButton.interactable = !show;
            }
        }
        
        public void OnSlotSelected(int slotIndex)
        {
            Debug.Log($"[LoadGamePanel] Slot {slotIndex} selected.");

            if (_isLoadingGame)
            {
                Debug.Log("[LoadGamePanel] Already loading a game, ignoring.");
                return;
            }

            _isLoadingGame = true;
            ShowLoadingState(true);

            // TODO: Implement game loading via a GameFlowService or similar orchestration service.
            Debug.Log($"[LoadGamePanel] Slot {slotIndex} selected. Implement game load logic.");
            _isLoadingGame = false;
            ShowLoadingState(false);
        }
        
        public async void OnSlotDeleteRequested(int slotIndex)
        {
            Debug.Log($"[LoadGamePanel] Delete requested for slot {slotIndex}.");
            
            if (_isLoadingGame)
            {
                return;
            }
            
            try
            {
                await _saveSlotManager.DeleteSlot(slotIndex, destroyCancellationToken);
                Debug.Log($"[LoadGamePanel] Slot {slotIndex} deleted successfully.");
                
                PopulateSlotList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadGamePanel] Error deleting slot {slotIndex}: {ex}");
            }
        }
        
        private void OnBackClicked()
        {
            if (_isLoadingGame)
            {
                return;
            }
            
            Debug.Log("[LoadGamePanel] Back clicked.");
            
            if (_menuContext != null)
            {
                _menuContext.HideLoadGamePanel();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
