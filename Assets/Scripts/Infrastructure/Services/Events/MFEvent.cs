using MessagePipe;
using VContainer;

namespace MonsterFactory.Events
{
    /// <summary>
    /// Event Registration helper is just an easy way for us to keep track of events for each lifetime scope
    /// Create a new method for each lifetime scope you create.
    /// </summary>
    public static partial class EventRegistrationHelper
    {
        public static void RegisterGlobalEventClasses(IContainerBuilder builder, MessagePipeOptions options)
        {
            EventRegistrationHelper.builder = builder;
            EventRegistrationHelper.options = options;
            
            //Register Event types here
            RegisterEvent<TestEvent>();

            
            
            EventRegistrationHelper.builder = null;
            EventRegistrationHelper.options = null;
        }

        public static void RegisterOpeningSceneEvents(IContainerBuilder builder, MessagePipeOptions options)
        {
            
        }
    }
}

namespace MonsterFactory.Events
{

    public class MFBaseEvent
    {
    }

    public class TestEvent : MFBaseEvent
    {
    }

    public class PlayerTaskBaseEvent : MFBaseEvent
    {
        public readonly string Id;

        public PlayerTaskBaseEvent(string Id)
        {
            this.Id = Id;
        }
    }
    
    public class PlayerTaskCompleted: PlayerTaskBaseEvent
    {
        public PlayerTaskCompleted(string Id) : base(Id)
        {
        }
    }

    public class PlayerTaskStarted : PlayerTaskBaseEvent
    {
        public PlayerTaskStarted(string Id) : base(Id)
        {
        }
    }

    public class PlayerTaskAborted: PlayerTaskBaseEvent
    {
        public PlayerTaskAborted(string Id) : base(Id)
        {
        }
    }
    
    public class PlayerTaskFailed : PlayerTaskBaseEvent
    {
        public PlayerTaskFailed(string Id) : base(Id)
        {
        }
    }

}

