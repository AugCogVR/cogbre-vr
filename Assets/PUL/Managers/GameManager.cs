using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.InputSystem;
using TMPro;
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

        public GameObject panelView;
        
        //required for DisplayTextOnPanelView, since this function is called in a loop. Really shady solution, but what can you do?
        HashSet<string> processedLines = new HashSet<string>();

        public GameObject transformationEdgePrefab;

        
        //string value refers to block ID. The List of GameObjects refers to all present in the path.
        public IDictionary<string, List<GameObject>> totalPaths = new Dictionary<string, List<GameObject>>(); 

        public CompVizStages cvs;

        public List<GameObject> totalNodesAcrossGraphs = new List<GameObject>();

        //refers to the list of current edges comprising a flow graph 
        public List<GameObject> currentFlowGraphEdges = new List<GameObject>();

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


        //works for the most part, but could potentially have some difficulty with edge case on B8? Everything else seems to be working, though.
        public void GenerateAllPaths(IList<IList<IList<string>>> TotalRelationSet)
        {
            
            
            //Iterate through the set of all possible relations.
            foreach(IList<IList<string>> curRelationSet in TotalRelationSet)
            {
                //for the values associated with stage 0 to be set as key values in totalPaths. Once incremented, this function will switch to
                //"adding mode." where it adds all the current functions associated with the block in stage 0.
                int stage = 0;
                //this facillitates "adding mode." the current stage 0 block id is stored here, and every subsequent re
                string curPathBlockID = null;
                //Iterate through All Relations.
                foreach (IList<string> blocksInCurRelationStage in curRelationSet)
                {
                      for(int i = 1; i < blocksInCurRelationStage.Count; i++)
                        {
                        if(stage  ==  0)
                        {
                            Debug.Log("CURRENT BLOCK TO BE ADDED " + blocksInCurRelationStage[i]);
                            curPathBlockID = blocksInCurRelationStage[i];
                            totalPaths[curPathBlockID] = new List<GameObject>();
                        }
                        else
                        {
                            GameObject curNode = null;
                            //if the stage is greater than 0, assume that it's been added, and generate the path with the analogous node GameObject
                            foreach (GameObject node in totalNodesAcrossGraphs)
                            {
                                if(node.GetComponent<Node>().nodeName == blocksInCurRelationStage[i])
                                {
                                     curNode = node;
                                    break;
                                }
                            }
                            if (curPathBlockID != null && curNode != null)
                            {
                                //totalPaths[curPathBlockID].Add(curNode);
                            }
                            else
                            {
                                Debug.LogError("WARNING: curPathBlock ID and or curNode is null! Check GenerateAllPaths()");
                            }
                        }
                    }
                      stage++;
                    }
                }    
            
        }   



        //places the graphHolders in the world.
        List<GameObject> instantiateGraphHolders(int graphTotal)
        {
            List<GameObject> graphList = new List<GameObject>();

            Vector3 graphPos = Vector3.zero;
            for (int i = 0; i < graphTotal; i++)
            {
                //Debug.Log("Running Graph Holder Instantiation!");
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
        void instantiateGraph2(ref List<GameObject> graphHolder)
        {
            Vector3 graphPos = Vector3.zero;
            foreach (GameObject graph in graphHolder)
            {
                //Debug.Log("Adding Graph!");
                codeGraph2 = graph.AddComponent<Graph>() as Graph;
            }

            if (cvs != null)
            {
                nexusClient.buildGraph2FromOxideBlocks(cvs, ref graphHolder);
                fillTotalNodesAcrossGraphs();
               // GenerateAllPaths(cvs.blockRelations);

                //Debug.LogWarning("TotalNodesAcrossGraphs " + graphHolder.Count);

            }

           else
            {
                Debug.LogWarning("Error! CVS is Empty!");
            }

            
        }

        void fillTotalNodesAcrossGraphs()
        {
            foreach(GameObject graph in graphHolder)
            {
                foreach(Transform node in graph.transform)
                {
                    totalNodesAcrossGraphs.Add(node.gameObject);
                }
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
        //displays text on the panelview slate. Works very similarly to DisplayTextOnNode in Node.cs. I need to outfit that script so it takes in buttons instead of text. might consolidate the two in the future.
        public void DisplayTextOnPanelView(GameObject PanelView, GameObject sourceCodeGraph)
        {
            int i = 0;
            int counter = 0;
            GameObject buttonPrefab = PanelView.transform.GetChild(2).gameObject;
         

            foreach (Transform childNode in sourceCodeGraph.transform)
            {
                foreach (string line in childNode.gameObject.GetComponent<Node>().nodeValue)
                {
                    //Debug.LogWarning("PROCESSING LINE" + " " + line);
                    if (processedLines.Contains(line))
                    {
                        continue; // Skip duplicate lines
                    }

                    if (i == 0)
                    {
                        PanelView.transform.GetChild(2).GetChild(3).gameObject.GetComponent<TextMeshPro>().text = line;
                        PanelView.transform.GetChild(2).GetComponent<SourceCodeLineController>().blockID = childNode.gameObject.GetComponent<Node>().nodeName;
                        i++;
                    }
                    else
                    {
                        GameObject newButtonObject = Instantiate(buttonPrefab, PanelView.transform);
                        newButtonObject.transform.GetChild(3).gameObject.GetComponent<TextMeshPro>().text = line;
                        newButtonObject.GetComponent <SourceCodeLineController>().blockID = childNode.gameObject.GetComponent<Node>().nodeName;

                        // Adjust position with vertical spacing
                        Vector3 newPosition = newButtonObject.transform.localPosition;
                        newPosition.y -= 0.025f * i;
                        newButtonObject.transform.localPosition = newPosition;
                        i++;
                    }
       

                    processedLines.Add(line); // Add the line to the processed set
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
                instantiateGraph2(ref graphHolder);
                // Graph has to be a component within a GameObject for StartCoroutine to work
            }
            

            // Update the graph
            //codeGraph1.OnUpdate();
        }

        //#endregion
    }
}
