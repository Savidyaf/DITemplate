namespace SpiralingStudio.Events
{
    /// <summary>
    /// Event published during game initialization to report progress.
    /// Used to update the initialization screen progress bar.
    /// </summary>
    public class GameInitializationProgressEvent : MFBaseEvent
    {
        /// <summary>
        /// Progress value from 0.0 to 1.0 (0% to 100%).
        /// </summary>
        public float Progress { get; }
        
        /// <summary>
        /// Localization key for the current initialization step description.
        /// </summary>
        public string StepDescriptionKey { get; }
        
        public GameInitializationProgressEvent(float progress, string stepDescriptionKey)
        {
            Progress = progress;
            StepDescriptionKey = stepDescriptionKey;
        }
    }
}



