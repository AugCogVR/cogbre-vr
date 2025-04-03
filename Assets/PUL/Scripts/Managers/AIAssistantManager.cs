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

        // We'll receive commands from the websocket. It runs in its own thread.
        // That thread can't create GameObjects; only the Unity main thread can.
        // So we create a queue of actions that the socket will add to, and 
        // we'll check it and execute those actions during Update().
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
            // Check the action queue and execute every pending action.
            while (_executionQueue.Count > 0)
            {
                var action = _executionQueue.Dequeue();
                action.Invoke();
            }
        }

        // Add an action to the action queue. 
        public void Enqueue(Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        // Stop the websocket server when the app quits. 
        void OnApplicationQuit()
        {
            if (_wsServer != null)
            {
                _wsServer.Stop();
            }
        }    
    }

    [System.Serializable]
    public class ColorRGBA
    {
        public float r, g, b, a;
    }

    [System.Serializable]
    public class Vector3D
    {
        public float x, y, z;
    }

    [System.Serializable]
    public class Primitive
    {
        public string type;
        public Vector3D position;
        public Vector3D rotation;
        public Vector3D scale;
        public ColorRGBA color;
        public string name;
    }

    [System.Serializable]
    public class SceneData
    {
        public List<Primitive> primitives;
    }

    public class WebSocketBehavior : WebSocketSharp.Server.WebSocketBehavior
    {
        // Delegate to create objects on the main thread
        private Action<string> onMessageReceived;

        protected override void OnOpen()
        {
            base.OnOpen();
            // Initialize the delegate to create objects on the main thread
            onMessageReceived = ProcessPayloadOnMainThread;
        }

        protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
        {
            string payload = e.Data;  // The message (command) from the Python MCP server
            Debug.Log("Received payload: " + payload);

            // Enqueue the object creation for later execution on the main thread
            AIAssistantManager.Instance.Enqueue(() => onMessageReceived(payload));
        }

        // Method to instantiate objects in the environment.
        // Must execute within Unity's main thread (not directly called
        // from websocket OnMessage).
        private void ProcessPayloadOnMainThread(string payload)
        {
            Debug.Log($"Payload received: {payload}");

            SceneData sceneData = JsonConvert.DeserializeObject<SceneData>(payload);

            Debug.Log($"Payload scene has {sceneData.primitives.Count} objects");

            foreach (var primitive in sceneData.primitives)
            {
                GameObject newObject = null;

                switch (primitive.type.ToLower())
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
                        Debug.LogError("Unknown object type: " + primitive.type);
                        return;
                }

                newObject.transform.position = new Vector3(primitive.position.x, primitive.position.y, primitive.position.z);
                newObject.transform.rotation = Quaternion.Euler(primitive.rotation.x, primitive.rotation.y, primitive.rotation.z);
                newObject.transform.localScale = new Vector3(primitive.scale.x, primitive.scale.y, primitive.scale.z);
                Renderer renderer = newObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = new Color(primitive.color.r, primitive.color.g, primitive.color.b, primitive.color.a);
                    renderer.material.color = color;
                }            
                newObject.name = primitive.name;

                Debug.Log("Object created: " + newObject.name + " at " + newObject.transform.position); 
            }
        }
    }
}

