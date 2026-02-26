using System; 

namespace SpiralingStudio.Services.Session
{
    public class SessionData 
    {
        public string sessionID { get; private set; }
        public string userID { get; private set; }
        public int? activeSlotIndex { get; private set; }

        public SessionData(string sessionID, string userID, DateTime lastLoginDateTime, int? activeSlotIndex = null) 
        {
            this.sessionID = sessionID;
            this.userID = userID;
            this.activeSlotIndex = activeSlotIndex;
        }
        
        /// <summary>
        /// Updates the active slot index for this session.
        /// </summary>
        /// <param name="slotIndex">The new active slot index</param>
        public void SetActiveSlot(int slotIndex)
        {
            activeSlotIndex = slotIndex;
        }
        
        /// <summary>
        /// Clears the active slot index.
        /// </summary>
        public void ClearActiveSlot()
        {
            activeSlotIndex = null;
        }
    }

}
