using extOSC;
using System;
using System.Globalization;
using System.Linq;
using SenseXR.Core;

namespace SenseXR.Demo.OSC
{
    /// <summary>
    /// Class for the Emotibit Sensor callbacks and OSC Paths
    /// Each sensor is expected, as per Emotibit specification, to only have one float value per message
    /// </summary>
    public class EmotibitOscSensor
    {
        public virtual string Name { get; set; }
        public string Path;
        private const string BasePath = "/EmotiBit/0/";
        public EmotibitOscSensor()
        {
            BuildPath();
        }

        private void BuildPath()
        {
            Path = BasePath + Name;
        }

        /// <summary>
        /// Allows for this sensor to be bound to a collector (associating its path with its callback event)
        /// </summary>
        /// <param name="emotibitOscCollector"></param>
        public void Register(EmotibitOSCCollector emotibitOscCollector)
        {
            emotibitOscCollector.AddBinding(this);
        }

        /// <summary>
        /// Callback to trigger SenseXR collection from the OSCMessage sent to the bound path
        /// </summary>
        /// <param name="oscMessage"></param>
        public void Callback(OSCMessage oscMessage)
        {
            StatisticsManager.Instance.AddRawLine(DataSingleton.Instance.UserId + "_Emotibit" + Name,
                new string[] { DateTime.UtcNow.ToString("o"),
                    string.Join(", ", oscMessage.Values.Select(
                        msg => Convert.ToString(msg.FloatValue, CultureInfo.InvariantCulture))) },
                new[] { "DateTime(UTC), " + Name + " Value" });
        }
    }
}