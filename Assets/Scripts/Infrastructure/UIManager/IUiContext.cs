namespace Infrastructure.UIManager
{
    /// <summary>
    /// A basic interface for something that constitutes a UI context:
    /// - A group of UI elements that can be shown/hidden together.
    /// - Potentially also has subcontexts that can be shown/hidden together.
    /// - Extends IUiInteractableComponent to support both visibility and interaction events.
    /// </summary>
    public interface IUiContext : IUiInteractableComponent
    {
        
    }
}