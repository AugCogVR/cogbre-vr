//using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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

            StartCoroutine(BuildBinaryCallGraphCoroutine(binary, graph, functionNodeDict));
        }

        IEnumerator BuildBinaryCallGraphCoroutine(OxideBinary binary, ForceDirectedGraph graph, Dictionary<OxideFunction, NodeInfo> functionNodeDict)
        {
            OxideFunction entryFunction = null;
            foreach (OxideFunction function in binary.functionDict.Values)
            {
                if (function.name == "entry") 
                {
                    entryFunction = function; 
                    break;
                }
            }

            if (entryFunction != null)
            {
                Queue<(OxideFunction, int)> functionsToProcess = new Queue<(OxideFunction, int)>();
                functionsToProcess.Enqueue((entryFunction, 0));

                while (functionsToProcess.Count > 0)
                {
                    (OxideFunction sourceFunction, int level) = functionsToProcess.Dequeue();

                    Vector3 position = new Vector3(Random.Range(-1.0f, 1.0f), 10.0f - (1.0f * level), Random.Range(-1.0f, 1.0f));
                    NodeInfo sourceNode = graph.AddNodeToGraph(position, sourceFunction.name, sourceFunction.signature);
                    functionNodeDict[sourceFunction] = sourceNode;

                    foreach (OxideFunction targetFunction in sourceFunction.calledFunctionsDict.Values)
                    {
                        if (functionNodeDict.ContainsKey(targetFunction)) continue;
                        functionsToProcess.Enqueue((targetFunction, level + 1));
                    }

                    yield return new WaitForEndOfFrame(); 
                }
            }

            // foreach (OxideFunction function in binary.functionDict.Values)
            // {
            //     // Debug.Log($"Add node {function.name} to graph");
            //     Vector3 position = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(2.0f, 5.0f), Random.Range(-5.0f, 5.0f));
            //     NodeInfo nodeInfo = graph.AddNodeToGraph(position, function.name, function.signature);
            //     functionNodeDict[function] = nodeInfo;
            //     yield return new WaitForEndOfFrame(); 
            // }

            foreach (OxideFunction sourceFunction in functionNodeDict.Keys)
            {
                foreach (OxideFunction targetFunction in sourceFunction.calledFunctionsDict.Values)
                {
                    // Debug.Log($"Add edge {sourceFunction.name} -> {targetFunction.name} to graph");
                    NodeInfo sourceNode = functionNodeDict[sourceFunction];
                    NodeInfo targetNode = functionNodeDict[targetFunction];
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

        public void BuildFunctionControlFlowGraph(OxideFunction function)
        {
 
        }
    }
}
