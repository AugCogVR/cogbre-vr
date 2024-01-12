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

        public void BuildBinaryCallGraph(OxideBinary binary)
        {
            ForceDirectedGraph graph = gameObject.AddComponent<ForceDirectedGraph>();
            graphList.Add(graph);
            
            Dictionary<OxideFunction, NodeInfo> functionNodeDict = new Dictionary<OxideFunction, NodeInfo>(); 
            // TODO: Promote this to a class-level value or put in class-level data structure later

            StartCoroutine(BuildBinaryCallGraphCoroutine(binary, graph, functionNodeDict));
        }

        IEnumerator BuildBinaryCallGraphCoroutine(OxideBinary binary, ForceDirectedGraph graph, Dictionary<OxideFunction, NodeInfo> functionNodeDict)
        {
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
                Vector3 position = new Vector3(Random.Range(-1.0f, 1.0f), 4.0f - (0.5f * level), 2.0f);
                GameObject gameObject = Instantiate(graphNodePrefab, position, Quaternion.identity);
                TextMeshPro nodeTitleTMP = gameObject.transform.Find("TitleBar/TitleTMP").gameObject.GetComponent<TextMeshPro>();
                nodeTitleTMP.text = sourceFunction.name;
                TextMeshPro nodeContentTMP = gameObject.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
                nodeContentTMP.text = sourceFunction.signature;

                // This is not necessary but is a good test and kind of fun. Comment it out!
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
            float xBuffer = 0.5f;
            float yBuffer = 0.33f;
            float top = layout.Keys.Count * yBuffer;
            foreach (int level in layout.Keys)
            {
                float y = top - (level * yBuffer);
                int xCount = 0;
                foreach (GameObject gameObject in layout[level])
                {
                    float x = xBuffer * xCount;
                    gameObject.transform.position = new Vector3(x, y, 2.0f);
                    xCount++;
                }
                yield return new WaitForEndOfFrame(); 
            }

            // ALTERNATIVE: Just add all functions indiscriminately, starting in completely random positions. 
            // foreach (OxideFunction function in binary.functionDict.Values)
            // {
            //     // Debug.Log($"Add node {function.name} to graph");
            //     Vector3 position = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(2.0f, 5.0f), Random.Range(-5.0f, 5.0f));
            //     NodeInfo nodeInfo = graph.AddNodeToGraph(position, function.name, function.signature);
            //     functionNodeDict[function] = nodeInfo;
            //     yield return new WaitForEndOfFrame(); 
            // }

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

            graph.StartGraph();
            // graph.RunForIterations(2000);
            // STUPID HACK BECAUSE "RunForIterations" ISN'T WORKING YET
            for (int crap = 0; crap < 500; crap++) yield return new WaitForEndOfFrame(); 
            graph.StopGraph();
        }

        public void BuildFunctionControlFlowGraph(OxideFunction function)
        {
            ForceDirectedGraph graph = gameObject.AddComponent<ForceDirectedGraph>();
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
