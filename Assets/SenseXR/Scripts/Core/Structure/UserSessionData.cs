using System;
using Newtonsoft.Json;

namespace SenseXR.Core.Structure
{
    /// <summary>
    /// Base Container for User Session Data, inherited by specific data types (Raw, Statistics).
    /// </summary>
    public class UserSessionData
    {
        [JsonProperty]
        public string Filename;

        protected readonly string SimulationID;
        protected readonly string UserID;

        public UserSessionData(string simID, string userID)
        {
            SimulationID = simID;
            UserID = userID;
            RegenerateFilename();
        }

        public virtual void RegenerateFilename()
        {
            var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            int currentEpochTime = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            Filename = SimulationID + "_" + UserID + "_" + currentEpochTime + ".json";
        }
    }
}