using MessagePipe;
using VContainer;

namespace SpiralingStudio.Events
{
    public static partial class EventRegistrationHelper
    {
        private static IContainerBuilder builder;
        private static MessagePipeOptions options;

        private static MessagePipeOptions _scopedOptions = new MessagePipeOptions
        {
            DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel,
            EnableCaptureStackTrace = false,
            HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore,
            InstanceLifetime = InstanceLifetime.Scoped,
            RequestHandlerLifetime = InstanceLifetime.Scoped
        };

        private static void RegisterGlobalEvent<TtypedEvent>() where TtypedEvent : MFBaseEvent
        {
            builder.RegisterMessageBroker<TtypedEvent>(options);
        }

        public static void RegisterScopedEvent<TtypedEvent>(this IContainerBuilder builderLifetime) where TtypedEvent : MFBaseEvent
        {
            builderLifetime.RegisterMessageBroker<TtypedEvent>(_scopedOptions);
        }
    }

}