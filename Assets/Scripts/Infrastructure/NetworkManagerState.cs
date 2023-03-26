public enum NetworkManagerState
{
    /// <summary>
    /// Network is in Initializing Mode, Only InitTraffic Allowed
    /// </summary>
    Initializing,

    /// <summary>
    /// Network is open
    /// </summary>
    Ready,

    /// <summary>
    /// Developer specified expensive tasks are running, High priority traffic only
    /// </summary>
    Busy,

    /// <summary>
    /// Notwork is disconnected bouncing all traffic
    /// </summary>
    NotConnected
}