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
            foreach (OxideFunction function in binary.functionDict.Values)
            {
                // Debug.Log($"Add node {function.name} to graph");
                Vector3 position = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(2.0f, 5.0f), Random.Range(-5.0f, 5.0f));
                NodeInfo nodeInfo = graph.AddNodeToGraph(position, function.name, function.name);
                functionNodeDict[function] = nodeInfo;
                yield return new WaitForEndOfFrame(); 
            }

            foreach (OxideFunction sourceFunction in binary.functionDict.Values)
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
        }

        public void BuildFunctionControlFlowGraph(OxideFunction function)
        {
 
        }
    }
}
