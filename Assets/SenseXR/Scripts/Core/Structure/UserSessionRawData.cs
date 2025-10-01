using System;
using System.Text;

namespace SenseXR.Core.Structure
{
    /// <summary>
    /// Payload of raw data from the User's Session, only sent at the end of it.
    /// </summary>
    [Serializable]
    public class UserSessionRawData : UserSessionData
    {
        /// <summary> Raw string for JSON data from replay </summary>
        public string RawData {get; set; }
        public StringBuilder RawDataBuilder { get; set; }

        public UserSessionRawData(string simID, string userID, string rawData) : base(simID, userID)
        {
            RawData = rawData;
            RawDataBuilder = new StringBuilder();
            
        }

        public override void RegenerateFilename()
        {
            var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            int currentEpochTime = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
            Filename = SimulationID + "_" + UserID + "_" + currentEpochTime + ".csv";
        }

        public override string ToString()
        {
            return RawData.ToString();
        }
    }
}