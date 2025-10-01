using System;
using Newtonsoft.Json;

namespace SenseXR.Core.Structure
{
    /// <summary>
    /// Container for Statistics data from a User Session.
    /// </summary>
    [Serializable]
    public class UserSessionStatistics : UserSessionData
    {
        public DtoObject Data { get; set; }

        /// <summary>
        /// Holds the events' data collected for a given user during a given simulation.
        /// </summary>
        /// <param name="simID"></param>
        /// <param name="userID"></param>
        public UserSessionStatistics(string simID, string userID) : base(simID, userID)
        {
            Data = new DtoObject();
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(Data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore});
        }
    }
}