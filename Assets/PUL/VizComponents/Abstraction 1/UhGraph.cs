using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace PUL_OLD
{
    // /// <summary>
    // /// Uh... a graph. UNTESTED. Adapted from https://github.com/atonalfreerider/Unity-FDG 
    // ///
    // /// Usage:
    // /// -Attach this component to a GameObject in a scene.
    // /// -Add nodes by calling <see cref="AddNodeToGraph"/> and passing a Unity component that will act
    // ///  as the node.
    // /// -Add edges by calling <see cref="AddEdgeToGraph"/> and passing two components that have been
    // ///  previously added to the graph and therefore have <see cref="Node"/> components attached to them.
    // /// </summary>
    // public class UhGraph : MonoBehaviour
    // {
    //     /// <summary>
    //     /// All of the nodes in the graph.
    //     /// </summary>
    //     readonly Dictionary<int, Node> nodes = new();
    //     readonly Dictionary<int, int> idToIndexMap = new();


    //     /// <summary>
    //     /// Adds a <see cref="Node"/> component to the component gameobject that is passed. When the graph is run,
    //     /// this behaviour will move the gameobject as it responds to forces in the graph.
    //     /// </summary>
    //     /// <param name="component">The component whose gameobject will have a node attached.</param>
    //     /// <param name="index">A UNIQUE index for this node.</param>
    //     /// <param name="nodeMass">The mass of the node. NOT USED YET.</param>
    //     [PublicAPI]
    //     public void AddNodeToGraph(Component component, int index, int nodeMass = 1)
    //     {
    //         Node newNode = component.gameObject.AddComponent<Node>();
    //         newNode.scn = (SimpleCubeNode)component;
    //         nodes.Add(index, newNode);
    //         idToIndexMap.Add(component.GetInstanceID(), index);
    //     }

    //     [PublicAPI]
    //     public void AddEdgeToGraph(Component componentA, Component componentB)
    //     {
    //         int indexA = idToIndexMap[componentA.GetInstanceID()];
    //         int indexB = idToIndexMap[componentB.GetInstanceID()];
    //         nodes.TryGetValue(indexA, out Node nodeA);
    //         nodes.TryGetValue(indexB, out Node nodeB);

    //         if (nodeA != null && nodeB != null)
    //         {
    //             nodeA.MyEdges.Add(indexB);
    //             nodeB.MyEdges.Add(indexA);
    //         }
    //     }

    //     [PublicAPI]
    //     public void OnUpdate()
    //     {
    //         foreach (Node node in nodes.Values)
    //         {
    //             node.scn.OnUpdate();
    //         }
    //     }

    //     [PublicAPI]
    //     public void Clear()
    //     {
    //         foreach (Node node in nodes.Values)
    //         {
    //             Destroy(node);
    //         }

    //         nodes.Clear();
    //     }

    //     [PublicAPI]
    //     public void StartGraph()
    //     {
    //         foreach (Node node in nodes.Values)
    //         {
    //             node.VirtualPosition = node.transform.position;
    //         }
    //     }

    //      [PublicAPI]
    //     public void StopGraph()
    //     {
    //     }

    //     void OnDestroy()
    //     {
    //         Clear();
    //     }

    //     class Node : MonoBehaviour
    //     {
    //         public SimpleCubeNode scn;   // TODO: This reference probably doesn't belong here OR should be a "node interface" for abstraction purposes
    //         public float Mass;
    //         public bool IsImmobile = false;
    //         public Vector3 VirtualPosition = Vector3.zero;
    //         public readonly List<int> MyEdges = new();
    //    }
    // }
}
