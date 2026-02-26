using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SpiralingStudio.Events;

namespace Infrastructure.UIManager
{
    /// <summary>
    /// Base interface for UI components that support show/hide callbacks and visibility event handling.
    /// </summary>
    public interface IUiComponent
{
    void HideGameObject();
    void ShowGameObject();
    ref IDisposable GetDisposableReference();
    UniTask OnUiVisibilityToggled(ToggleUIVisibilityEvent visibilityEvent, CancellationToken cancellationToken);
    }
}