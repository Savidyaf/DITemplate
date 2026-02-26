using System.Diagnostics;
using UnityEngine;

namespace SpiralingStudio.Services
{
    /// <summary>
    /// Centralized logging utility with conditional compilation support.
    /// Debug logs are compiled out in release builds for performance.
    /// </summary>
    public static class MFLogger
    {
        /// <summary>
        /// Logs a debug message. Only compiled in development builds.
        /// </summary>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void LogDebug(string message, Object context = null)
        {
            if (context != null)
                UnityEngine.Debug.Log($"[SS] {message}", context);
            else
                UnityEngine.Debug.Log($"[SS] {message}");
        }

        /// <summary>
        /// Logs an info message. Available in all builds.
        /// </summary>
        public static void LogInfo(string message, Object context = null)
        {
            if (context != null)
                UnityEngine.Debug.Log($"[SS] {message}", context);
            else
                UnityEngine.Debug.Log($"[SS] {message}");
        }

        /// <summary>
        /// Logs a warning message. Available in all builds.
        /// </summary>
        public static void LogWarning(string message, Object context = null)
        {
            if (context != null)
                UnityEngine.Debug.LogWarning($"[SS] {message}", context);
            else
                UnityEngine.Debug.LogWarning($"[SS] {message}");
        }

        /// <summary>
        /// Logs an error message. Available in all builds.
        /// </summary>
        public static void LogError(string message, Object context = null)
        {
            if (context != null)
                UnityEngine.Debug.LogError($"[SS] {message}", context);
            else
                UnityEngine.Debug.LogError($"[SS] {message}");
        }

        /// <summary>
        /// Logs an exception. Available in all builds.
        /// </summary>
        public static void LogException(System.Exception exception, Object context = null)
        {
            if (context != null)
                UnityEngine.Debug.LogException(exception, context);
            else
                UnityEngine.Debug.LogException(exception);
        }
    }
}





