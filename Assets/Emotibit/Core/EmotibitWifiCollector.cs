using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SenseXR.Core;
using UnityEngine;

namespace Emotibit.Core
{
    [RequireComponent(typeof (EmotibitManager))]
    public class EmotibitWifiCollector : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("List of names of Emotibit sensors to collect data from")]
        private List<string> sensors;
        
        [SerializeField]
        [Tooltip("Emotibit Config file (as per Oscilloscope app). If none is provided, the default config values will be used.")]
        private TextAsset configFile;
        
        private EmotibitManager _emotibitManager;

        private void Start()
        {
            _emotibitManager = GetComponent<EmotibitManager>();
            foreach (var sensor in sensors)
            {
                _emotibitManager.RegisterSensorCallback(sensor, (values) =>
                {
                    Collect(sensor, values);
                });
            }
        }
        
        private void OnDestroy()
        {
            foreach (var sensor in sensors)
            {
                _emotibitManager?.UnregisterSensorCallback(sensor);
            }
        }

        private static void Collect(string sensorName, List<float> values)
        {
            StatisticsManager.Instance.AddRawLine(DataSingleton.Instance.UserId + "_Emotibit" + sensorName,
                new string[] { DateTime.UtcNow.ToString("o"),
                    string.Join(", ", values.Select(
                        val => Convert.ToString(val, CultureInfo.InvariantCulture))) },
                new[] { "DateTime(UTC), " + sensorName + " Value" });
        }
    }
}