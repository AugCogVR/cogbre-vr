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
        /// <summary>
        /// Adds a <see cref="Node"/> component to the component gameobject that is passed. When the graph is run,
        /// this behaviour will move the gameobject as it responds to forces in the graph.
        /// </summary>
        /// <param name="gameObject">The gameobject that will have a node attached.</param>
        [PublicAPI]
        public override NodeInfo AddNodeToGraph(GameObject gameObject)
        {
            NodeInfo nodeInfo = base.AddNodeToGraph(gameObject);

            nodeInfo.Mass = 1;

            return nodeInfo;
        }

        [PublicAPI]
        public override EdgeInfo AddEdgeToGraph(NodeInfo sourceNode, NodeInfo targetNode)
        {
            return base.AddEdgeToGraph(sourceNode, targetNode);
        }

        [PublicAPI]
        public override void StartGraph()
        {
            // TODO: Move logic that positions the nodes from GraphManager to here
        }
    }
}


