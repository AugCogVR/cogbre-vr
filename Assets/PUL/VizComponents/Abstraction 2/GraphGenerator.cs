using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphGenerator : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public List<GameObject> totalNodes = new List<GameObject>();

    //adds node to graph and stores the node in the list "total nodes"
    public void AddNodeToGraph(string nodeName, string nodeValue)
    {
        Vector3 objPosition = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0);
        GameObject node = Object.Instantiate(nodePrefab, objPosition, Quaternion.identity);
        Node nodeComponent = node.GetComponent<Node>();
        nodeComponent.nodeName = nodeName;
        nodeComponent.nodeValue = nodeValue;
        totalNodes.Add(node);
    }

    //assigns all children to the nodes present within the graph (if they have any)
    public void AssignChildrenToNode(GameObject parentNode, List<GameObject> childNodes)
    {
        foreach(GameObject node in childNodes)
        {
            GameObject newEdge = Object.Instantiate(edgePrefab);
            parentNode.GetComponent<Node>().AddChild(node, newEdge);
            node.GetComponent<Node>().AddParent(parentNode, newEdge);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
