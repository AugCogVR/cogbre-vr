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
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

namespace PUL
{
    [System.Serializable]
    public class AIAssistantManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Nexus Client object

        public GameObject AIAssistantManagerObject; 

        public GameObject VisHandlePrefab = null;

        public GameObject GraphEdgePrefab = null;

        public float visHandleScale = 1f;

        // END: These values are wired up in the Unity Editor -> Nexus Client object
        // ====================================

        // Keep track of all the active visualizations
        public int visCounter = 0;
        List<GameObject> visList;

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

            visList = new List<GameObject>();
        }

        // Start is called before the first frame update if object is active
        void Start()
        {
            // Create a new WebSocket server that listens on localhost:8080
            _wsServer = new WebSocketServer("ws://localhost:8989");
            _wsServer.AddWebSocketService<WebSocketBehavior>("/vis");
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

        class WebSocketBehavior : WebSocketSharp.Server.WebSocketBehavior
        {
            // Delegate to create objects on the main thread
            private Action<string> onMessageReceived;

            protected override void OnOpen()
            {
                base.OnOpen();
                // Initialize the delegate to create objects on the main thread
                onMessageReceived = AIAssistantManager.Instance.ProcessPayloadOnMainThread;
            }

            protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
            {
                string payload = e.Data;  // The message (command) from the Python MCP server
                Debug.Log("Received payload: " + payload);

                // Enqueue the object creation for later execution on the main thread
                AIAssistantManager.Instance.Enqueue(() => onMessageReceived(payload));
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
        public class VisData
        {
            public string payload_type;
            public string id;
            public List<Primitive> primitives;
        }

        [System.Serializable]
        public class Edge
        {
            public string source;
            public string target;
        }

        [System.Serializable]
        public class GraphData
        {
            public string payload_type;
            public string id;
            public List<Primitive> nodes;
            public List<Edge> edges;
        }

        // Method to process the payload of incoming messages. Since it will
        // likely create/update/destroy objects in the scene, it 
        // must execute within Unity's main thread (not directly called
        // from websocket OnMessage).
        public void ProcessPayloadOnMainThread(string payload)
        {
            Debug.Log($"Payload received: {payload}");

            // For now, wrap all the processing of messages that were
            // likely generated by an LLM into a giant try block.
            try
            {
                var firstProperty = JObject.Parse(payload).Properties().First();
                string payloadType = firstProperty.Value.ToString();
                Debug.Log($"Payload type: {payloadType}");

                if (payloadType == "vis") processVis(payload);

                if (payloadType == "graph") processGraph(payload);
            }
            catch (Exception e)
            {
                Debug.Log($"Exception processing payload: {e.Message}");
            }
        }

        private void processVis(string payload)
        {
            try
            {
                VisData visData = JsonConvert.DeserializeObject<VisData>(payload);

                Debug.Log($"Vis {visData.id} has {visData.primitives.Count} objects");

                GameObject visHandle = createVisHandle(visData.id);

                foreach (Primitive primitive in visData.primitives)
                {
                    createPrimitive(primitive, visHandle);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Exception processing vis: {e.Message}");
            }
        }

        private void processGraph(string payload)
        {
            try
            {
                GraphData graphData = JsonConvert.DeserializeObject<GraphData>(payload);

                Debug.Log($"Graph {graphData.id} has {graphData.nodes.Count} nodes");

                GameObject visHandle = createVisHandle(graphData.id);

                foreach (Primitive primitive in graphData.nodes)
                {
                    createPrimitive(primitive, visHandle);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Exception processing graph: {e.Message}");
            }
        }
        private GameObject createPrimitive(Primitive primitive, GameObject visHandle)
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
                    Debug.Log("Create primitive: Unknown object type: " + primitive.type);
                    return newObject;
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
            newObject.transform.parent = visHandle.transform;

            // Title Text
            // Add object to hold text 
            GameObject textHolder = new GameObject();
            textHolder.transform.parent = newObject.transform;
            // Create text mesh and attach to text holder object; position above cube
            TextMeshPro textObject = textHolder.AddComponent<TextMeshPro>();
            RectTransform rectTransform = textHolder.GetComponent<RectTransform>();
            rectTransform.localPosition = new Vector3(0, 1.0f, 0);
            // Set text contents and style
            textObject.font = Resources.Load("Fonts/LiberationSans", typeof(TMP_FontAsset)) as TMP_FontAsset;
            textObject.color = new Color(0,0,0,1.0f);
            textObject.text = primitive.name;
            textObject.fontSize = 1;  
            //textObject.autoSizeTextContainer = true;
            textObject.alignment = TextAlignmentOptions.Center;

            // Content Text
            // Add object to hold text 
            GameObject textHolder2 = new GameObject();
            textHolder2.transform.parent = newObject.transform;
            // Create text mesh and attach to text holder object; position outside cube
            TextMeshPro textObject2 = textHolder2.AddComponent<TextMeshPro>();
            RectTransform rectTransform2 = textHolder2.GetComponent<RectTransform>();
            rectTransform2.localPosition = new Vector3(0, 0, -1.0f);
            // Set text contents and style
            textObject2.font = Resources.Load("Fonts/LiberationSans", typeof(TMP_FontAsset)) as TMP_FontAsset;
            textObject2.color = new Color(0,0,0,1.0f);
            textObject2.text = $"some info about\n{primitive.name}";
            textObject2.fontSize = 0.25f;  
            textObject2.alignment = TextAlignmentOptions.Center;

            Debug.Log("Object created: " + newObject.name + " at " + newObject.transform.position); 
            return newObject;
        }

        private GameObject createVisHandle(string label)
        {
            GameObject visHandle = Instantiate(VisHandlePrefab, GameManager.Instance.getSpawnPosition(), GameManager.Instance.getSpawnRotation());
            visHandle.transform.rotation = Quaternion.LookRotation(visHandle.transform.position - Camera.main.transform.position);
            visCounter++;
            visHandle.name = $"vis{visCounter}";

            Debug.Log($"VisHandle {label} created at {GameManager.Instance.getSpawnPosition()}");

            // Set the label
            TextMeshPro nodeTitleTMP = visHandle.transform.Find("TextBar/TextTMP").gameObject.GetComponent<TextMeshPro>();
            nodeTitleTMP.text = label;

            // Wire up graph close button
            GameObject closeButton = visHandle.transform.Find("CloseGraphButton").gameObject;
            PressableButtonHoloLens2 buttonFunction = closeButton.GetComponent<PressableButtonHoloLens2>();
            buttonFunction.TouchBegin.AddListener(() => DestroyVisCallback(visHandle));
            Interactable distanceInteract = closeButton.GetComponent<Interactable>();
            distanceInteract.OnClick.AddListener(() => DestroyVisCallback(visHandle));

            visList.Add(visHandle);

            return visHandle;
        }

        // Destroy a visualizatiobn attached to the provided visHandle
        public void DestroyVisCallback(GameObject visHandle)
        {
            // Remove from vis list.
            visList.Remove(visHandle);

            // Destroy the game object.
            Destroy(visHandle);
        }
    }
}

