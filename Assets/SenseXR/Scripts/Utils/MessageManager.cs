using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

namespace SenseXR.Utils
{
    [ExecuteInEditMode]
    public class MessageManager : MonoBehaviour
    {
        private const int NumberOfThreads = 8;
        private const string MessageManagerGameObjectName = "Message Manager";
        private readonly Queue<Action> _actions = new Queue<Action>();

        private bool _abort;

        private static MessageManager Instance
        {
            get;
            set;
        }

        static MessageManager()
        {
            ThreadPool.SetMaxThreads(NumberOfThreads, NumberOfThreads);
        }
        // Update is called once per frame
        private void Start()
        {
            Instance = this;
            _abort = false;
        }

        private void Update()
        {
            lock (_actions)
            {
                while (_actions.Count > 0)
                {
                    var action = _actions.Dequeue();
                    action.Invoke();
                }
            }
        }

        private void OnApplicationQuit()
        {
            lock (_actions)
            {
                _actions?.Clear();
            }

            _abort = true;
        }

        /// <summary>
        /// Execute provided action asynchronously, using a ThreadPool.
        /// Warning: Unity related operations may not be executed.
        /// Please chain with QueueOnMainThread.
        /// </summary>
        /// <param name="action">Action to execute</param>
        public static void RunAsync(Action action)
        {
            Initialize();
            ThreadPool.QueueUserWorkItem(RunAction, action);
        }

        private static void RunAction(object a)
        {
            if (Instance == null || Instance._abort == false)
                (a as Action)?.Invoke();
        }

        /// <summary>
        /// Execute provided action synchronously, using Monobehaviour Update event of the Message Manager game object.
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <returns></returns>
        public static void QueueOnMainThread(Action action)
        {
            Initialize();
            lock (Instance._actions)
            {
                Instance._actions.Enqueue(action);
            }
        }

        /// <summary>
        /// Used to ensure that a MessageManager instance is available and initialized for use.
        /// Warning: If ran from a thread other than Main, it may fail silently.
        /// </summary>
        public static void Initialize()
        {
            if (Instance == null)
            {
                var candidate = FindObjectOfType<MessageManager>();
                if (candidate != null)
                {
                    Instance = candidate;
                    return;
                }
                var go = new GameObject(MessageManagerGameObjectName);
                Instance = go.AddComponent<MessageManager>();
                DontDestroyOnLoad(Instance.gameObject);
            }
        }
    }
}