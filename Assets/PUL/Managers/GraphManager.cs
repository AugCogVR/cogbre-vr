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

            // Track what functions are associated with what graph nodes
            Dictionary<OxideFunction, NodeInfo> functionNodeDict = new Dictionary<OxideFunction, NodeInfo>(); 
            // TODO: Promote this to a class-level value or put in class-level data structure later

            // Build the graph
            StartCoroutine(BuildBinaryCallGraphCoroutine(binary, graph, functionNodeDict));
        }

        IEnumerator BuildBinaryCallGraphCoroutine(OxideBinary binary, BasicGraph graph, Dictionary<OxideFunction, NodeInfo> functionNodeDict)
        {
            // Build a hierarchical graph
            // As we process the graph nodes, collect what nodes are at what levels
            Dictionary<int, IList<GameObject>> layout = new Dictionary<int, IList<GameObject>>();

            // Add function nodes by doing a breadth-first search with a queue
            Queue<(OxideFunction, int)> functionsToProcess = new Queue<(OxideFunction, int)>();

            // Start by adding functions that have no sources to the queue
            foreach (OxideFunction function in binary.functionDict.Values)
                if ((function.sourceFunctionDict.Count == 0) && (function.targetFunctionDict.Count > 0))
                    functionsToProcess.Enqueue((function, 0));

            // Until the queue is empty, add function nodes to the graph.
            while (functionsToProcess.Count > 0)
            {
                (OxideFunction sourceFunction, int level) = functionsToProcess.Dequeue();

                // Create the GameObject that visually represents this node
                GameObject graphNodePrefab = Resources.Load("Prefabs/FunctionNode") as GameObject;
                GameObject gameObject = Instantiate(graphNodePrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
                TextMeshPro nodeTitleTMP = gameObject.transform.Find("TextBar/TextTMP").gameObject.GetComponent<TextMeshPro>();
                nodeTitleTMP.text = sourceFunction.name;

                // TEST: This is not necessary but is a good test of attaching a behavior and 
                // is kind of fun. Recommend to comment it out!
                // gameObject.AddComponent<TwistyBehavior>();

                NodeInfo sourceNode = graph.AddNodeToGraph(gameObject);
                functionNodeDict[sourceFunction] = sourceNode;

                if (!layout.ContainsKey(level)) layout[level] = new List<GameObject>();
                layout[level].Add(gameObject);

                foreach (OxideFunction targetFunction in sourceFunction.targetFunctionDict.Values)
                {
                    if (functionNodeDict.ContainsKey(targetFunction)) continue; // don't add nodes multiple times
                    functionsToProcess.Enqueue((targetFunction, level + 1));
                }

                yield return new WaitForEndOfFrame(); 
            }

            // Reposition nodes per the collected layout
            float xOffset = 0.25f;
            float yOffset = -0.25f;
            float xMultiplier = 0.5f;
            float yMultiplier = -0.33f;
            // float top = layout.Keys.Count * yBuffer;
            foreach (int level in layout.Keys)
            {
                float y = (level * yMultiplier) + yOffset;
                int xCount = 0;
                foreach (GameObject gameObject in layout[level])
                {
                    float x = (xMultiplier * xCount) + xOffset;
                    gameObject.transform.localPosition = new Vector3(x, y, 0);
                    xCount++;
                }
                yield return new WaitForEndOfFrame(); 
            }

            // Add edges
            foreach (OxideFunction sourceFunction in functionNodeDict.Keys)
            {
                foreach (OxideFunction targetFunction in sourceFunction.targetFunctionDict.Values)
                {
                    // Debug.Log($"Add edge {sourceFunction.name} -> {targetFunction.name} to graph");
                    NodeInfo sourceNode = functionNodeDict[sourceFunction];
                    NodeInfo targetNode = functionNodeDict[targetFunction];
                    graph.AddEdgeToGraph(sourceNode, targetNode);
                }
                yield return new WaitForEndOfFrame(); 
            }
        }

        public void BuildFunctionControlFlowGraph(OxideFunction function)
        {
            // Build the "handle" object that the user can use to move the whole graph around
            GameObject graphHandle = buildGraphHandle($"CFG for {function.name}");

            // Create a graph as a component of the graph handle, add it to our list, and set its parent to the graph handle
            ForceDirectedGraph graph = graphHandle.AddComponent<ForceDirectedGraph>();
            graphList.Add(graph);
            
            Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeDict = new Dictionary<OxideBasicBlock, NodeInfo>(); 
            // TODO: Promote this to a class-level value or put in class-level data structure later

            StartCoroutine(BuildFunctionControlFlowGraphCoroutine(function, graph, basicBlockNodeDict));
        }

        IEnumerator BuildFunctionControlFlowGraphCoroutine(OxideFunction function, ForceDirectedGraph graph, Dictionary<OxideBasicBlock, NodeInfo> basicBlockNodeDict)
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

                // This is not necessary but is a good test and kind of fun. Comment it out!
                // gameObject.AddComponent<TwistyBehavior>();

                NodeInfo sourceNode = graph.AddNodeToGraph(gameObject);
                basicBlockNodeDict[sourceBasicBlock] = sourceNode;

                foreach (OxideBasicBlock targetBasicBlock in sourceBasicBlock.targetBasicBlockDict.Values)
                {
                    if (basicBlockNodeDict.ContainsKey(targetBasicBlock)) continue; // don't add nodes multiple times
                    if (!function.basicBlockDict.ContainsValue(targetBasicBlock)) continue; // don't add blocks outside this function
                    basicBlocksToProcess.Enqueue((targetBasicBlock, level + 1));
                }

                yield return new WaitForEndOfFrame(); 
            }

            // Add edges
            foreach (OxideBasicBlock sourceBasicBlock in basicBlockNodeDict.Keys)
            {
                foreach (OxideBasicBlock targetBasicBlock in sourceBasicBlock.targetBasicBlockDict.Values)
                {
                    if (!function.basicBlockDict.ContainsValue(targetBasicBlock)) continue; // don't add blocks outside this function
                    // Debug.Log($"Add edge {sourceBasicBlock.offset} -> {targetBasicBlock.offset} to graph");
                    NodeInfo sourceNode = basicBlockNodeDict[sourceBasicBlock];
                    NodeInfo targetNode = basicBlockNodeDict[targetBasicBlock];
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
