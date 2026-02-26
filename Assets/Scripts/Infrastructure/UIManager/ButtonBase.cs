using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using SpiralingStudio.Events;
using UnityEngine;
using VContainer;

namespace Infrastructure.UIManager
{
    /// <summary>
    /// Abstract base class for simple interactable UI elements.
    /// Subscribes directly to global visibility and interaction events.
    /// </summary>
    public abstract class ButtonBase : MonoBehaviour, IUiInteractableComponent
    {
        private IDisposable _messagePipeSubscriptions;

        /// <summary>
        /// VContainer injection point. Subscribes to global visibility and interaction events.
        /// </summary>
        [Inject]
        public void Construct(
            IAsyncSubscriber<ToggleGlobalUiVisibility> visibilityToggleSubscriber,
            IAsyncSubscriber<ToggleGlobalUiInteractions> interactionsToggleSubscriber)
        {
            this.RegisterContextSubscriptions(visibilityToggleSubscriber, interactionsToggleSubscriber);
        }

        public virtual void HideGameObject() => gameObject.SetActive(false);
        public virtual void ShowGameObject() => gameObject.SetActive(true);
        public ref IDisposable GetDisposableReference() => ref _messagePipeSubscriptions;

        /// <summary>
        /// Called when global UI visibility is toggled.
        /// Override to implement custom visibility behavior.
        /// </summary>
        public abstract UniTask OnUiVisibilityToggled(ToggleUIVisibilityEvent visibilityEvent, CancellationToken cancellationToken);
        
        /// <summary>
        /// Called when global UI interactions are toggled.
        /// Override to implement custom interaction behavior.
        /// </summary>
        public abstract UniTask OnUiInteractionsToggled(ToggleUiInteractionsEvent interactionEvent, CancellationToken cancellationToken);

        protected virtual void OnDestroy()
        {
            _messagePipeSubscriptions?.Dispose();
        }
    }
}
