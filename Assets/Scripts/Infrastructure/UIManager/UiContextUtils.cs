using System;
using MessagePipe;
using SpiralingStudio.Events;

namespace Infrastructure.UIManager
{
    /// <summary>
    /// Utility extension methods for simplifying event subscriptions for UI components.
    /// Provides convenience methods for registering visibility and interaction event handlers.
    /// </summary>
    public static class UiContextUtils
    {
        /// <summary>
        /// Subscribes an IUiInteractableComponent to visibility and interaction events.
        /// Uses global events (ToggleGlobalUiVisibility, ToggleGlobalUiInteractions) directly.
        /// </summary>
        /// <typeparam name="TVisEvent">The visibility event type (should be ToggleGlobalUiVisibility or subclass).</typeparam>
        /// <typeparam name="TInteractEvent">The interaction event type (should be ToggleGlobalUiInteractions or subclass).</typeparam>
        /// <param name="uiComponent">The UI component to subscribe.</param>
        /// <param name="visibilitySubscriber">Subscriber for visibility events.</param>
        /// <param name="interactionsSubscriber">Subscriber for interaction events.</param>
        public static void RegisterContextSubscriptions<TVisEvent, TInteractEvent>(
            this IUiInteractableComponent uiComponent,
            IAsyncSubscriber<TVisEvent> visibilitySubscriber,
            IAsyncSubscriber<TInteractEvent> interactionsSubscriber)
            where TVisEvent : ToggleUIVisibilityEvent
            where TInteractEvent : ToggleUiInteractionsEvent
        {
            // Dispose any existing subscriptions to prevent memory leaks
            ref IDisposable existingSubscription = ref uiComponent.GetDisposableReference();
            existingSubscription?.Dispose();
            
            var bagBuilder = DisposableBag.CreateBuilder();
            visibilitySubscriber?.Subscribe(uiComponent.OnUiVisibilityToggled).AddTo(bagBuilder);
            interactionsSubscriber?.Subscribe(uiComponent.OnUiInteractionsToggled).AddTo(bagBuilder);
            existingSubscription = bagBuilder.Build();
        }

        /// <summary>
        /// Subscribes a basic IUiComponent only to visibility events.
        /// </summary>
        /// <typeparam name="TVisEvent">The visibility event type.</typeparam>
        /// <param name="uiComponent">The UI component to subscribe.</param>
        /// <param name="visibilitySubscriber">Subscriber for visibility events.</param>
        public static void RegisterComponentSubscription<TVisEvent>(
            this IUiComponent uiComponent,
            IAsyncSubscriber<TVisEvent> visibilitySubscriber)
            where TVisEvent : ToggleUIVisibilityEvent
        {
            // Dispose any existing subscriptions to prevent memory leaks
            ref IDisposable existingSubscription = ref uiComponent.GetDisposableReference();
            existingSubscription?.Dispose();
            
            var bagBuilder = DisposableBag.CreateBuilder();
            visibilitySubscriber?.Subscribe(uiComponent.OnUiVisibilityToggled).AddTo(bagBuilder);
            existingSubscription = bagBuilder.Build();
        }

        /// <summary>
        /// Disposes existing subscriptions and registers new ones.
        /// This is an alias for RegisterContextSubscriptions since both now safely handle re-registration.
        /// </summary>
        /// <typeparam name="TVisEvent">The visibility event type.</typeparam>
        /// <typeparam name="TInteractEvent">The interaction event type.</typeparam>
        /// <param name="uiComponent">The UI component to subscribe.</param>
        /// <param name="visibilitySubscriber">Subscriber for visibility events.</param>
        /// <param name="interactionsSubscriber">Subscriber for interaction events.</param>
        public static void ReregisterContextSubscriptions<TVisEvent, TInteractEvent>(
            this IUiInteractableComponent uiComponent,
            IAsyncSubscriber<TVisEvent> visibilitySubscriber,
            IAsyncSubscriber<TInteractEvent> interactionsSubscriber)
            where TVisEvent : ToggleUIVisibilityEvent
            where TInteractEvent : ToggleUiInteractionsEvent
        {
            // Delegate to RegisterContextSubscriptions which now safely handles re-registration
            uiComponent.RegisterContextSubscriptions(visibilitySubscriber, interactionsSubscriber);
        }
    }
}
