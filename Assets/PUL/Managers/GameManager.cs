using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem.Utilities;

namespace PUL
{
    public class GameManager : MonoBehaviour
    {
        //#region Singleton

        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("GameManager is NULL");
                }
                
                return _instance;
            }
        }


        public List<GameObject> graphHolder = new List<GameObject>();

        // TODO: We need to support more than a single graph of a single type, obviously, but we're here for now.
        //public UhGraph codeGraph;
        public RandomStartForceDirectedGraph codeGraph1;

        public Graph codeGraph2;

        public CompVizStages cvs;

        public InputAction rightHandRotateAnchor = null;

        public NexusClient nexusClient;

        public int graphTotal = 0;

        bool isFinished = false;

        private void Awake()
        {
            // https://bergstrand-niklas.medium.com/setting-up-a-simple-game-manager-in-unity-24b080e9516c
            // If another game manager exists, destroy that game object. If no other game manager exists, 
            // initialize the instance to itself. As a game manager needs to exist throughout all scenes, 
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



            // https://forum.unity.com/threads/xr-device-simulator-wasd-inputs-ignored.1346477/
            // InputSystem.settings.SetInternalFeatureFlag("DISABLE_SHORTCUT_SUPPORT", true);
        }

        //places the graphHolders in the world.
        List<GameObject> instantiateGraphHolders(int graphTotal)
        {
            List<GameObject> graphList = new List<GameObject>();

            Vector3 graphPos = Vector3.zero;
            for (int i = 0; i < graphTotal; i++)
            {
                Debug.Log("Running Graph Holder Instantiation!");
                GameObject newGraph = new GameObject("graphHolder");
                newGraph.transform.position = graphPos;
                newGraph.transform.rotation = Quaternion.identity;
                graphPos.x += 5;
                graphList.Add(newGraph);
            }
            return graphList;
        }
        //instantiates the random stat force directed graph
        void instantiateGraph1()
        {
            Vector3 graphPos = Vector3.zero;
            foreach (GameObject graph in graphHolder)
            {
                codeGraph1 = graph.AddComponent<RandomStartForceDirectedGraph>() as RandomStartForceDirectedGraph;
                codeGraph1.transform.localPosition = graphPos;
                graphPos.x += 5;
                codeGraph1.transform.localEulerAngles = Vector3.zero;
            }
            //use nexus to build this graph later on.
        }

        //instantiates the 2D Node graph.
        void instantiateGraph2(List<GameObject> graphHolder)
        {
            Vector3 graphPos = Vector3.zero;
            foreach (GameObject graph in graphHolder)
            {
                Debug.Log("Adding Graph!");
                codeGraph2 = graph.AddComponent<Graph>() as Graph;
            }

            if (cvs != null)
            {
                nexusClient.buildGraph2FromOxideBlocks(cvs, ref graphHolder);
            }

           else
            {
                Debug.LogWarning("Error! CVS is Empty!");
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // Initialize Nexus client
            nexusClient = new NexusClient(this);


            nexusClient.NexusSessionInit();
          //  graphTotal = cvs.stages.Count;
            //Debug.Log("GameManager START");

            // Initialize scene objects

            // TODO: This probably needs to be a class member
            

            // codeGraph = graphHolder.AddComponent<UhGraph>() as UhGraph;

            //instantiateGraph1();

            //instantiateGraph2();
            //findRightHandAnchor();

            
        }
        void findRightHandAnchor()
        {
            // Find rightHandRotateAnchor input action
            // TODO: Find a direct route to the action instead of this tedious drill-down
            // TODO: Learn the Unity Action-based input system better... I guess
            InputActionManager iam = FindObjectOfType<InputActionManager>();
            List<InputActionAsset> actionAssets = iam.actionAssets;
            foreach (InputActionAsset iaa in actionAssets)
            {
                //Debug.Log("ASSET: " + iaa.name);
                ReadOnlyArray<InputActionMap> actionMaps = iaa.actionMaps;
                foreach (InputActionMap iamap in actionMaps)
                {
                    //Debug.Log("  MAP: " + iamap.name);
                    ReadOnlyArray<InputAction> actions = iamap.actions;
                    foreach (InputAction ia in actions)
                    {
                        //Debug.Log("    ACTION: " + ia.name);
                        if (iamap.name == "XRI RightHand Interaction" && ia.name == "Rotate Anchor")
                        {
                            rightHandRotateAnchor = ia;
                        }
                    }
                }
            }
        }

        void rotateGraph()
        {
            foreach (GameObject graph in graphHolder)
            {
                // Rotate the code graph based on user input
                Vector2 v = rightHandRotateAnchor.ReadValue<Vector2>();
                //Debug.Log(v.x);
                if (Math.Abs(v.x) > 0.7f)
                {
                    graph.transform.localEulerAngles = graph.transform.localEulerAngles + new Vector3(0, v.x * -0.5f, 0);
                }
                if (Math.Abs(v.y) > 0.7f)
                {
                    graph.transform.localPosition = graph.transform.localPosition + new Vector3(0, v.y * 10.0f, 0);
                }

            }
        }

        // Update is called once per frame
        void Update()
        {
           
            // Sync with Nexus
            nexusClient.OnUpdate();

            if (graphTotal != 0 && !isFinished)
            {
                isFinished = true;
              
                graphHolder = instantiateGraphHolders(graphTotal);
                //Debug.Log(graphHolder[0]);
                instantiateGraph2(graphHolder);
                // Graph has to be a component within a GameObject for StartCoroutine to work
            }
            

            // Update the graph
            //codeGraph1.OnUpdate();
        }

        //#endregion
    }
}
