using System;
using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    public class PolylineEdgeRouting
    {
        // Main function to route the edge between nodes.
        // THIS CLASS IS UNFINISHED. 
        // It simply creates a straight-line edge between two nodes. 
        // The value it currently offers is to route the edge between 
        // appropriate locations along the outer bounds of the node graphic 
        // instead of between the node centers.
        public static void RouteEdge(EdgeInfo edgeInfo, List<NodeInfo> nodes)
        {
            // Step 0: Set up convenience variables
            Transform parentTransform = edgeInfo.sourceNodeInfo.nodeGameObject.transform.parent;
            NodeInfo startNode = edgeInfo.sourceNodeInfo;
            NodeInfo endNode = edgeInfo.targetNodeInfo;

            // Step 1: Initialize the new control points
            edgeInfo.controlPoints = new List<GameObject>();

            // Step 2: Get start and end positions of the nodes
            (Vector3 startPoint, Vector3 endPoint) = FindEdgeStartAndEndPositions(startNode, endNode);

            // // TEMP TEST: Add a couple of artificial control points to test out rest of code
            edgeInfo.controlPoints.Add(MakeControlPointGameObject(startPoint, parentTransform));
            // edgeInfo.controlPoints.Add(MakeControlPointGameObject(new Vector3(startPoint.x * 0.2f, startPoint.y * 2.1f, startPoint.z), parentTransform));
            // edgeInfo.controlPoints.Add(MakeControlPointGameObject(new Vector3(endPoint.x * 2.1f, endPoint.y * 0.2f, endPoint.z), parentTransform));
            edgeInfo.controlPoints.Add(MakeControlPointGameObject(endPoint, parentTransform));

            // // Step 3: Check if the start and end nodes are on the same level or row
            // if (Mathf.Abs(startPoint.y - endPoint.y) < 0.01f) || (Mathf.Abs(startPoint.x - endPoint.x) < 0.01f);
            // {
            //     // Simple case: Route edge directly between the nodes
            //     edgeInfo.controlPoints.Add(MakeControlPointGameObject(startPoint, parentTransform));
            //     edgeInfo.controlPoints.Add(MakeControlPointGameObject(endPoint, parentTransform));
            // }
            // else
            // {
            //     // Step 4: Avoid crossing through nodes by routing around them
            //     Rect bufferStart = GetNodeBufferZone(startNode);
            //     Rect bufferEnd = GetNodeBufferZone(endNode);
            //     List<GameObject> intermediatePath = FindPathAroundNodes(startNode, endNode, nodes, bufferStart, bufferEnd, parentTransform);

            //     if (intermediatePath != null)
            //     {
            //         edgeInfo.controlPoints.AddRange(intermediatePath);
            //     }
            //     else
            //     {
            //         // Default fallback: direct route if no obstacles found
            //         edgeInfo.controlPoints.Add(MakeControlPointGameObject(startPoint, parentTransform));
            //         edgeInfo.controlPoints.Add(MakeControlPointGameObject(endPoint, parentTransform));
            //     }
            // }
        }

        // Given a position and a parent, create a gameObject at that position as a child of the parent.
        public static GameObject MakeControlPointGameObject(Vector3 localPosition, Transform parentTransform)
        {
            GameObject temp = new GameObject();
            temp.transform.SetParent(parentTransform, false);
            temp.transform.localPosition = localPosition;
            return temp;
        }

        // Given two rectangular nodes, find the start and end positions of the edge between them.
        // The edge connects the midpoints of the sides closest to each other.
        public static (Vector3, Vector3) FindEdgeStartAndEndPositions(NodeInfo startNode, NodeInfo endNode)
        {
            Vector3 startPosition = startNode.nodeGameObject.transform.localPosition;
            Vector3 endPosition = endNode.nodeGameObject.transform.localPosition;

            Vector3 startSize = startNode.nodeGameObject.GetComponent<Collider>().bounds.size;
            Vector3 endSize = endNode.nodeGameObject.GetComponent<Collider>().bounds.size;
            // Debug.Log($"SCALE: {startSize} {endSize}");

            // Find new start position
            float closestDistance = float.MaxValue;
            // Find corners
            Vector3[] corners = new Vector3[4];
            corners[0] = new Vector3(startPosition.x - (startSize.x / 2f), startPosition.y - (startSize.y / 2f), startPosition.z);
            corners[1] = new Vector3(startPosition.x - (startSize.x / 2f), startPosition.y + (startSize.y / 2f), startPosition.z);
            corners[2] = new Vector3(startPosition.x + (startSize.x / 2f), startPosition.y + (startSize.y / 2f), startPosition.z);
            corners[3] = new Vector3(startPosition.x + (startSize.x / 2f), startPosition.y - (startSize.y / 2f), startPosition.z);
            // Check distance to all four sides and choose the closest one
            Vector3[] sides = new Vector3[]
            {
                (corners[0] + corners[1]) / 2,  // Left side (mid-point between bottom-left and top-left)
                (corners[1] + corners[2]) / 2,  // Top side (mid-point between top-left and top-right)
                (corners[2] + corners[3]) / 2,  // Right side (mid-point between top-right and bottom-right)
                (corners[3] + corners[0]) / 2   // Bottom side (mid-point between bottom-right and bottom-left)
            };
            // Debug.Log($"SPOTS: left {sides[0]} top {sides[1]} right {sides[2]} bottom {sides[3]}");
            foreach (var side in sides)
            {
                float distance = Vector3.Distance(side, endPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    startPosition = side;
                }
            }

            // Find new end position
            closestDistance = float.MaxValue;
            // Find corners
            corners = new Vector3[4];
            corners[0] = new Vector3(endPosition.x - (endSize.x / 2f), endPosition.y - (endSize.y / 2f), endPosition.z);
            corners[1] = new Vector3(endPosition.x - (endSize.x / 2f), endPosition.y + (endSize.y / 2f), endPosition.z);
            corners[2] = new Vector3(endPosition.x + (endSize.x / 2f), endPosition.y + (endSize.y / 2f), endPosition.z);
            corners[3] = new Vector3(endPosition.x + (endSize.x / 2f), endPosition.y - (endSize.y / 2f), endPosition.z);
            // Check distance to all four sides and choose the closest one
            sides = new Vector3[]
            {
                (corners[0] + corners[1]) / 2,  // Left side (mid-point between bottom-left and top-left)
                (corners[1] + corners[2]) / 2,  // Top side (mid-point between top-left and top-right)
                (corners[2] + corners[3]) / 2,  // Right side (mid-point between top-right and bottom-right)
                (corners[3] + corners[0]) / 2   // Bottom side (mid-point between bottom-right and bottom-left)
            };
            foreach (var side in sides)
            {
                float distance = Vector3.Distance(side, startPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    endPosition = side;
                }
            }

            return (startPosition, endPosition);
        }

        // // Helper function: Get a buffer zone around a node to avoid overlap
        // public static Rect GetNodeBufferZone(NodeInfo node)
        // {
        //     float margin = 0.10f;  // Define a margin to avoid nodes

        //     Vector3 nodePosition = node.nodeGameObject.transform.localPosition;
        //     Vector3 bounds = node.nodeGameObject.GetComponent<Collider>().bounds.size;

        //     Vector3 rectPosition = new Vector3(nodePosition.x - (bounds.x / 2f), nodePosition.y - (bounds.y / 2f), nodePosition.z);

        //     return new Rect(rectPosition.x - margin, rectPosition.y - margin,
        //                     bounds.x + (2 * margin), bounds.y + (2 * margin));
        // }

        // // Helper function: Find a path around any obstacles (nodes)
        // public static List<GameObject> FindPathAroundNodes(NodeInfo startNode, NodeInfo endNode, List<NodeInfo> nodes, Rect bufferStart, Rect bufferEnd, Transform parentTransform)
        // {
        //     // Initialize potential path
        //     List<GameObject> potentialPath = new List<GameObject>();

        //     // Step 1: Check for nodes that might obstruct the direct path
        //     foreach (NodeInfo node in nodes)
        //     {
        //         if (node.Bounds.Overlaps(bufferStart) || node.Bounds.Overlaps(bufferEnd))
        //         {
        //             // Path intersects with this node; attempt to find a detour path
        //             List<GameObject> detourPath = FindDetourPath(startNode, endNode, node);
        //             if (detourPath != null)
        //             {
        //                 return detourPath;
        //             }
        //             else
        //             {
        //                 return null;  // No valid detour found, return null
        //             }
        //         }
        //     }

        //     // If no obstacles are found, return a direct path
        //     potentialPath.Add(MakeControlPointGameObject(startNode.nodeGameObject.transform.localPosition, parentTransform));
        //     potentialPath.Add(MakeControlPointGameObject(endNode.nodeGameObject.transform.localPosition, parentTransform));
        //     return potentialPath;
        // }

        // // Helper function: Find a detour path around an obstacle node
        // public static List<GameObject> FindDetourPath(NodeInfo startNode, NodeInfo endNode, NodeInfo obstacleNode)
        // {
        //     // Simple strategy: Route above, below, or to the sides of the obstacle node
        //     List<GameObject> detourPath = new List<GameObject>();

        //     // Example: Route above the obstacle node
        //     detourPath.Add(startNode.Position);
        //     detourPath.Add(new Vector2(startNode.Position.x, obstacleNode.Position.y - 20));  // Detour above node
        //     detourPath.Add(new Vector2(endNode.Position.x, obstacleNode.Position.y - 20));   // Continue after the obstacle
        //     detourPath.Add(endNode.Position);

        //     return detourPath;
        // }
    }
}
