using System;
using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    public class PolylineEdgeRouting
    {
        // // Represents a node in the graph
        // public class Node
        // {
        //     public Vector2 Position;
        //     public Rect Bounds;  // Rectangular bounding box of the node

        //     public Node(Vector2 position, float width, float height)
        //     {
        //         Position = position;
        //         Bounds = new Rect(position.x - width / 2, position.y - height / 2, width, height);
        //     }
        // }

        // // Represents a polyline edge from one node to another
        // public class Edge
        // {
        //     public Node StartNode;
        //     public Node EndNode;
        //     public List<Vector2> Path;

        //     public Edge(Node startNode, Node endNode)
        //     {
        //         StartNode = startNode;
        //         EndNode = endNode;
        //         Path = new List<Vector2>();
        //     }
        // }


        // edgeInfo.controlPoints = new List<GameObject>();
        // edgeInfo.controlPoints.Add(sourceNodeInfo.nodeGameObject);
        // edgeInfo.controlPoints.Add(targetNodeInfo.nodeGameObject);


        // Main function to route the edge between nodes.
        public static void RouteEdge(EdgeInfo edgeInfo, List<NodeInfo> nodes)
        {
            // Step 0: Set up convenience variables
            Transform parentTransform = edgeInfo.sourceNodeInfo.nodeGameObject.transform.parent;
            NodeInfo startNode = edgeInfo.sourceNodeInfo;
            NodeInfo endNode = edgeInfo.targetNodeInfo;

            // Step 1: Initialize the new control points
            edgeInfo.controlPoints = new List<GameObject>();

            // Step 2: Get start and end positions of the nodes
            // Vector3 startPosition = startNode.nodeGameObject.transform.localPosition;
            // Vector3 endPosition = endNode.nodeGameObject.transform.localPosition;

            (Vector3 startPoint, Vector3 endPoint) = FindEdgeStartAndEndPositions(startNode, endNode);

            // TO DO: Modify positions to point to edge of graph node instead of center

            // TEMP TEST: Add a couple of artificial control points to test out rest of code
            GameObject temp = new GameObject();
            temp.transform.SetParent(parentTransform, false);
            temp.transform.localPosition = startPoint;
            edgeInfo.controlPoints.Add(temp);
            // temp = new GameObject();
            // temp.transform.SetParent(parentTransform, false);
            // temp.transform.localPosition = new Vector3(startPoint.x * 0.2f, startPoint.y * 2.1f, startPoint.z);
            // edgeInfo.controlPoints.Add(temp);
            // temp = new GameObject();
            // temp.transform.SetParent(parentTransform, false);
            // temp.transform.localPosition = new Vector3(endPoint.x * 2.1f, endPoint.y * 0.2f, endPoint.z);
            // edgeInfo.controlPoints.Add(temp);
            temp = new GameObject();
            temp.transform.SetParent(parentTransform, false);
            temp.transform.localPosition = endPoint;
            edgeInfo.controlPoints.Add(temp);

            // // Step 3: Check if the start and end nodes are on the same level or row
            // if (Mathf.Abs(startPosition.y - endPosition.y) < 0.01f) || (Mathf.Abs(startPosition.x - endPosition.x) < 0.01f);
            // {
            //     // Simple case: Route edge directly between the nodes
            //     path.Add(startPosition);
            //     path.Add(endPosition);
            // }
            // else
            // {
            //     // Step 4: Avoid crossing through nodes by routing around them
            //     Rect bufferStart = GetNodeBufferZone(startNode);
            //     Rect bufferEnd = GetNodeBufferZone(endNode);

            //     // Step 4.2: Find a path around any obstacles (other nodes)
            //     List<Vector2> intermediatePath = FindPathAroundNodes(startNode, endNode, nodes, bufferStart, bufferEnd);

            //     if (intermediatePath != null)
            //     {
            //         path.AddRange(intermediatePath);
            //     }
            //     else
            //     {
            //         // Default fallback: direct route if no obstacles found
            //         path.Add(startPosition);
            //         path.Add(endPosition);
            //     }
            // }
        }

        // Given two rectangular nodes, find 
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
        //     float margin = 10f;  // Define a margin to avoid nodes
        //     return new Rect(node.Bounds.xMin - margin, node.Bounds.yMin - margin,
        //                     node.Bounds.width + 2 * margin, node.Bounds.height + 2 * margin);
        // }

        // // Helper function: Find a path around any obstacles (nodes)
        // public static List<Vector2> FindPathAroundNodes(Node startNode, Node endNode, List<Node> nodes, Rect bufferStart, Rect bufferEnd)
        // {
        //     // Initialize potential path
        //     List<Vector2> potentialPath = new List<Vector2>();

        //     // Step 1: Check for nodes that might obstruct the direct path
        //     foreach (Node node in nodes)
        //     {
        //         if (Intersects(node, bufferStart, bufferEnd))
        //         {
        //             // Path intersects with this node; attempt to find a detour path
        //             List<Vector2> detourPath = FindDetourPath(startNode, endNode, node);
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
        //     potentialPath.Add(startNode.Position);
        //     potentialPath.Add(endNode.Position);
        //     return potentialPath;
        // }

        // // Helper function: Check if two zones (e.g., node's buffer and an edge) intersect
        // public static bool Intersects(Node node, Rect bufferStart, Rect bufferEnd)
        // {
        //     return node.Bounds.Overlaps(bufferStart) || node.Bounds.Overlaps(bufferEnd);
        // }

        // // Helper function: Find a detour path around an obstacle node
        // public static List<Vector2> FindDetourPath(Node startNode, Node endNode, Node obstacleNode)
        // {
        //     // Simple strategy: Route above, below, or to the sides of the obstacle node
        //     List<Vector2> detourPath = new List<Vector2>();

        //     // Example: Route above the obstacle node
        //     detourPath.Add(startNode.Position);
        //     detourPath.Add(new Vector2(startNode.Position.x, obstacleNode.Position.y - 20));  // Detour above node
        //     detourPath.Add(new Vector2(endNode.Position.x, obstacleNode.Position.y - 20));   // Continue after the obstacle
        //     detourPath.Add(endNode.Position);

        //     return detourPath;
        // }

        // // Example of using the routing function
        // public static void ExampleUsage()
        // {
        //     // Create some example nodes
        //     Node startNode = new Node(new Vector2(0, 0), 50, 50);
        //     Node endNode = new Node(new Vector2(100, 100), 50, 50);
        //     List<Node> nodes = new List<Node>
        //     {
        //         new Node(new Vector2(50, 50), 50, 50),
        //         new Node(new Vector2(75, 75), 50, 50)
        //     };

        //     // Route the edge
        //     List<Vector2> path = RouteEdge(startNode, endNode, nodes);

        //     // Print the path
        //     foreach (var point in path)
        //     {
        //         Debug.Log("Path point: " + point);
        //     }
        // }
    }
}
