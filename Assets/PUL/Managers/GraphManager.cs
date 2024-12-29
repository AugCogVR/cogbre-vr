using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

namespace PUL
{
    [System.Serializable]
    public class GraphManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor

        public float graphHandleScale = 1f;

        // END: These values are wired up in the Unity Editor
        // ====================================

        private static GraphManager _instance; // this manager is a singleton

        public static GraphManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("GraphManager is NULL");
                return _instance;
            }
        }

        // Keep track of all the active graphs
        IList<GameObject> graphList;


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

            graphList = new List<GameObject>();
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        // Build the "handle" object that the user can use to move the whole graph around
        GameObject buildGraphHandle(string labelText)
        {
            // Create the graph handle object at the spawn position and rotation
            GameObject graphHandlePrefab = Resources.Load("Prefabs/GraphHandle") as GameObject;
            GameObject graphHandle = Instantiate(graphHandlePrefab, GameManager.Instance.getSpawnPosition(), GameManager.Instance.getSpawnRotation());

            Debug.Log($"Graph at {GameManager.Instance.getSpawnPosition()}");

            // Set the label
            TextMeshPro nodeTitleTMP = graphHandle.transform.Find("TextBar/TextTMP").gameObject.GetComponent<TextMeshPro>();
            nodeTitleTMP.text = labelText;
            graphHandle.transform.SetParent(this.gameObject.transform, false);

            // Wire up graph close button
            GameObject closeButton = graphHandle.transform.Find("CloseGraphButton").gameObject;
            PressableButtonHoloLens2 buttonFunction = closeButton.GetComponent<PressableButtonHoloLens2>();
            buttonFunction.TouchBegin.AddListener(() => CloseGraphCallback(graphHandle));
            Interactable distanceInteract = closeButton.GetComponent<Interactable>();
            distanceInteract.OnClick.AddListener(() => CloseGraphCallback(graphHandle));

            return graphHandle;
        }

        // Close (destroy) a graph attached to the provided graphHandle
        public void CloseGraphCallback(GameObject graphHandle)
        {
            graphList.Remove(graphHandle);
            Destroy(graphHandle);
        }

        public void BuildBinaryCallGraph(OxideBinary binary)
        {
            // Build the "handle" object that the user can use to move the whole graph around
            GameObject graphHandle = buildGraphHandle($"Call Graph for {binary.name}");
            graphList.Add(graphHandle);

            // Hide the graph until it's done
            graphHandle.transform.position = new Vector3(graphHandle.transform.position.x,
                                                         graphHandle.transform.position.y - 100f,
                                                         graphHandle.transform.position.z);

            // Create a graph as a component of the graph handle and set its parent to the graph handle
            // HierarchicalGraph graph = graphHandle.AddComponent<HierarchicalGraph>();
            SugiyamaGraph graph = graphHandle.AddComponent<SugiyamaGraph>();

            // Track the nodes create for each function
            // TODO: Promote this to a class-level value or put in class-level data structure later
            Dictionary<OxideFunction, NodeInfo> functionNodeInfoDict = new Dictionary<OxideFunction, NodeInfo>();

            // Build the graph
            StartCoroutine(BuildBinaryCallGraphCoroutine(binary, graph, functionNodeInfoDict, graphHandle));
        }

        IEnumerator BuildBinaryCallGraphCoroutine(OxideBinary binary, BasicGraph graph, Dictionary<OxideFunction, NodeInfo> functionNodeInfoDict, GameObject graphHandle)
        {
            // Create and add all the nodes to the graph
            foreach (OxideFunction function in binary.functionDict.Values)
            {
                // Skip disconnected functions for now
                if ((function.sourceFunctionDict.Count == 0) && (function.targetFunctionDict.Count == 0)) continue;

                // Create graph node 
                GameObject graphNodePrefab = Resources.Load("Prefabs/FunctionNode") as GameObject;
                GameObject graphNode = Instantiate(graphNodePrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
                TextMeshPro nodeTitleTMP = graphNode.transform.Find("TextBar/TextTMP").gameObject.GetComponent<TextMeshPro>();
                nodeTitleTMP.text = function.name;
                graphNode.GetComponent<CapaFlags>().flags = function.capaList;
                if (function.capaList != null)
                { 
                    if (function.capaList.Count > 0)
                    {           
                        graphNode.GetComponent<CapaFlags>().functionCube.GetComponent<Renderer>().material.color = Color.gray;
                    }
                    graphNode.GetComponent<CapaFlags>().AssignFlagGameObject();
                }
                else
                {
                    Debug.Log($"BuildBinaryCallGraphCoroutine: capaList null!");
                }
                
                // Wire up selection button
                GameObject selectionButton = graphNode.transform.Find("FunctionSelectButton").gameObject;
                PressableButtonHoloLens2 buttonFunction = selectionButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchBegin.AddListener(() => MenuManager.Instance.FunctionButtonCallback(binary, function, null));
                Interactable distanceInteract = selectionButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => MenuManager.Instance.FunctionButtonCallback(binary, function, null));

                // TEST: This is not necessary but is a good test of attaching a behavior and 
                // is kind of fun. Recommend to comment it out!
                // gameObject.AddComponent<TwistyBehavior>();

                NodeInfo nodeInfo = graph.AddNodeToGraph(graphNode);
                
                functionNodeInfoDict[function] = nodeInfo;
                yield return new WaitForEndOfFrame();
            }

            // Add edges to the graph
            foreach (OxideFunction sourceFunction in binary.functionDict.Values)
            {
                if (functionNodeInfoDict.ContainsKey(sourceFunction))
                {
                    NodeInfo sourceNodeInfo = functionNodeInfoDict[sourceFunction];
                    foreach (OxideFunction targetFunction in sourceFunction.targetFunctionDict.Values)
                    {
                        if (functionNodeInfoDict.ContainsKey(targetFunction))
                        {
                            NodeInfo targetNodeInfo = functionNodeInfoDict[targetFunction];
                            graph.AddEdgeToGraph(sourceNodeInfo, targetNodeInfo);
                        }
                    }
                }
                yield return new WaitForEndOfFrame();
            }

            // Let the graph position the nodes
            graph.StartGraph(graphHandle);

            // Bring the graph back
            graphHandle.transform.position = new Vector3(graphHandle.transform.position.x,
                                                         graphHandle.transform.position.y + 100f,
                                                         graphHandle.transform.position.z);

            // Scale the graph
            graphHandle.transform.localScale = Vector3.one * graphHandleScale;

            // Assume this action to build the graph originated from a menu call,
            // so signal its completion. 
            MenuManager.Instance.unsetBusy();
        }

        public void BuildFunctionControlFlowGraph(OxideFunction function)
        {
            // Build the "handle" object that the user can use to move the whole graph around
            GameObject graphHandle = buildGraphHandle($"CFG for {function.parentBinary.name} / {function.name}");
            graphList.Add(graphHandle);

            // Hide the graph until it's done
            graphHandle.transform.position = new Vector3(graphHandle.transform.position.x,
                                                         graphHandle.transform.position.y - 100f,
                                                         graphHandle.transform.position.z);

            // Create a graph as a component of the graph handle and set its parent to the graph handle
            // HierarchicalGraph graph = graphHandle.AddComponent<HierarchicalGraph>();
            SugiyamaGraph graph = graphHandle.AddComponent<SugiyamaGraph>();

            // Track the nodes create for each basic block            
            // TODO: Promote this to a class-level value or put in class-level data structure later
            Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeInfoDict = new Dictionary<OxideBasicBlock, NodeInfo>();

            StartCoroutine(BuildFunctionControlFlowGraphCoroutine(function, graph, basicBlockNodeInfoDict, graphHandle));
        }

        IEnumerator BuildFunctionControlFlowGraphCoroutine(OxideFunction function, BasicGraph graph, Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeInfoDict, GameObject graphHandle)
        {
            // Create and add all the nodes to the graph
            foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
            {
                // Skip disconnected blocks for now
                if ((basicBlock.sourceBasicBlockDict.Count == 0) && (basicBlock.targetBasicBlockDict.Count == 0)) continue;

                GameObject graphNodePrefab = Resources.Load("Prefabs/BasicBlockNode") as GameObject;
                GameObject graphNode = Instantiate(graphNodePrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
                TextMeshPro nodeTitleTMP = graphNode.transform.Find("TitleBar/TitleTMP").gameObject.GetComponent<TextMeshPro>();
                nodeTitleTMP.text = basicBlock.offset;
                TextMeshPro nodeContentTMP = graphNode.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
                nodeContentTMP.text = "";
                foreach (OxideInstruction instruction in basicBlock.instructionDict.Values)
                {
                    nodeContentTMP.text += $"<color=#777777>{instruction.offset} <color=#99FF99>{instruction.mnemonic} <color=#FFFFFF>{instruction.op_str}\n";
                }
                NodeInfo nodeInfo = graph.AddNodeToGraph(graphNode);
                basicBlockNodeInfoDict[basicBlock] = nodeInfo;
                yield return new WaitForEndOfFrame();
            }

            // Add edges to the graph
            foreach (OxideBasicBlock sourceBasicBlock in function.basicBlockDict.Values)
            {
                if (basicBlockNodeInfoDict.ContainsKey(sourceBasicBlock))
                {
                    NodeInfo sourceNodeInfo = basicBlockNodeInfoDict[sourceBasicBlock];
                    foreach (OxideBasicBlock targetBasicBlock in sourceBasicBlock.targetBasicBlockDict.Values)
                    {
                        if (basicBlockNodeInfoDict.ContainsKey(targetBasicBlock))
                        {
                            NodeInfo targetNodeInfo = basicBlockNodeInfoDict[targetBasicBlock];
                            graph.AddEdgeToGraph(sourceNodeInfo, targetNodeInfo);
                        }
                    }
                }
                yield return new WaitForEndOfFrame();
            }

            // Let the graph position the nodes
            graph.StartGraph(graphHandle);

            // Bring the graph back
            graphHandle.transform.position = new Vector3(graphHandle.transform.position.x,
                                                         graphHandle.transform.position.y + 100f,
                                                         graphHandle.transform.position.z);

            // Scale the graph
            graphHandle.transform.localScale = Vector3.one * graphHandleScale;

            // Assume this action to build the graph originated from a menu call,
            // so signal its completion. 
            MenuManager.Instance.unsetBusy();
        }

        // The "FDG" version of the Control Flow Graph works, but isn't very useful--
        // highly recommend to continue using the Sugiyama version above-- 
        // but keep this code here to periodically test FDG to ensure we 
        // maintain it for future use. FDG will be good for visualization of 
        // relational but non-hierarchical data.
        public void BuildFunctionControlFlowGraphFDG(OxideFunction function)
        {
            // Build the "handle" object that the user can use to move the whole graph around
            GameObject graphHandle = buildGraphHandle($"CFG for {function.name}");
            graphList.Add(graphHandle);

            // Hide the graph until it's done
            graphHandle.transform.position = new Vector3(graphHandle.transform.position.x,
                                                         graphHandle.transform.position.y - 100f,
                                                         graphHandle.transform.position.z);

            // Create a graph as a component of the graph handle and set its parent to the graph handle
            ForceDirectedGraph graph = graphHandle.AddComponent<ForceDirectedGraph>();

            // Track the nodes create for each basic block            
            // TODO: Promote this to a class-level value or put in class-level data structure later
            Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeInfoDict = new Dictionary<OxideBasicBlock, NodeInfo>();

            StartCoroutine(BuildFunctionControlFlowGraphFDGCoroutine(function, graph, basicBlockNodeInfoDict, graphHandle));
        }

        IEnumerator BuildFunctionControlFlowGraphFDGCoroutine(OxideFunction function, ForceDirectedGraph graph, Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeInfoDict, GameObject graphHandle)
        {
            // Add basic block nodes by doing a breadth-first search with a queue
            Queue<(OxideBasicBlock, int)> basicBlocksToProcess = new Queue<(OxideBasicBlock, int)>();

            // Start by adding blocks that have no sources to the queue
            foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
                if ((basicBlock.sourceBasicBlockDict.Count == 0) && (basicBlock.targetBasicBlockDict.Count > 0))
                    basicBlocksToProcess.Enqueue((basicBlock, 0));

            // Until the queue is empty, add block nodes to the graph.
            while (basicBlocksToProcess.Count > 0)
            {
                (OxideBasicBlock sourceBasicBlock, int level) = basicBlocksToProcess.Dequeue();

                // Create the GameObject that visually represents this node
                GameObject graphNodePrefab = Resources.Load("Prefabs/BasicBlockNode") as GameObject;
                Vector3 position = new Vector3(Random.Range(-1.0f, 1.0f), 10.0f - (1.0f * level), Random.Range(-1.0f, 1.0f));
                GameObject gameObject = Instantiate(graphNodePrefab, position, Quaternion.identity);
                TextMeshPro nodeTitleTMP = gameObject.transform.Find("TitleBar/TitleTMP").gameObject.GetComponent<TextMeshPro>();
                nodeTitleTMP.text = sourceBasicBlock.offset;
                // -> World Space Text Mesh Pro
                TextMeshPro nodeContentTMP = gameObject.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
                nodeContentTMP.text = "";
                foreach (OxideInstruction instruction in sourceBasicBlock.instructionDict.Values)
                {
                    nodeContentTMP.text += $"<color=#777777>{instruction.offset} <color=#99FF99>{instruction.mnemonic} <color=#FFFFFF>{instruction.op_str}\n";
                }

                NodeInfo sourceNode = graph.AddNodeToGraph(gameObject);
                basicBlockNodeInfoDict[sourceBasicBlock] = sourceNode;

                foreach (OxideBasicBlock targetBasicBlock in sourceBasicBlock.targetBasicBlockDict.Values)
                {
                    if (basicBlockNodeInfoDict.ContainsKey(targetBasicBlock)) continue; // don't add nodes multiple times
                    if (!function.basicBlockDict.ContainsValue(targetBasicBlock)) continue; // don't add blocks outside this function
                    basicBlocksToProcess.Enqueue((targetBasicBlock, level + 1));
                }

                yield return new WaitForEndOfFrame();
            }

            // Add edges
            foreach (OxideBasicBlock sourceBasicBlock in basicBlockNodeInfoDict.Keys)
            {
                foreach (OxideBasicBlock targetBasicBlock in sourceBasicBlock.targetBasicBlockDict.Values)
                {
                    if (!function.basicBlockDict.ContainsValue(targetBasicBlock)) continue; // don't add blocks outside this function
                    // Debug.Log($"Add edge {sourceBasicBlock.offset} -> {targetBasicBlock.offset} to graph");
                    NodeInfo sourceNode = basicBlockNodeInfoDict[sourceBasicBlock];
                    NodeInfo targetNode = basicBlockNodeInfoDict[targetBasicBlock];
                    graph.AddEdgeToGraph(sourceNode, targetNode);
                }
                yield return new WaitForEndOfFrame();
            }

            // Let the graph position the nodes
            graph.StartGraph(graphHandle);

            // Bring the graph back
            graphHandle.transform.position = new Vector3(graphHandle.transform.position.x,
                                                         graphHandle.transform.position.y + 100f,
                                                         graphHandle.transform.position.z);

            // Scale the graph
            graphHandle.transform.localScale = Vector3.one * graphHandleScale;

            // Assume this action to build the graph originated from a menu call,
            // so signal its completion. 
            MenuManager.Instance.unsetBusy();

            //graph.RunForIterations(1);
            // STUPID HACK BECAUSE "RunForIterations" ISN'T WORKING YET
            for (int crap = 0; crap < 500; crap++) yield return new WaitForEndOfFrame();
            graph.StopGraph();
        }


        public string GetGraphTelemetryJSON()
        {
            string returnMe = "";

            if (graphList.Count > 0)
            {
                returnMe += $"[\"session_update\", \"objectTelemetry\"";

                foreach (GameObject obj in graphList)
                {
                    TextMeshPro titleTMP = obj.transform.Find("TextBar/TextTMP").gameObject.GetComponent<TextMeshPro>();
                    returnMe += $", \"graph-{titleTMP.text}\", ";
                    Vector3 pos = obj.transform.position;
                    returnMe += $"\"{pos.x}\", \"{pos.y}\", \"{pos.z}\", ";
                    Vector3 ori = obj.transform.eulerAngles;
                    returnMe += $"\"{ori.x}\", \"{ori.y}\", \"{ori.z}\"";
                }

                returnMe += "]";
                // Debug.Log("GRAPH TELEMETRY: " + returnMe);
            }

            return returnMe;
        }
    }
}
