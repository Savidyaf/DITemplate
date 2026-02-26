using MessagePipe;
using VContainer;

namespace SpiralingStudio.Events
{
    public static partial class EventRegistrationHelper
    {
        public static void RegisterGlobalEventClasses(IContainerBuilder builder, MessagePipeOptions options)
        {
            EventRegistrationHelper.builder = builder;
            EventRegistrationHelper.options = options;

            RegisterGlobalEvent<TestEvent>();
            RegisterGlobalEvent<DataEventLoadData>();
            RegisterGlobalEvent<DataEventSaveData>();
            RegisterGlobalEvent<SaveSlotCreatedEvent>();
            RegisterGlobalEvent<SaveSlotDeletedEvent>();
            RegisterGlobalEvent<SaveSlotChangedEvent>();

            #region SceneManagement

            RegisterGlobalEvent<PreSceneLoadEvent>();
            RegisterGlobalEvent<PostSceneLoadEvent>();
            RegisterGlobalEvent<PreSceneUnloadEvent>();

            #endregion

            #region UiSystemGlobalEvents

            RegisterGlobalEvent<ToggleGlobalUiInteractions>();
            RegisterGlobalEvent<ToggleGlobalUiVisibility>();

            #endregion

            #region LocalizationEvents

            RegisterGlobalEvent<RefreshLanguageEvent>();

            #endregion

            #region GameInitializationEvents

            RegisterGlobalEvent<GameInitializationProgressEvent>();

            #endregion

            EventRegistrationHelper.builder = null;
            EventRegistrationHelper.options = null;
        }
    }
}

namespace SpiralingStudio.Events
{
    public class MFBaseEvent
    {
    }

    public class TestEvent : MFBaseEvent
    {
    }

    #region DataSystem

    public class DataEventLoadData : MFBaseEvent
    {
        public bool CanOverwrite { get; }

        public DataEventLoadData(bool canOverwrite)
        {
            CanOverwrite = canOverwrite;
        }
    }

    public class DataEventSaveData : MFBaseEvent
    {
        public bool CanForceSave { get; }

        public DataEventSaveData(bool canForceSave)
        {
            CanForceSave = canForceSave;
        }
    }

    #endregion

    #region SaveSlotSystem

    public class SaveSlotCreatedEvent : MFBaseEvent
    {
        public int SlotIndex { get; }
        public string DisplayName { get; }

        public SaveSlotCreatedEvent(int slotIndex, string displayName)
        {
            SlotIndex = slotIndex;
            DisplayName = displayName;
        }
    }

    public class SaveSlotDeletedEvent : MFBaseEvent
    {
        public int SlotIndex { get; }

        public SaveSlotDeletedEvent(int slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }

    public class SaveSlotChangedEvent : MFBaseEvent
    {
        public int OldSlotIndex { get; }
        public int NewSlotIndex { get; }

        public SaveSlotChangedEvent(int oldSlotIndex, int newSlotIndex)
        {
            OldSlotIndex = oldSlotIndex;
            NewSlotIndex = newSlotIndex;
        }
    }

    #endregion

    #region SceneManagement

    public class SceneManagementEvent : MFBaseEvent
    {
        public string SceneName { get; }
        public int SceneLayer { get; }

        protected SceneManagementEvent(string sceneName, int sceneLayer)
        {
            SceneName = sceneName;
            SceneLayer = sceneLayer;
        }
    }

    public class PreSceneLoadEvent : SceneManagementEvent
    {
        public PreSceneLoadEvent(string sceneName, int sceneLayer) : base(sceneName, sceneLayer)
        {
        }
    }

    public class PreSceneUnloadEvent : SceneManagementEvent
    {
        public PreSceneUnloadEvent(string sceneName, int sceneLayer) : base(sceneName, sceneLayer)
        {
        }
    }

    public class PostSceneLoadEvent : SceneManagementEvent
    {
        public PostSceneLoadEvent(string sceneName, int sceneLayer) : base(sceneName, sceneLayer)
        {
        }
    }

    #endregion

    #region Localization

    public class RefreshLanguageEvent : MFBaseEvent
    {
        public readonly string PreviousLanguage;
        public readonly string NewLanguage;

        public RefreshLanguageEvent(string previousLanguage, string newLanguage)
        {
            PreviousLanguage = previousLanguage;
            NewLanguage = newLanguage;
        }
    }

    #endregion

    #region UIEvents

    public class ToggleUIVisibilityEvent : MFBaseEvent
    {
        public readonly bool IsVisible;

        public ToggleUIVisibilityEvent(bool state)
        {
            IsVisible = state;
        }
    }

    public class ToggleUiInteractionsEvent : MFBaseEvent
    {
        public readonly bool IsInteractionDisabled;

        public ToggleUiInteractionsEvent(bool state)
        {
            IsInteractionDisabled = state;
        }
    }

    public class ToggleGlobalUiVisibility : ToggleUIVisibilityEvent
    {
        public ToggleGlobalUiVisibility(bool state) : base(state)
        {
        }
    }

    public class ToggleGlobalUiInteractions : ToggleUiInteractionsEvent
    {
        public ToggleGlobalUiInteractions(bool state) : base(state)
        {
        }
    }

    #endregion
}
