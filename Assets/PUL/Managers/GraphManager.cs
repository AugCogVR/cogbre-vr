using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace PUL
{
    [System.Serializable]
    public class GraphManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Graph Manager object

        public GameManager gameManager;

        // END: These values are wired up in the Unity Editor -> Menu Manager object
        // ====================================

        IList<Component> graphList;


        void Awake()
        {
        }

        // Start is called before the first frame update
        void Start()
        {
            graphList = new List<Component>();
        }

        // Update is called once per frame
        void Update()
        {
        }

        // Build the "handle" object that the user can use to move the whole graph around
        GameObject buildGraphHandle(string labelText)
        {
            GameObject graphHandlePrefab = Resources.Load("Prefabs/GraphHandle") as GameObject;
            Vector3 position = new Vector3(0.82f, 0, 0.77f);
            GameObject graphHandle = Instantiate(graphHandlePrefab, position, Quaternion.identity);
            TextMeshPro nodeTitleTMP = graphHandle.transform.Find("TextBar/TextTMP").gameObject.GetComponent<TextMeshPro>();
            nodeTitleTMP.text = labelText;
            graphHandle.transform.SetParent(this.gameObject.transform, false);
            return graphHandle;
        }

        public void BuildBinaryCallGraph(OxideBinary binary)
        {
            // Build the "handle" object that the user can use to move the whole graph around
            GameObject graphHandle = buildGraphHandle($"Call Graph for {binary.name}");

            // Create a graph as a component of the graph handle, add it to our list, and set its parent to the graph handle
            HierarchicalGraph graph = graphHandle.AddComponent<HierarchicalGraph>();
            graphList.Add(graph);

            // Track the nodes create for each function
            // TODO: Promote this to a class-level value or put in class-level data structure later
            Dictionary<OxideFunction, NodeInfo> functionNodeInfoDict = new Dictionary<OxideFunction, NodeInfo>();

            // Build the graph
            StartCoroutine(BuildBinaryCallGraphCoroutine(binary, graph, functionNodeInfoDict));
        }

        IEnumerator BuildBinaryCallGraphCoroutine(OxideBinary binary, BasicGraph graph, Dictionary<OxideFunction, NodeInfo> functionNodeInfoDict)
        {
            // Create and add all the nodes to the graph
            foreach (OxideFunction function in binary.functionDict.Values)
            {
                // Skip disconnected functions for now
                if ((function.sourceFunctionDict.Count == 0) && (function.targetFunctionDict.Count == 0)) continue;

                GameObject graphNodePrefab = Resources.Load("Prefabs/FunctionNode") as GameObject;
                GameObject gameObject = Instantiate(graphNodePrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
                TextMeshPro nodeTitleTMP = gameObject.transform.Find("TextBar/TextTMP").gameObject.GetComponent<TextMeshPro>();
                nodeTitleTMP.text = function.name;
                // TEST: This is not necessary but is a good test of attaching a behavior and 
                // is kind of fun. Recommend to comment it out!
                // gameObject.AddComponent<TwistyBehavior>();
                NodeInfo nodeInfo = graph.AddNodeToGraph(gameObject);
                functionNodeInfoDict[function] = nodeInfo;
                yield return new WaitForEndOfFrame();
            }

            // Set source/target relationships in graph nodes and add edges
            foreach (OxideFunction function in binary.functionDict.Values)
            {
                if (functionNodeInfoDict.ContainsKey(function))
                {
                    NodeInfo nodeInfo = functionNodeInfoDict[function];
                    foreach (OxideFunction sourceFunction in function.sourceFunctionDict.Values)
                        nodeInfo.sourceNodeInfos.Add(functionNodeInfoDict[sourceFunction]);
                    foreach (OxideFunction targetFunction in function.targetFunctionDict.Values)
                    {
                        nodeInfo.targetNodeInfos.Add(functionNodeInfoDict[targetFunction]);
                        graph.AddEdgeToGraph(nodeInfo, functionNodeInfoDict[targetFunction]);
                    }
                }
                yield return new WaitForEndOfFrame();
            }

            // Let the graph position the nodes
            graph.StartGraph();
        }

        public void BuildFunctionControlFlowGraph(OxideFunction function)
        {
            // Build the "handle" object that the user can use to move the whole graph around
            GameObject graphHandle = buildGraphHandle($"CFG for {function.name}");

            // Create a graph as a component of the graph handle, add it to our list, and set its parent to the graph handle
            HierarchicalGraph graph = graphHandle.AddComponent<HierarchicalGraph>();
            graphList.Add(graph);

            // Track the nodes create for each basic block            
            // TODO: Promote this to a class-level value or put in class-level data structure later
            Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeInfoDict = new Dictionary<OxideBasicBlock, NodeInfo>();

            StartCoroutine(BuildFunctionControlFlowGraphCoroutine(function, graph, basicBlockNodeInfoDict));
        }

        IEnumerator BuildFunctionControlFlowGraphCoroutine(OxideFunction function, HierarchicalGraph graph, Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeInfoDict)
        {
            // Create and add all the nodes to the graph
            foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
            {
                // Skip disconnected blocks for now
                if ((basicBlock.sourceBasicBlockDict.Count == 0) && (basicBlock.targetBasicBlockDict.Count == 0)) continue;

                GameObject graphNodePrefab = Resources.Load("Prefabs/BasicBlockNode") as GameObject;
                GameObject gameObject = Instantiate(graphNodePrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
                TextMeshPro nodeTitleTMP = gameObject.transform.Find("TitleBar/TitleTMP").gameObject.GetComponent<TextMeshPro>();
                nodeTitleTMP.text = basicBlock.offset;
                TextMeshPro nodeContentTMP = gameObject.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
                nodeContentTMP.text = "";
                foreach (OxideInstruction instruction in basicBlock.instructionDict.Values)
                {
                    nodeContentTMP.text += $"<color=#777777>{instruction.offset} <color=#99FF99>{instruction.mnemonic} <color=#FFFFFF>{instruction.op_str}\n";
                }
                NodeInfo nodeInfo = graph.AddNodeToGraph(gameObject);
                basicBlockNodeInfoDict[basicBlock] = nodeInfo;
                yield return new WaitForEndOfFrame();
            }

            // Set source/target relationships in graph nodes and add edges
            foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
            {
                if (basicBlockNodeInfoDict.ContainsKey(basicBlock))
                {
                    NodeInfo nodeInfo = basicBlockNodeInfoDict[basicBlock];
                    foreach (OxideBasicBlock sourceBasicBlock in basicBlock.sourceBasicBlockDict.Values)
                    {
                        if (basicBlockNodeInfoDict.ContainsKey(sourceBasicBlock)) // TODO: Why do I need to do this check? Why is a nodeInfo missing for a basicBlock that has sources???
                        {
                            nodeInfo.sourceNodeInfos.Add(basicBlockNodeInfoDict[sourceBasicBlock]);
                        }
                    }
                    foreach (OxideBasicBlock targetBasicBlock in basicBlock.targetBasicBlockDict.Values)
                    {
                        if (basicBlockNodeInfoDict.ContainsKey(targetBasicBlock)) // TODO: Why do I need to do this check? Why is a nodeInfo missing for a basicBlock that has targets???
                        {
                            nodeInfo.targetNodeInfos.Add(basicBlockNodeInfoDict[targetBasicBlock]);
                            graph.AddEdgeToGraph(nodeInfo, basicBlockNodeInfoDict[targetBasicBlock]);
                        }
                    }
                }
                yield return new WaitForEndOfFrame();
            }

            // Let the graph position the nodes
            graph.StartGraph();
        }


        // Save the "FDG" versions of these functions for reference; not using
        // force-directed graph at this time but code will be handy in the future
        public void BuildFunctionControlFlowGraphFDG(OxideFunction function)
        {
            // Build the "handle" object that the user can use to move the whole graph around
            GameObject graphHandle = buildGraphHandle($"CFG for {function.name}");

            // Create a graph as a component of the graph handle, add it to our list, and set its parent to the graph handle
            ForceDirectedGraph graph = graphHandle.AddComponent<ForceDirectedGraph>();
            graphList.Add(graph);

            // Track the nodes create for each basic block            
            // TODO: Promote this to a class-level value or put in class-level data structure later
            Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeInfoDict = new Dictionary<OxideBasicBlock, NodeInfo>();

            StartCoroutine(BuildFunctionControlFlowGraphFDGCoroutine(function, graph, basicBlockNodeInfoDict));
        }

        IEnumerator BuildFunctionControlFlowGraphFDGCoroutine(OxideFunction function, ForceDirectedGraph graph, Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeInfoDict)
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

            graph.StartGraph();
            //graph.RunForIterations(1);
            // STUPID HACK BECAUSE "RunForIterations" ISN'T WORKING YET
            for (int crap = 0; crap < 500; crap++) yield return new WaitForEndOfFrame();
            graph.StopGraph();
        }
    }
}
