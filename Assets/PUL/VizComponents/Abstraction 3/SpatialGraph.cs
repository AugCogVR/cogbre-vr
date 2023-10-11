using PUL2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialGraph : MonoBehaviour
{
    // Refrence to graph manager
    public SpatialGraphManager graphManager;

    // Holds refrence to the node prefab
    public GameObject node;
    // Holds refrence to all contained nodes
    public List<SpatialNode> nodes = new List<SpatialNode>();

    // Flag for if the graph is being interacted with
    public bool interacting = false;

    // Generates graph based off OID. Might move this to a manager to help parse data
    public void GenerateGraph(NexusObject OID)
    {
        // -> NOTE! CURRENTLY DOESNT PARSE ANY DATA BUT JUST GENERATES A RANDOM GRAPH

        int nodeCount = Random.Range(5, 25);
        int nodeID = 0;

        for(int i = 0; i < nodeCount; i++)
        {
            // Create node based off of prefab
            // -> Childs to graph
            GameObject sNode = Instantiate(node, transform);
            // -> Pull node script
            SpatialNode spatialNode = sNode.GetComponent<SpatialNode>();

            // Set node information
            spatialNode.graph = this;
            spatialNode.ID = nodeID;
            nodeID++;

            // Set up node transform
            sNode.transform.position = Random.insideUnitSphere.normalized * 1;


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
    }
}
