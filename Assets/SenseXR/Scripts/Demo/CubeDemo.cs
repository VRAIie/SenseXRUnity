using SenseXR.Core.Structure;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SenseXR.Core.Demo
{
    public class CubeDemo : MonoBehaviour
    {
        private Rigidbody _rb;
        private string[] _header = new[] { "time", "id", "vx", "vy", "vz" };
        private object[] _reading = new object[] { 0, "cube", 0, 0, 0 };

        private string[] _headerPos = new[] { "time", "id", "posx", "posy", "posz" };
        private object[] _readingPos = new object[] { 0, "cubePos", 0, 0, 0 };

        // Start is called before the first frame update
        void Start()
        {
            DataSingleton.Instance.SetVariables("12345");
            StatisticsManager.Instance.Initialize();
            _rb = GetComponent<Rigidbody>();

            //We should keep track of when a new session has started
            var testSessionEvent = new SessionEvent()
            {
                ActorItem = new ActorItem
                {
                    ActorId = DataSingleton.Instance.UserId,
                    ActorType = ActorType.User,
                    ActorName = "SenseXR Demo"
                },
                EventInfoItem = new EventInfoItem
                {
                    EventName = EventName.Start,
                    EventVerb = EventVerb.Started
                },
                TimeItem = new TimeItem(),
                ObjectItem = new ObjectItem(),
                ResultItem = new ResultItem()
                {
                    ResultMessage = "Session started",
                    ResultType = ResultType.Succeeded,
                    ResultValue = "1"
                }
            };
            StatisticsManager.Instance.Add(DataSingleton.Instance.UserId, testSessionEvent);
        }

        private void Update()
        {
            _readingPos[0] = Time.time;
            _readingPos[1] = transform.position.x;
            _readingPos[2] = transform.position.y;
            _readingPos[3] = transform.position.z;
            StatisticsManager.Instance.AddRawLine("CubePos",
                _readingPos,
                _headerPos);
        }

        void FixedUpdate()
        {
            
            var force = new Vector3(UnityEngine.Random.Range(-1f,1f), 0 ,UnityEngine.Random.Range(-1f,1f)).normalized;
            _rb.AddForce(force, ForceMode.Force);
            var vel = _rb.velocity;

            //Whenever we want to collect rawdata, we can use the AddRawLine to include data and a header (optional for subsequent calls after the first one)
            _reading[0] = Time.time;
            _reading[1] = vel.x;
            _reading[2] = vel.y;
            _reading[3] = vel.z;
            StatisticsManager.Instance.AddRawLine("CubeSpeed",
                _reading,
                _header);

            if (Input.GetKeyDown(KeyCode.Space) || Input.touchCount > 0)
            {
                //We can collect interaction events when a certain user interaction is made
                var testSessionEvent = new InteractionEvent()
                {
                    ActorItem = new ActorItem
                    {
                        ActorId = DataSingleton.Instance.UserId,
                        ActorType = ActorType.User,
                        ActorName = "SenseXR Demo"
                    },
                    EventInfoItem = new EventInfoItem
                    {
                        EventName = "INPUT_INTERACTION",
                        EventVerb = Input.touchCount > 0 ? "TOUCHED" : "KEY_RELEASED"
                    },
                    TimeItem = new TimeItem(),
                    ObjectItem = new ObjectItem
                    {
                        ObjectId = Input.touchCount > 0 ? "Touch" : "space",
                        ObjectName = Input.touchCount > 0 ? "TouchScreen" : "Keyboard",
                        ObjectType = ObjectType.Interactable
                    },
                    ResultItem = new ResultItem()
                    {
                        ResultMessage = Input.touchCount > 0 ? "Touched the screen" : "Released Spacebar",
                        ResultType = ResultType.Completed,
                        ResultValue = "1"
                    }
                };
                StatisticsManager.Instance.Add(DataSingleton.Instance.UserId, testSessionEvent);
            }
        }

        public void CloseSession()
        {
            //We should also keep track of when a session ends with a session event.
            var testSessionEvent = new SessionEvent()
            {
                ActorItem = new ActorItem
                {
                    ActorId = DataSingleton.Instance.UserId,
                    ActorType = ActorType.User,
                    ActorName = "SenseXR Demo"
                },
                EventInfoItem = new EventInfoItem
                {
                    EventName = EventName.Leave,
                    EventVerb = EventVerb.Finished
                },
                TimeItem = new TimeItem(),
                ObjectItem = new ObjectItem(),
                ResultItem = new ResultItem()
                {
                    ResultMessage = "Session Finished",
                    ResultType = ResultType.Succeeded,
                    ResultValue = "1"
                }
            };
            StatisticsManager.Instance.Add(DataSingleton.Instance.UserId, testSessionEvent);
            
            //Data can then be published (online or locally depending on the settings specified)
            StatisticsManager.Instance.Publish(OnUploadFinished);
        }

        void OnUploadFinished(string res)
        {
            Debug.Log("Request Finished with response : " + res);
            SceneManager.LoadScene(0);
        }
    }
}
