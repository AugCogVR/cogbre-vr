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


        public GameObject graphHolder;

        // TODO: We need to support more than a single graph of a single type, obviously, but we're here for now.
        //public UhGraph codeGraph;
        public RandomStartForceDirectedGraph codeGraph;

        public InputAction rightHandRotateAnchor = null;

        public NexusClient nexusClient;

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

        // Start is called before the first frame update
        void Start()
        {
            //Debug.Log("GameManager START");

            // Initialize scene objects

            // TODO: This probably needs to be a class member
            graphHolder = new GameObject("graphHolder"); // Graph has to be a component within a GameObject for StartCoroutine to work
            graphHolder.transform.position = Vector3.zero;
            graphHolder.transform.eulerAngles = Vector3.zero;

            // codeGraph = graphHolder.AddComponent<UhGraph>() as UhGraph;
            codeGraph = graphHolder.AddComponent<RandomStartForceDirectedGraph>() as RandomStartForceDirectedGraph;
            codeGraph.transform.localPosition = Vector3.zero;
            codeGraph.transform.localEulerAngles = Vector3.zero;


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


            // Initialize Nexus client
            nexusClient = new NexusClient(this);
        }

        // Update is called once per frame
        void Update()
        {
            // Sync with Nexus
            nexusClient.OnUpdate();

            // Rotate the code graph based on user input
            Vector2 v = rightHandRotateAnchor.ReadValue<Vector2>();
            //Debug.Log(v.x);
            if (Math.Abs(v.x) > 0.7f)
            {   
                graphHolder.transform.localEulerAngles = graphHolder.transform.localEulerAngles + new Vector3(0, v.x * -0.5f, 0);
            }
            if (Math.Abs(v.y) > 0.7f)
            {   
                graphHolder.transform.localPosition = graphHolder.transform.localPosition + new Vector3(0, v.y * 10.0f, 0);
            }

            // Update the graph
            codeGraph.OnUpdate();
        }

        //#endregion
    }
}
