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
    /// Graph for inherently hierarchical data. Positions nodes on 2D plane in 3D space.
    ///
    /// Usage:
    /// -Attach this component to a GameObject in a scene.
    /// -Add nodes by calling <see cref="AddNodeToGraph"/> 
    /// -Add edges by calling <see cref="AddEdgeToGraph"/> and passing two nodes that have been
    ///  previously added to the graph
    /// -Run and Stop the graph using <see cref="StartGraph"/> and <see cref="StopGraph"/>. 
    /// </summary>
    public class HierarchicalGraph : BasicGraph
    {
        // Graph nodes
        public List<NodeInfo> nodes = new();

        // Graph edges, accessed by source and target node tuples.
        public Dictionary<(NodeInfo, NodeInfo), EdgeInfo> edges = new();


        /// <summary>
        /// Adds a <see cref="Node"/> component to the component gameobject that is passed. When the graph is run,
        /// this behaviour will move the gameobject as it responds to forces in the graph.
        /// </summary>
        /// <param name="gameObject">The gameobject that will have a node attached.</param>
        [PublicAPI]
        public override NodeInfo AddNodeToGraph(GameObject gameObject)
        {
            NodeInfo nodeInfo = base.AddNodeToGraph(gameObject);
            nodes.Add(nodeInfo);
            return nodeInfo;
        }

        [PublicAPI]
        public override EdgeInfo AddEdgeToGraph(NodeInfo sourceNode, NodeInfo targetNode)
        {
            EdgeInfo edgeInfo = base.AddEdgeToGraph(sourceNode, targetNode);
            edges[(sourceNode, targetNode)] = edgeInfo;
            return edgeInfo;
        }

        [PublicAPI]
        public override void StartGraph()
        {
            StartCoroutine(StartGraphCoroutine());
        }

        IEnumerator StartGraphCoroutine()
        {
            // Arrange the nodes into a hierarchical graph

            // THIS IS CURRENTLY A VERY RUDIMENTARY GRID-BASED LAYOUT.
            // TODO: Implement a Sugiyama-like layout

            // As we process the graph nodes, collect what nodes are at what levels
            Dictionary<int, IList<NodeInfo>> layout = new Dictionary<int, IList<NodeInfo>>();

            // Add nodes by doing a breadth-first search with a queue
            Queue<(NodeInfo, int)> nodeInfosToProcess = new Queue<(NodeInfo, int)>();

            // Start by adding nodes that have no sources to the queue
            foreach (NodeInfo nodeInfo in nodes)
                if (nodeInfo.sourceNodeInfos.Count == 0)
                    nodeInfosToProcess.Enqueue((nodeInfo, 0));

            // Until the queue is empty, add function nodes to the graph.
            while (nodeInfosToProcess.Count > 0)
            {
                (NodeInfo sourceNodeInfo, int level) = nodeInfosToProcess.Dequeue();
                if (!layout.ContainsKey(level)) layout[level] = new List<NodeInfo>();
                layout[level].Add(sourceNodeInfo);
                sourceNodeInfo.added = true;
                foreach (NodeInfo targetNodeInfo in sourceNodeInfo.targetNodeInfos)
                {
                    if (targetNodeInfo.added) continue; // don't add nodes multiple times
                    nodeInfosToProcess.Enqueue((targetNodeInfo, level + 1));
                }
                yield return new WaitForEndOfFrame();
            }

            // Reposition nodes per the collected layout
            float xOffset = 0.25f;
            float yOffset = -0.25f;
            foreach (int level in layout.Keys)
            {
                int xCount = 0;
                foreach (NodeInfo nodeInfo in layout[level])
                {
                    Vector3 size = nodeInfo.nodeGameObject.GetComponent<Collider>().bounds.size;
                    // Debug.Log($" SIZE SIZE SIZE {size}");
                    float x = ((size.x + 0.2f) * xCount) + xOffset;
                    float y = (-size.y * 2.0f * level) + yOffset;
                    nodeInfo.transform.localPosition = new Vector3(x, y, 0);
                    xCount++;
                }
                yield return new WaitForEndOfFrame();
            }

            // Add additional control points to the edges to test the handling of multiple control points
            // along an edge. 
            foreach(EdgeInfo edgeInfo in edges.Values)
            {
                Vector3 sourcePosition = edgeInfo.controlPoints[0].transform.position;
                Vector3 targetPosition = edgeInfo.controlPoints[1].transform.position;
                GameObject onethird = new GameObject();
                onethird.transform.SetParent(this.gameObject.transform, false);
                onethird.transform.position = Vector3.Lerp(sourcePosition, targetPosition, 0.33f);
                edgeInfo.controlPoints.Insert(1, onethird);
                GameObject twothirds = new GameObject();
                twothirds.transform.SetParent(this.gameObject.transform, false);
                twothirds.transform.position = Vector3.Lerp(sourcePosition, targetPosition, 0.67f);
                edgeInfo.controlPoints.Insert(2, twothirds);
                // Uncomment the next two lines to stress the Bezier capability
                // onethird.transform.position = new Vector3(0, 0, 0);
                // twothirds.transform.position = new Vector3(1, 1, 1);
                yield return new WaitForEndOfFrame();
            }
        }
    }
}


