using System; 

namespace MonsterFactory.Services.Session
{
    public class SessionData 
    {
        public string sessionID { get; private set; }
        public string userID { get; private set; }

        public SessionData(string sessionID, string userID, DateTime lastLoginDateTime) 
        {
            this.sessionID = sessionID;
            this.userID = userID;
        } 

    }

}
