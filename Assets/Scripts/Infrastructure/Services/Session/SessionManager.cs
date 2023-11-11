using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MonsterFactory.Services.Session
{
    public static class SessionManager 
    {
        public static SessionData sessionData;   
        
        public static SessionData CreateSession()
        {
            return sessionData ??= new SessionData("sessionID", "userID", DateTime.Now);
        }

    }

}
