using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace SpiralingStudio.Services.Session
{
    /// <summary>
    /// Manages the current user session lifecycle.
    /// </summary>
    public class SessionManager : IMFService
    {
        private readonly object _sessionLock = new object();
        private SessionData _currentSession;
        
        /// <summary>
        /// Gets the current active session. Returns null if no session is active.
        /// </summary>
        public SessionData CurrentSession
        {
            get
            {
                lock (_sessionLock)
                {
                    return _currentSession;
                }
            }
        }
        
        [Inject]
        public SessionManager()
        {
            // Constructor for DI
        }
        
        /// <summary>
        /// Creates a new session with the provided user ID.
        /// </summary>
        /// <param name="userId">The unique user identifier.</param>
        /// <param name="activeSlotIndex">Optional active slot index for the session.</param>
        /// <returns>The newly created session data.</returns>
        public SessionData CreateSession(string userId, int? activeSlotIndex = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            lock (_sessionLock)
            {
                if (_currentSession != null)
                {
                    Debug.LogWarning($"[SessionManager] Overwriting existing session for user '{_currentSession.userID}' with new session for '{userId}'.");
                }

                var sessionId = Guid.NewGuid().ToString();
                _currentSession = new SessionData(sessionId, userId, DateTime.UtcNow, activeSlotIndex);
                Debug.Log($"[SessionManager] Session created: SessionID={sessionId}, UserID={userId}, SlotIndex={activeSlotIndex?.ToString() ?? "none"}");
                return _currentSession;
            }
        }
        
        /// <summary>
        /// Sets the active slot index for the current session.
        /// </summary>
        /// <param name="slotIndex">The slot index to set as active</param>
        public void SetActiveSlot(int slotIndex)
        {
            lock (_sessionLock)
            {
                if (_currentSession == null)
                {
                    throw new InvalidOperationException("[SessionManager] No active session. Create a session first.");
                }
                
                _currentSession.SetActiveSlot(slotIndex);
                Debug.Log($"[SessionManager] Active slot set to {slotIndex} for session {_currentSession.sessionID}");
            }
        }
        
        /// <summary>
        /// Clears the active slot index from the current session.
        /// </summary>
        public void ClearActiveSlot()
        {
            lock (_sessionLock)
            {
                if (_currentSession != null)
                {
                    _currentSession.ClearActiveSlot();
                    Debug.Log($"[SessionManager] Cleared active slot for session {_currentSession.sessionID}");
                }
            }
        }
        
        /// <summary>
        /// Ends the current session.
        /// </summary>
        public void EndSession()
        {
            lock (_sessionLock)
            {
                if (_currentSession != null)
                {
                    Debug.Log($"[SessionManager] Session ended: SessionID={_currentSession.sessionID}, UserID={_currentSession.userID}");
                    _currentSession = null;
                }
                else
                {
                    Debug.LogWarning("[SessionManager] Attempted to end session, but no active session exists.");
                }
            }
        }
        
        /// <summary>
        /// Checks if a session is currently active.
        /// </summary>
        public bool IsSessionActive()
        {
            lock (_sessionLock)
            {
                return _currentSession != null;
            }
        }

        public UniTask[] GetInitializeTasks()
        {
            // Session manager doesn't need async initialization
            return Array.Empty<UniTask>();
        }
    }
}
