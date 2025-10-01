using extOSC;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace SenseXR.Demo.OSC
{
    [Serializable]
    public class OscPathList
    {
        public List<string> oscPaths;
    }

    [RequireComponent(typeof(OSCTransmitter))]
    [RequireComponent(typeof(OSCReceiver))]
    public class EmotibitOSCCollector : MonoBehaviour
    {
        private OSCTransmitter _transmitter;
        private OSCReceiver _receiver;

        private Type[] _emotibitSensorTypes;
        private readonly List<EmotibitOscSensor> _emotibitSensors = new();

        [Tooltip("JSON file containing the OSC paths to capture. If not provided, nor available in Resources folder, all available sensors will be captured.")]
        [SerializeField]
        private TextAsset jsonAsset;

        void Start()
        {
            _transmitter = GetComponent<OSCTransmitter>();
            _transmitter.AutoConnect = true;
            _transmitter.Connect();

            _receiver = GetComponent<OSCReceiver>();
            LoadAvailableSensorTypes();
        }

        /// <summary>
        /// Loads, creates one instance of each and registers respective callbacks
        /// for all EmotibitOSCSensor subclasses
        /// </summary>
        private void LoadAvailableSensorTypes()
        {
            jsonAsset ??= Resources.Load<TextAsset>("oscPaths");
            if (jsonAsset == null)
            {
                Debug.LogError("OSC Paths JSON file not found in Resources folder. Capturing all available sensors.");
                _emotibitSensorTypes = GetAllSubtypesOf<EmotibitOscSensor>();
                foreach (Type emotibitSensorType in _emotibitSensorTypes)
                {
                    AddBinding(Activator.CreateInstance(emotibitSensorType) as EmotibitOscSensor);
                }
                return;
            }

            OscPathList pathList = JsonUtility.FromJson<OscPathList>(jsonAsset.text);

            if (pathList == null || pathList.oscPaths == null)
            {
                Debug.LogError("OSC Paths JSON is invalid or empty.No sensors will be captured.");
                return;
            }

            foreach (string path in pathList.oscPaths)
            {
                var sensor = new EmotibitOscSensor
                {
                    Path = path,
                    Name = path[(path.LastIndexOf("/", StringComparison.Ordinal) + 1)..].Replace(':', '_')
                };
                AddBinding(sensor);
            }
        }

        /// <summary>
        /// Registers the callback of the given sensor for its own OSC path
        /// </summary>
        /// <param name="sensor">OSC Sensor</param>
        /// <returns>If the binding was succesfful or not</returns>
        public bool AddBinding(EmotibitOscSensor sensor)
        {
            if (sensor.Path == null) return false;
            _emotibitSensors.Add(sensor);
            _receiver.Bind(sensor.Path, sensor.Callback);
            return true;
        }

        /// <summary>
        /// Clears all bindings and reregisters them
        /// </summary>
        public void RefreshBinding()
        {
            _receiver.ClearBinds();
            foreach (var emotibitSensor in _emotibitSensors)
            {
                emotibitSensor.Register(this);
            }
        }

        /// <summary>
        /// Returns all Types of the given Base Type (including itself) that are in the current Assembly
        /// </summary>
        /// <typeparam name="TBase">Base Type to search for</typeparam>
        /// <returns>all Types of the given Base Type</returns>
        private static Type[] GetAllSubtypesOf<TBase>()
        {
            return GetAllSubtypesOf(typeof(TBase));
        }

        /// <summary>
        /// Helper function for the GetAllSubtypesOf template
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        private static Type[] GetAllSubtypesOf(Type baseType)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type != null && type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                .ToArray();
        }

        void Awake()
        {
            if (_transmitter != null) _transmitter.Connect();
        }
    }
}
