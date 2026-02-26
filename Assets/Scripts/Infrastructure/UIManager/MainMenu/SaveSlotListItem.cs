using System;
using SpiralingStudio.Services.DataManagement;
using SpiralingStudio.Services.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Infrastructure.UIManager.MainMenu
{
    /// <summary>
    /// Represents a single save slot in the load game list.
    /// Displays slot information and provides select/delete functionality.
    /// </summary>
    public class SaveSlotListItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI slotNameText;
        [SerializeField] private TextMeshProUGUI lastPlayedText;
        [SerializeField] private TextMeshProUGUI playtimeText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button deleteButton;
        
        private MFLocalizationService _localizationService;
        private SaveSlotMetadata _metadata;
        private LoadGamePanel _parentPanel;
        
        [Inject]
        public void Construct(MFLocalizationService localizationService)
        {
            _localizationService = localizationService;
        }
        
        private void Awake()
        {
            // Wire up button listeners
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectClicked);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(OnDeleteClicked);
            }
        }
        
        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectClicked);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(OnDeleteClicked);
            }
        }
        
        /// <summary>
        /// Initializes the list item with save slot metadata.
        /// </summary>
        public void Initialize(SaveSlotMetadata metadata, LoadGamePanel parentPanel)
        {
            _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            _parentPanel = parentPanel ?? throw new ArgumentNullException(nameof(parentPanel));
            
            // If localization service wasn't injected, try to find it
            if (_localizationService == null)
            {
                // Try to get it from the parent context's container
                var context = GetComponentInParent<UiContext>();
                if (context != null && context.Container != null)
                {
                    _localizationService = context.Container.Resolve<MFLocalizationService>();
                }
            }
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// Updates the UI elements with current metadata values.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_metadata == null)
            {
                Debug.LogWarning("[SaveSlotListItem] Cannot update display - metadata is null.");
                return;
            }
            
            // Slot name
            if (slotNameText != null)
            {
                slotNameText.text = _metadata.displayName;
            }
            
            // Last played date
            if (lastPlayedText != null)
            {
                string dateString = _metadata.LastPlayedDate.ToLocalTime().ToString("g"); // General short date/time
                
                if (_localizationService != null)
                {
                    lastPlayedText.text = _localizationService.GetTranslation("ui_saveslot_text_lastplayed", dateString);
                }
                else
                {
                    lastPlayedText.text = $"Last Played: {dateString}";
                }
            }
            
            // Playtime
            if (playtimeText != null)
            {
                float hours = _metadata.totalPlaytimeSeconds / 3600f;
                string hoursString = hours.ToString("F1"); // One decimal place
                
                if (_localizationService != null)
                {
                    playtimeText.text = _localizationService.GetTranslation("ui_saveslot_text_playtime", hoursString);
                }
                else
                {
                    playtimeText.text = $"Playtime: {hoursString}h";
                }
            }
            
            // Progress indicator
            if (progressText != null)
            {
                progressText.text = _metadata.progressIndicator ?? "New Game";
            }
        }
        
        /// <summary>
        /// Sets the interactable state of the slot item buttons.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (selectButton != null)
            {
                selectButton.interactable = interactable;
            }
            
            if (deleteButton != null)
            {
                deleteButton.interactable = interactable;
            }
        }
        
        /// <summary>
        /// Handler for select button click.
        /// </summary>
        private void OnSelectClicked()
        {
            if (_metadata != null && _parentPanel != null)
            {
                Debug.Log($"[SaveSlotListItem] Select clicked for slot {_metadata.slotIndex}.");
                _parentPanel.OnSlotSelected(_metadata.slotIndex);
            }
        }
        
        /// <summary>
        /// Handler for delete button click.
        /// </summary>
        private void OnDeleteClicked()
        {
            if (_metadata != null && _parentPanel != null)
            {
                Debug.Log($"[SaveSlotListItem] Delete clicked for slot {_metadata.slotIndex}.");
                _parentPanel.OnSlotDeleteRequested(_metadata.slotIndex);
            }
        }
    }
}






