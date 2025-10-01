using System.Collections.Generic;
using SenseXR.Core.Demo.OSC.Emotibit.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Emotibit.Core
{
    /**
     * Monobehaviour class for the Emotibit Manager. It relies on the EmotibitWifiHost class to manage the
     * Emotibit Wifi connection and data transmission. It also provides a callback for when data is received
     * from the Emotibit.
     */
    public class EmotibitManager : MonoBehaviour
    {
        [SerializeField]
        private bool debug;
        [SerializeField]
        private Text debugText;
        
        private EmotibitWifiHost _emotibitHost;
        private readonly Dictionary<string, UnityAction<List<float>>> _sensorCallbacks = new();
        private void Start()
        {
            _emotibitHost = new EmotibitWifiHost(new EmotibitConfig(), Debug.Log, Newtonsoft.Json.JsonConvert.DeserializeObject<EmotibitConfig>  );
            _emotibitHost.OnDataReceived += OnDataReceived;
        }

        /// <summary>
        /// Registers an action for a specific sensor with the provided name.
        /// </summary>
        /// <param name="sensor">The name of the sensor to register the callback for</param>
        /// <param name="callback"> The callback receives an array of values, as Emotibit sensors may report multiple values between readings</param>
        public void RegisterSensorCallback(string sensor, UnityAction<List<float>> callback)
        {
            _sensorCallbacks.Add(sensor, callback);
        }
        
        /// <summary>
        /// Removes the callback for the given sensor.
        /// </summary>
        /// <param name="sensor"></param>
        public void UnregisterSensorCallback(string sensor)
        {
            _sensorCallbacks.Remove(sensor);
        }

        /// <summary>
        /// Identifies what the callback (if any) is to be triggered for the given sensor with the provided values.
        /// </summary>
        /// <param name="sensor"></param>
        /// <param name="values"></param>
        /// <param name="header"></param>
        public void OnDataReceived(string sensor, List<float> values, EmotibitHeader header)
        {
            if (debug)
            {
                var stringifiedValues = "";
                values.ForEach((val) => stringifiedValues += val + ",");
                var debugString = "Sensor: " + sensor + " Values: " + stringifiedValues;
                
                if (debugText)
                {
                    debugText.text = debugString;
                }
                Debug.Log(debugString); 
            }

            foreach (var keyValuePair in _sensorCallbacks)
            {
                if (keyValuePair.Key.Equals(sensor))
                {
                    keyValuePair.Value.Invoke(values);
                }
            }
        }

        private void OnApplicationQuit()
        {
            _emotibitHost?.Destroy();
        }
        
        private void Update()
        {
            _emotibitHost?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _emotibitHost?.Destroy();
            _emotibitHost = null;
            _sensorCallbacks.Clear();
        }
    }
}