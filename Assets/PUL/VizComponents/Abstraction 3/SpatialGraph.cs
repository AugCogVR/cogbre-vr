using Microsoft.MixedReality.Toolkit;
using PUL2;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpatialGraph : MonoBehaviour
{
    // Refrence to graph manager
    public SpatialGraphManager graphManager;

    // Holds refrence to the node prefab
    public GameObject node;
    // Holds refrence to all contained nodes
    public List<SpatialNode> nodes = new List<SpatialNode>();
    // Refers to the parent node
    GameObject nodeParent = null;

    // Holds refrence to the ring graphic
    public GameObject graphBorder = null;
    // Holds refrence to the size of the ring graphic
    float graphBorderScale = 5;

    // Flag to check if the graph is done generating
    public bool doneGenerating = false;
    // Flag for if the graph is being interacted with
    public bool interacting = false;
    // Flag for if the nodes contained in the graph are done setting up
    // -> Starts true, sent to false by generation
    public bool staticGraph = true;
    // Flag for checking if we have shrunk the graph
    public bool graphTiny = false;

    // Refers to the bounds surrounding the graph
    Bounds graphBounds = new Bounds();
    // Refers to the target position of the current graph
    // -> This will be right in front of the player
    Vector3 targetPosition = Vector3.zero;
    // Refers to the distance the graph should set in relation to the player
    float targetSetDistance = .5f; 
    // Refers to the target scale of the current graph
    // -> This is used for resizing the graph to a consistant size
    Vector3 targetScale = Vector3.zero;
    // Refers to the general scale of the graph nodes
    float graphScale = .15f;
    [Space]
    public bool resizeRing = false;

    // Generates graph based off OID. Might move this to a manager to help parse data
    public void GenerateGraph(NexusObject OID)
    {
        // -> NOTE! CURRENTLY DOESNT PARSE ANY DATA BUT JUST GENERATES A RANDOM GRAPH

        // -> Adds nodes to the current graph
        int nodeCount = Random.Range(5, 25);
        int nodeID = 0;

        // -> Creates a new game object for storing nodes
        nodeParent = new GameObject("Node Parent");
        nodeParent.transform.parent = transform;

        for(int i = 0; i < nodeCount; i++)
        {
            // Create node based off of prefab
            // -> Childs to node parent
            GameObject sNode = Instantiate(node, nodeParent.transform);
            // -> Pull node script
            SpatialNode spatialNode = sNode.GetComponent<SpatialNode>();

            // Set node information
            spatialNode.graph = this;
            spatialNode.ID = nodeID;
            spatialNode.idOut.text = nodeID.ToString();
            sNode.name = $"Node [{nodeID}]";
            // Increment node id
            nodeID++;

            // Set up node transform
            sNode.transform.position = Random.insideUnitSphere.normalized * 0.1f;


            // -> Check if a link is allowed
            if(i != 0)
            {
                // Links nodes to random nodes contained in the current list
                // -> Creates a copy of the total list
                List<SpatialNode> avaliableLinks = new List<SpatialNode>(nodes);
                // -> Randomizes links
                int randomLinks = Random.Range(0, avaliableLinks.Count);

                for(int  j = 0; j < randomLinks; j++)
                {
                    // -> Randomize index in avaliableLinks
                    int randomIndex = Random.Range(0, avaliableLinks.Count);
                    spatialNode.ConnectToNode(avaliableLinks[randomIndex]);

                    // -> Remove avaliable link
                    avaliableLinks.RemoveAt(randomIndex);
                }
            }

            // Adds node to list
            nodes.Add(spatialNode);
        }

        // Flag the graph as done generating
        doneGenerating = true;
        // Set the graph to not static
        staticGraph = false;
    }

    // Pushes in a point, if the point is outside of the bounds then it will extend the bounds accordingly
    public void ModifyBounds(Vector3 point)
    {
        // Check if the point is outside of bounds
        // -> If so, return early
        if (graphBounds.Contains(point)) return;

        // -> If not, extend bounds to include
        graphBounds.Encapsulate(point);
    }

    public void Update()
    {
        if (resizeRing)
        {
            ResizeRing();
            resizeRing = false;
        }

        // -> Check if the graph is done generating
        if (!doneGenerating) return;

        // -> Check if every node in the graph is done moving
        if (!staticGraph)
        {
            // -> Check through all nodes, see if any are still moving
            foreach(SpatialNode node in nodes)
                if (!node.doneMoving)
                    return;

            // -> If all nodes are done moving, set graph to static
            staticGraph = true;

            // Grab the target position of the graph
            targetPosition = (Camera.main.transform.forward * targetSetDistance) + Camera.main.transform.position;
            targetPosition += Vector3.down * targetSetDistance / 6f;

            // Get the biggest side of the graph bounds
            float maxSide = Mathf.Max(graphBounds.size.x, Mathf.Max(graphBounds.size.y, graphBounds.size.z));

            // Grab the target scale for the current graph
            targetScale = Vector3.one * (1 / maxSide) * graphScale;

            // Set transform and scale
            transform.localScale = targetScale;
            transform.position = targetPosition;



            // Force nodes to centralize around graph point
            // -> Recalculate the center of the graph
            Vector3 totalNodePosition = Vector3.zero;
            foreach (SpatialNode node in nodes)
                totalNodePosition += node.transform.localPosition;
            // -> Get average position
            totalNodePosition /= nodes.Count;
            // -> Get world difference
            Vector3 nodeDifference = totalNodePosition - nodeParent.transform.position;
            // Move nodes (forcefully)
            foreach (SpatialNode node in nodes)
                node.transform.localPosition += nodeDifference;



            // Recalculate bounds
            graphBounds = new Bounds();
            graphBounds.center = totalNodePosition;
            // -> Roll through each node
            foreach (SpatialNode node in nodes)
                ModifyBounds(node.transform.position);
            // Grow bounds by scale / 2 units
            // -> This is for padding
            graphBounds.Expand(graphScale / 2f);
            // Get the new biggest side of the graph bounds
            maxSide = Mathf.Max(graphBounds.size.x, Mathf.Max(graphBounds.size.y, graphBounds.size.z));



            // Set nodes to render
            foreach (SpatialNode node in nodes)
                node.SetVisible(true);



            // Set border to max size
            // -> Unify edges by dividing by parent scale
            // -> Add extra padding
            graphBorder.transform.localScale = new Vector3(maxSide * graphBorder.transform.localScale.x, maxSide * graphBorder.transform.localScale.y, maxSide * graphBorder.transform.localScale.z) + (Vector3.one * graphScale / 4f);
            graphBorder.transform.localPosition = Vector3.zero;
            graphBorderScale = graphBorder.transform.localScale.x;
            // -> Activate
            graphBorder.SetActive(true);



            // -> Push parent node underneath the border
            nodeParent.transform.parent = graphBorder.transform;
            nodeParent.transform.localPosition = Vector3.zero;// new Vector3(0, graphBounds.size.y, 0) * 6;
        }
    }

    // When the user is done manipulating the scale/rotation of the graph the border will resize into view
    public void ResizeRing()
    {
        // Grab the scale that the border should be shrunk by
        float sScale = graphBorder.transform.localScale.x / graphBorderScale;

        // -> Expand the graph object by the shrink scale
        //nodeParent.transform.localPosition *= sScale;
        nodeParent.transform.localScale *= sScale;

        // -> Shrink the bounds to be 1 unit in scale
        graphBorder.transform.localScale = new Vector3(1, 1, 1) * graphBorderScale;

    }


    // Draws Gizmos associated with graph, this includes...
    // -> Graph bounds
    // -> Target graph position
    // -> Target graph size
    public void OnDrawGizmos()
    {
        // Draw bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(nodeParent.transform.position, graphBounds.size);

        // Draw graph position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.1f);

        // Draw graph size
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(targetPosition, targetScale);
    }
}
