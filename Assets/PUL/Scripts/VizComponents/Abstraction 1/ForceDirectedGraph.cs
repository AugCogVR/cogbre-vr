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
    /// A force directed graph for Unity that uses Hooke's Law and Coulombs Law running in Unity C# Jobs.
    /// Adaptation of this code: https://github.com/atonalfreerider/Unity-FDG
    ///
    /// Usage:
    /// -Attach this component to a GameObject in a scene.
    /// -Add nodes by calling <see cref="AddNodeToGraph"/> 
    /// -Add edges by calling <see cref="AddEdgeToGraph"/> and passing two nodes that have been
    ///  previously added to the graph
    /// -Run and Stop the graph using <see cref="StartGraph"/> and <see cref="StopGraph"/>. 
    /// </summary>
    public class ForceDirectedGraph : BasicGraph
    {
        /// <summary>
        /// All of the nodes in the graph.
        /// </summary>
        public Dictionary<int, NodeInfo> nodes = new();
        public Dictionary<int, int> idToIndexMap = new();

        // Unique index for each node. Just start at 0 and increment it for each new node.
        protected int currIndex = 0;

        bool backgroundCalculation = false;

        /// <summary>
        /// A coroutine that continuously runs and updates the state of the world on every iteration.
        /// </summary>
        Coroutine graphAnimator;

        // variables that can be set externally or adjusted from the Unity Editor.
        [Header("Adjustable Values")][Range(0.001f, 500)]
        // The constant that resembles Ke in Coulomb's Law to signify the strength of the repulsive force between nodes.
        public float UniversalRepulsiveForce = 1.0f; // 0.001f; // 4.5f;

        [Range(0.001f, 100)]
        // The constant that resembles K in Hooke's Law to signify the strength of the attraction on an edge.
        public float UniversalSpringForce = 0.001f; // 1.0f;

        [Range(1, 10)]
        // The speed at which each iteration is run (lower is faster).
        public int TimeStep = 2;

        [Range(1, 20)]
        // An optimization for the C# Job. Gradually increase this value until performance begins to drop.
        public int ForceCalcBatch = 1;

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
            nodes[currIndex] = nodeInfo;
            idToIndexMap[gameObject.GetInstanceID()] = currIndex;
            nodeInfo.MyIndex = currIndex;
            currIndex++;
            return nodeInfo;
        }

        [PublicAPI]
        public override EdgeInfo AddEdgeToGraph(NodeInfo sourceNode, NodeInfo targetNode)
        {
            EdgeInfo edgeInfo = base.AddEdgeToGraph(sourceNode, targetNode);
            sourceNode.MyEdges.Add(targetNode.MyIndex); 
            return edgeInfo;
        }

        [PublicAPI]
        public override void StartGraph(GameObject graphHandle)
        {
            foreach (NodeInfo node in nodes.Values)
            {
                node.VirtualPosition = node.transform.localPosition;
            }

            graphAnimator = StartCoroutine(Iterate());
        }

        [PublicAPI]
        public override void StopGraph()
        {
            if (graphAnimator == null) return;

            StopCoroutine(graphAnimator);
            graphAnimator = null;
        }

        [PublicAPI]
        public override void RunForIterations(int numIterations)
        {
            backgroundCalculation = true;  // this makes it not run.... ????
            foreach (NodeInfo node in nodes.Values)
            {
                node.VirtualPosition = node.transform.localPosition;
            }

            graphAnimator = StartCoroutine(Iterate(numIterations));
        }

        [PublicAPI]
        //This creates "Anchor Nodes" that are immobile. Stabilizes lone nodes, or nodes that only have 1 target.
        //TO DO: a good fix, but it's ugly. obvious that some work is going on behind the scenes, because the nodes with single edges are often stretched across the screen. Needs some work, but good for now.
        public void setLoneValuesImmobile()
        {
            foreach(NodeInfo node in nodes.Values)
            {
                if(node.MyEdges.Count <= 1)
                {
                    // Debug.Log(node.MyEdges.Count);
                    node.IsImmobile = true;
                }
            }
        }

        [PublicAPI]
        public void SetNodeMobility(Component nodeComponent, bool isImmobile)
        {
            NodeInfo node = nodeComponent.GetComponent<NodeInfo>();
            if (node == null) return;
            node.IsImmobile = isImmobile;
        }

        [PublicAPI]
        public void SetNodeMass(Component nodeComponent, float nodeMass)
        {
            NodeInfo node = nodeComponent.GetComponent<NodeInfo>();
            if (node == null) return;
            node.Mass = nodeMass;
        }

        IEnumerator Iterate(int remainingIterations = 0)
        {
            // Perform a job to get all resulting balance forces. Each set of forces of length 1 less than all nodes
            // are all of the forces acting on node N in the nodes collection.
            List<Vector3> balanceDisplacements = NodeBalanceDisplacements().ToList();

            int finalCount = 0;
            foreach (NodeInfo node in nodes.Values)
            {
                Vector3 finalForce = balanceDisplacements[finalCount];
                finalCount++;
                if (node.IsImmobile) continue;
                node.VirtualPosition += finalForce;
            }

            if (!backgroundCalculation || remainingIterations > 0)
            {
                if (!backgroundCalculation)
                {
                    foreach (NodeInfo node in nodes.Values)
                    {
                        node.transform.localPosition = node.VirtualPosition;
                    }

                    yield return null;
                    graphAnimator = StartCoroutine(Iterate());
                }
                else
                {
                    graphAnimator = StartCoroutine(Iterate(remainingIterations - 1));
                }
            }
            else
            {
                yield return MoveToFinal();
            }
        }

        IEnumerator MoveToFinal()
        {
            float prog = 0;
            float animSec = 1;
            while (prog < animSec)
            {
                foreach (NodeInfo node in nodes.Values)
                {
                    node.transform.localPosition = Vector3.Lerp(
                        node.transform.localPosition,
                        node.VirtualPosition,
                        Time.deltaTime / (animSec - prog)
                    );
                }

                yield return null;

                prog += Time.deltaTime;
            }

            foreach (NodeInfo node in nodes.Values)
            {
                node.transform.localPosition = node.VirtualPosition;
            }
        }

        /// <summary>
        /// Run a job to calculate the balance forces that each node is experiencing from every other node in the map. 
        /// </summary>
        /// <returns>The result forces on each node in the order that they are represented in nodes.</returns>
        IEnumerable<Vector3> NodeBalanceDisplacements()
        {
            // prepare native arrays for each calculation value
            NativeArray<Vector3> nodePositions =
                new NativeArray<Vector3>(nodes.Count, Allocator.TempJob);
            NativeArray<float> nodeMasses =
                new NativeArray<float>(nodes.Count, Allocator.TempJob);
            NativeArray<int> edgeBlocks =
                new NativeArray<int>(nodes.Count, Allocator.TempJob);

            NativeArray<Vector3> nodeResultDisplacement =
                new NativeArray<Vector3>(nodes.Count, Allocator.TempJob);

            List<int> allEdges = new List<int>();
            foreach (KeyValuePair<int, NodeInfo> idxAndNode in nodes)
            {
                nodePositions[idxAndNode.Key] = idxAndNode.Value.VirtualPosition;
                nodeMasses[idxAndNode.Key] = idxAndNode.Value.Mass;
                edgeBlocks[idxAndNode.Key] = idxAndNode.Value.MyEdges.Count;

                allEdges.AddRange(idxAndNode.Value.MyEdges);
            }

            NativeArray<int> edgeIndices =
                new NativeArray<int>(allEdges.Count, Allocator.TempJob);
            for (int i = 0; i < allEdges.Count; i++)
            {
                edgeIndices[i] = allEdges[i];
            }

            BalanceForceJob balanceForceJob = new()
            {
                NodePositions = nodePositions,
                NodeMasses = nodeMasses,
                EdgeBlocks = edgeBlocks,
                EdgeIndices = edgeIndices,
                Ke = UniversalRepulsiveForce,
                K = UniversalSpringForce,
                TimeValue = TimeStep,
                NodeResultDisplacement = nodeResultDisplacement
            };

            // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
            JobHandle handle = balanceForceJob.Schedule(
                nodeResultDisplacement.Length,
                ForceCalcBatch);

            // Wait for the job to complete
            handle.Complete();

            List<Vector3> results = nodeResultDisplacement.ToList();

            // Free the memory allocated by the arrays
            nodePositions.Dispose();
            nodeMasses.Dispose();
            edgeBlocks.Dispose();
            edgeIndices.Dispose();
            nodeResultDisplacement.Dispose();

            return results;
        }

        struct BalanceForceJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> NodePositions;
            [ReadOnly] public NativeArray<float> NodeMasses;
            [ReadOnly] public NativeArray<int> EdgeBlocks;
            [ReadOnly] public NativeArray<int> EdgeIndices;

            [ReadOnly] public float Ke;
            [ReadOnly] public float K;
            [ReadOnly] public float TimeValue;

            public NativeArray<Vector3> NodeResultDisplacement;

            public void Execute(int i)
            {
                Vector3 nodeI = NodePositions[i];
                Vector3 resultForceAndDirection = Vector3.zero;

                int edgesStart = 0;
                for (int z = 0; z < i; z++)
                {
                    edgesStart += EdgeBlocks[z];
                }

                int edgesEnd = edgesStart + EdgeBlocks[i];

                for (int j = 0; j < NodePositions.Length; j++)
                {
                    if (i == j) continue;
                    Vector3 nodeJ = NodePositions[j];
                    float distance = Vector3.Distance(nodeI, nodeJ);
                    Vector3 direction = Vector3.Normalize(nodeI - nodeJ);

                    bool isActor = false;
                    for (int w = edgesStart; w < edgesEnd; w++)
                    {
                        if (EdgeIndices[w] == j)
                        {
                            isActor = true;
                            w = edgesEnd;
                        }
                    }

                    // Hooke's Law attractive force p2 <- p1
                    float hF = isActor ? K * distance : 0;

                    // Coulomb's Law repulsive force p2 -> p1
                    float cF = Ke / (distance * distance);

                    resultForceAndDirection += (cF - hF) * direction;
                }

                // Divide the result force by the amount of displacements that were summed and also by the node mass and
                // the time step in the calculation.
                NodeResultDisplacement[i] = resultForceAndDirection /
                                            (TimeValue * NodeMasses[i] * (NodePositions.Length - 1));
            }
        }
    }
}


