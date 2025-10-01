using UnityEngine;

namespace SenseXR.Core
{
    public class DataSingleton : MonoBehaviour
    {
        public static DataSingleton Instance { get; private set; }
        public string UserId { get; private set; }

        private void Awake()
        {
            // If there is an instance, and it's not me, delete myself.

            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
        }

        public void SetVariables(string userId)
        {
            UserId = userId;
        }
        
    }
}
