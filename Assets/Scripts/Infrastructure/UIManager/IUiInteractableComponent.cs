using System.Threading;
using Cysharp.Threading.Tasks;
using SpiralingStudio.Events;

namespace Infrastructure.UIManager
{
    /// <summary>
    /// Interface for UI components that support both visibility and interaction events.
    /// Extends IUiComponent to add interaction state handling.
    /// </summary>
    public interface IUiInteractableComponent : IUiComponent
    {
        UniTask OnUiInteractionsToggled(ToggleUiInteractionsEvent interactionEvent, CancellationToken cancellationToken);
    }
}