using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SenseXR.Core.Structure;
using SenseXR.Utils;

namespace SenseXR.Core
{
    public class StatisticsManager
    {
        #region Properties
        private Dictionary<string,int> RawDataLinesSize { get; set; }
        private string SimulationID { get; set; }
        private string ScenarioID { get; set; }

        private static bool Initialized
        {
            get
            {
                if (Instance == null)
                {
                    UnityEngine.Debug.LogError("Uninitialized Statistics Manager");
                    return false;
                }
                return true;
            }
        }
        #endregion

        private const int RawDataLineSizeThreshold = 5000;
        private readonly HashSet<string> _uploads = new ();

        private Dictionary<string, UserSessionStatistics> UserStatistics
        {
            get;
            set;
        }
        private Dictionary<string, UserSessionRawData> UserRawData
        {
            get;
            set;
        }
        

        public static StatisticsManager Instance { get; }

        static StatisticsManager()
        {
            Instance = new StatisticsManager
            {
                UserStatistics = new Dictionary<string, UserSessionStatistics>(),
                UserRawData = new Dictionary<string, UserSessionRawData>(),
                RawDataLinesSize = new Dictionary<string, int>()
            };
        }

        /// <summary>
        /// Initialize the Statistics Manager for data collection and subsequent upload.
        /// </summary>
        public void Initialize()
        {
            if (!Initialized) return;
            Instance.SimulationID = Guid.NewGuid().ToString();
            Instance.ScenarioID = Guid.NewGuid().ToString();
            Instance.UserStatistics = new Dictionary<string, UserSessionStatistics>();
            Instance.UserRawData = new Dictionary<string, UserSessionRawData>();
        }

        /// <summary>
        /// Returns the UserSessionStatistics for the provided userID, or creates it if it doesn't exist
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        private UserSessionStatistics GetOrCreate(string userID)
        {
            // null check for offline
            if (userID == null) return null;

            if (Instance.UserStatistics.TryGetValue(userID, out var uss))
                return uss;
            Instance.UserStatistics.Add(userID, new UserSessionStatistics(SimulationID, userID));
            return Instance.UserStatistics[userID];
        }

        private UserSessionRawData GetOrCreateRawData(string userID, string rawData)
        {
            if (userID == null) return null;

            if (Instance.UserRawData.TryGetValue(userID, out var urd))
                return urd;
            Instance.UserRawData.Add(userID, new UserSessionRawData(SimulationID, userID, rawData));
            Instance.UserRawData[userID].RawDataBuilder.Append(rawData);
            return Instance.UserRawData[userID];
        }

        /// <summary>
        /// Adds an InteractionEvent to the associated User Statistics
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="ie"></param>
        public void Add(string userID, InteractionEvent ie)
        {
            if(!Initialized) return;
            ie.SimulationInstanceId = Instance.SimulationID;
            ie.ScenarioInstanceId = Instance.ScenarioID;
            Instance.GetOrCreate(userID)?.Data.Add(ie);
        }

        /// <summary>
        /// Adds a SessionEvent to the associated User Statistics
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="se"></param>
        public void Add(string userID, SessionEvent se)
        {
            if(!Initialized) return;
            se.SimulationInstanceId = Instance.SimulationID;
            se.ScenarioInstanceId = Instance.ScenarioID;
            Instance.GetOrCreate(userID)?.Data.Add(se);

        }

        /// <summary>
        /// Uploads to cloud storage if Debug is false, else it stores in My Documents / SenseXR
        /// </summary>
        public void Publish(Action<string> cb = null, bool justRawData = false, UserSessionRawData rd = null)
        {
            if (!Initialized) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var basePath = Path.Combine(Application.persistentDataPath, "SenseXR", Instance.SimulationID);
#else
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SenseXR", Instance.SimulationID);
#endif
            foreach (var userStatistics in Instance.UserStatistics)
            {
                if (!justRawData)
                {
                    MessageManager.RunAsync(() =>
                    {
                        var path = Path.Combine(basePath, userStatistics.Value.Filename);

                        if (IsPublishing(path)) return;

                        //Create Directory if it does not exist
                        var directoryPath = Path.GetDirectoryName(path);
                        if (!Directory.Exists(directoryPath))
                        {
                            if (directoryPath != null) Directory.CreateDirectory(directoryPath);
                        }

                        var contents = userStatistics.Value.ToString();
                        File.WriteAllText(path, contents);
                        MessageManager.QueueOnMainThread(() =>
                        {
                            StopPublishing(path);
                            Instance.SimulationID = Guid.NewGuid().ToString();
                            cb?.Invoke(contents);
                        });
                    });
                }
            }

            if (Instance.UserRawData == null || Instance.UserRawData.Count <= 0) return;
            
            if(rd == null)
            {
                foreach (var userRawData in Instance.UserRawData)
                {
                    userRawData.Value.RawData = userRawData.Value.RawDataBuilder.ToString();
                    PublishUserRawData(basePath, userRawData, cb);
                }
            }
            else
            {
                PublishUserRawData(basePath, new KeyValuePair<string, UserSessionRawData>(Instance.ScenarioID, rd), cb);
            }
        }


        private bool IsPublishing(string key)
        {
            return !_uploads.Add(key);
        }

        private void StopPublishing(string key)
        {
            if (_uploads.Contains(key)) _uploads.Remove(key);
        }

        private void PublishUserRawData(string basePath, KeyValuePair<string, UserSessionRawData> userRawData, Action<string> cb)
        {
            var path = Path.Combine(basePath, userRawData.Value.Filename);

            if (IsPublishing(path)) return;

            MessageManager.RunAsync(() =>
            {
                //Create Directory if it does not exist
                var directoryPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(directoryPath))
                {
                    if (directoryPath != null) Directory.CreateDirectory(directoryPath);
                }

                var contents = userRawData.Value.ToString();
                File.WriteAllText(path, contents);
                MessageManager.QueueOnMainThread(() =>
                {
                    StopPublishing(path);
                    cb?.Invoke(contents);
                });
            });
        }

        /// <summary>
        /// Adds a SessionEvent to the associated User Statistics
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="se"></param>
        public void Add(Guid userID, SessionEvent se)
        {
            Add(userID.ToString(), se);
        }

        /// <summary>
        /// Adds a InteractionEvent to the associated User Statistics
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="ie"></param>
        public void Add(Guid userID, InteractionEvent ie)
        {
            Add(userID.ToString(), ie);
        }

        private void AddRaw(string userID, string rawData, int size = 0)
        {
            if (!Initialized) return;
            Instance.GetOrCreateRawData(userID, rawData);
            RawDataLinesSize[userID]+=size;
            if (RawDataLinesSize[userID] > RawDataLineSizeThreshold)
            {
                Publish(  (_) => 
                {
                    RawDataLinesSize[userID] = 0;
                    Instance.UserRawData.Remove(userID);
                }, true);
            }
        }

        private static string ToInvariantCsvLine(object[] values)
        {
            var separator = CultureInfo.InvariantCulture.TextInfo.ListSeparator;

            return string.Join(separator, values.Select(FormatCsvValue));
        }

        private static string FormatCsvValue(object value)
        {
            var str = value is IFormattable f
                ? f.ToString(null, CultureInfo.InvariantCulture)
                : value?.ToString() ?? "";

            if (str.Contains('"'))
                str = str.Replace("\"", "\"\"");

            if (str.Contains(',') || str.Contains('\n') || str.Contains('\r') || str.Contains('"'))
                str = $"\"{str}\"";

            return str;
        }

        /// <summary>
        /// Manually changes the associated rawdata to include the given line (and header if the user rawdata doesn't yet exist).
        /// Warning: should not be used together with the RawDataManager, as this data will be potentially overwritten by it.
        /// </summary>
        /// <param name="containerID"></param>
        /// <param name="dataLine"></param>
        /// <param name="header"></param>
        public void AddRawLine(string containerID, object[] dataLine, string[] header = null)
        {
            if (!Initialized) return;
            if (dataLine.Length < 1) return;
            var rd = Instance.GetOrCreateRawData(containerID, ToInvariantCsvLine(header));
            rd.RawDataBuilder.Append("\n" + ToInvariantCsvLine(dataLine));
            // Add the new line to the active buffer
            RawDataLinesSize.TryAdd(containerID, 1);
            RawDataLinesSize[containerID]++;
            
            // If we've reached the threshold, swap buffers and publish the full one
            if (RawDataLinesSize[containerID] > RawDataLineSizeThreshold)
            {
                rd.RawData = rd.RawDataBuilder.ToString();
                rd.RawDataBuilder = new StringBuilder();
                
                Publish((_) =>
                {
                    // We do not touch the active buffer here; new lines may have been accumulated meanwhile
                    rd.RawData = "";
                    rd.RegenerateFilename();
                    RawDataLinesSize[containerID] = 0;
                }, true, rd);
            }
        }

        public void AddRaw(Guid userID, string rawData)
        {
            AddRaw(userID.ToString(), rawData);
        }
    }
}