using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace PUL
{
    /// <summary>
    /// Basic Graph -- add nodes and edges and nothing else interesting happens.
    ///
    /// Usage:
    /// -Attach this component to a GameObject in a scene.
    /// -Add nodes by calling <see cref="AddNodeToGraph"/> 
    /// -Add edges by calling <see cref="AddEdgeToGraph"/> and passing two nodes that have been
    ///  previously added to the graph
    /// -Run and Stop the graph using <see cref="StartGraph"/> and <see cref="StopGraph"/>. 
    /// </summary>
    public class BasicGraph : MonoBehaviour
    {
        /// <summary>
        /// All of the nodes in the graph.
        /// </summary>
        public Dictionary<int, NodeInfo> nodes = new();
        public Dictionary<int, int> idToIndexMap = new();

        // Unique index for each node. Just start at 0 and increment it for each new node.
        protected int currIndex = 0;

        /// <summary>
        /// Adds a <see cref="Node"/> component to the component gameobject that is passed.
        /// </summary>
        /// <param name="gameObject">The gameobject that will have a node attached.</param>
        [PublicAPI]
        public virtual NodeInfo AddNodeToGraph(GameObject gameObject) 
        {
            // Connect the gameObject to this graph
            gameObject.transform.SetParent(this.gameObject.transform, false);

            // Create and register the NodeInfo for this graph node.
            NodeInfo nodeInfo = gameObject.AddComponent<NodeInfo>();
            nodeInfo.nodeGameObject = gameObject;
            nodes[currIndex] = nodeInfo;
            idToIndexMap[gameObject.GetInstanceID()] = currIndex;
            nodeInfo.MyIndex = currIndex;
            currIndex++;
            return nodeInfo;
        }

        [PublicAPI]
        public virtual EdgeInfo AddEdgeToGraph(NodeInfo sourceNode, NodeInfo targetNode)
        {
            // Create gameObject
            GameObject graphEdgePrefab = Resources.Load("Prefabs/GraphArrow") as GameObject;
            GameObject graphEdge = Instantiate(graphEdgePrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);

            // Connect the gameObject to this graph
            graphEdge.transform.SetParent(this.gameObject.transform, false);

            // Attach line renderer
            LineRenderer lineRenderer = graphEdge.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
        
            // Attach, set, and return edge info
            EdgeInfo edgeInfo = graphEdge.AddComponent<EdgeInfo>();
            edgeInfo.sourceTransform = sourceNode.transform;
            edgeInfo.targetTransform = targetNode.transform;
            sourceNode.MyEdges.Add(targetNode.MyIndex);
            return edgeInfo;
        }

        [PublicAPI]
        public void Update()
        {
            // BARF-O-MATIC TEST -- test that the graph node & edge transforms are properly
            // connected to the parent graph by spinning the whole graph wildly. 
            // This should never be enabled in normal operations.
            // transform.localEulerAngles = transform.localEulerAngles + new Vector3(0, 1, 0);
        }

        [PublicAPI]
        public virtual void Clear()
        {
            foreach (NodeInfo node in nodes.Values)
            {
                Destroy(node);
            }

            nodes.Clear();
        }

        // void OnDestroy()
        // {
        //     Clear();
        // }

        [PublicAPI]
        public virtual void StartGraph()
        {
            // Nothing to do for basic graph
        }

        [PublicAPI]
        public virtual void StopGraph()
        {
            // Nothing to do for basic graph
        }

        [PublicAPI]
        public virtual void RunForIterations(int numIterations)
        {
            // Nothing to do for basic graph
        }
    }
}

