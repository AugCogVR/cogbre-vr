using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Globalization;
using LitJson;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace PUL
{
    [System.Serializable]
    public class AIAssistantManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Nexus Client object

        public GameObject AIAssistantManagerObject; 

        // END: These values are wired up in the Unity Editor -> Nexus Client object
        // ====================================

        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
 
        private WebSocketServer _wsServer;

        private static AIAssistantManager _instance; // this manager is a singleton

        public static AIAssistantManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("AIAssistantManager is NULL");
                return _instance;
            }
        }

        // Awake is called on all GameObjects before Start
        void Awake()
        {
            // If another instance exists, destroy that game object. If no other game manager exists, 
            // initialize the instance to itself. As this manager needs to exist throughout all scenes, 
            // call the function DontDestroyOnLoad.
            if (_instance)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            DontDestroyOnLoad(this);
        }

        // Start is called before the first frame update if object is active
        void Start()
        {
            // Create a new WebSocket server that listens on localhost:8080
            _wsServer = new WebSocketServer("ws://localhost:8989");
            _wsServer.AddWebSocketService<WebSocketBehavior>("/geometry");
            _wsServer.Start();

            Debug.Log("WebSocket server started on ws://localhost:8989");
        }

        // Update is called once per frame
        void Update()
        {
            while (_executionQueue.Count > 0)
            {
                var action = _executionQueue.Dequeue();
                action.Invoke();
            }
        }

        public void Enqueue(Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        void OnApplicationQuit()
        {
            if (_wsServer != null)
            {
                _wsServer.Stop();
            }
        }    
    }

    public class WebSocketBehavior : WebSocketSharp.Server.WebSocketBehavior
    {
        // Delegate to create objects on the main thread
        private Action<string> onCommandReceived;

        protected override void OnOpen()
        {
            base.OnOpen();
            // Initialize the delegate to create objects on the main thread
            onCommandReceived = CreateObjectOnMainThread;
        }

        protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
        {
            string command = e.Data;  // The message (command) from the Python MCP server
            Debug.Log("Received command: " + command);

            // Invoke the object creation on the main thread
            AIAssistantManager.Instance.Enqueue(() => onCommandReceived(command));
        }

        // Main-thread-safe object creation
        private void CreateObjectOnMainThread(string command)
        {
            // Split the input string by commas, handling the quoted label
            string[] parts = command.Split(',');

            // Extract the object type
            string objectType = parts[0];

            // Parse position
            float posX = float.Parse(parts[1]);
            float posY = float.Parse(parts[2]);
            float posZ = float.Parse(parts[3]);
            Vector3 position = new Vector3(posX, posY, posZ);

            // Parse scale
            float scaleX = float.Parse(parts[4]);
            float scaleY = float.Parse(parts[5]);
            float scaleZ = float.Parse(parts[6]);
            Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);

            // Parse color
            float colorR = float.Parse(parts[7]);
            float colorG = float.Parse(parts[8]);
            float colorB = float.Parse(parts[9]);
            Color color = new Color(colorR, colorG, colorB);

            // Parse the label (it's the last part, which could have quotes)
            string label = parts[10].Trim('\"');

            // Instantiate the appropriate object based on the type
            GameObject newObject = null;

            switch (objectType.ToLower())
            {
                case "cube":
                    newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;

                case "sphere":
                    newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;

                case "capsule":
                    newObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;

                case "cylinder":
                    newObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;

                case "plane":
                    newObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    break;

                case "quad":
                    newObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    break;

                default:
                    Debug.LogError("Unknown object type: " + objectType);
                    return;
            }

            // Set parent
            newObject.transform.SetParent(AIAssistantManager.Instance.AIAssistantManagerObject.transform);

            // Set the position, scale, and color of the object
            newObject.transform.position = position;
            newObject.transform.localScale = scale;

            Renderer renderer = newObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            // Set the label (for example, as the object name or using UI text)
            newObject.name = label;

            // Log the object creation
            Debug.Log("Object created: " + newObject.name + " at " + newObject.transform.position); 
        }
    }
}
