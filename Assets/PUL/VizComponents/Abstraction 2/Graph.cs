/*
 * CODE FOR INSTATNIATING A CONTROL FLOW GRAPH. USES DEPRECATED GAME MANAGER SCRIPT, HARVEST USEFUL CODE IF NECESSARY
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PUL2
{
    public class Graph : MonoBehaviour
    {
        public GameObject nodePrefab;
        public GameObject edgePrefab;
        //name of node associated with the game object;
        public IDictionary<string, GameObject> totalNodes = new Dictionary<string, GameObject>();
        public List<GameObject> totalEdges = new List<GameObject>();

        //adds node to graph and stores the node in the list "total nodes"
        public void AddNodeToGraph(string nodeName, List<string> nodeValue, GameManager gameManager)
        {

            GameObject node = Object.Instantiate(nodePrefab);
            Node nodeComponent = node.GetComponent<Node>();
            nodeComponent.gameManager = gameManager;
            nodeComponent.nodeName = nodeName;
            nodeComponent.nodeValue = nodeValue;
            totalNodes.Add(nodeName, node);
        }

        

        public GameObject FindNodeInGraph(string nodeName)
        {
            return GameObject.Find(nodeName);
        }
        //assigns all children to the nodes present within the graph (if they have any)
        //CONVERT THIS TO TAKE IN A DICTIONARY INSTEAD OF A LIST ***********************************************************
        public void AssignChildrenToNode(GameObject parentNode, List<GameObject> childNodes)
        {
            foreach (GameObject node in childNodes)
            {
                GameObject newEdge = Object.Instantiate(edgePrefab);
                parentNode.GetComponent<Node>().AddChild(node, newEdge);
                totalEdges.Add(newEdge);
                node.GetComponent<Node>().AddParent(parentNode, newEdge);
            }
        }

        //perfoms a level order traversal down the tree of nodes and returns a list of all the node layers' lengths in a convenient list.
        public void SetLayerList(ref List<List<GameObject>> layerNodes, GameObject startingNode)
        {
            List<List<GameObject>> ans = new List<List<GameObject>>();

            if (startingNode == null)
                Debug.Log("N-Ary tree does not any nodes");

            // Create one queue main_queue
            Queue<GameObject> main_queue = new Queue<GameObject>();
            HashSet<GameObject> visited = new HashSet<GameObject>();

            // Push the root value in the main_queue
            main_queue.Enqueue(startingNode);
            visited.Add(startingNode);

            // Traverse the N-ary Tree by level
            while (main_queue.Any())
            {
                // Create a temp vector to store the all the
                // node values present at a particular level
                List<GameObject> temp = new List<GameObject>();
                int size = main_queue.Count;
                // Iterate through the current level
                for (int i = 0; i < size; i++)
                {
                    GameObject node = main_queue.Dequeue();
                    temp.Add(node);

                    foreach (KeyValuePair<GameObject, GameObject> childNode in node.GetComponent<Node>().childNodes)
                    {
                        if (!visited.Contains(childNode.Key))
                        {
                            main_queue.Enqueue(childNode.Key);
                            visited.Add(childNode.Key);
                        }
                    }
                }

                ans.Add(temp);
            }

            foreach (List<GameObject> list in ans)
            {
                //Debug.Log(list.ToString());
                layerNodes.Add(list);
            }


        }

        public void ArrangeGraph(GameObject startNode, GameObject exitNode)
        {
            //Debug.Log("Arranging Graph!");
            List<List<GameObject>> layerNodes = new List<List<GameObject>>();
            SetLayerList(ref layerNodes, startNode);
            //Debug.Log(layerNodes.Count());
            float yTransform = layerNodes.Count();

            startNode.transform.localPosition = new Vector3(0, yTransform, 0);
            


            //Debug.Log(layerNodes.Count());

            foreach (List<GameObject> nodes in layerNodes)
            {
                bool noStartOrExitPresent = true;
                foreach (GameObject node in nodes)
                {
                    if (node.GetComponent<Node>().nodeName == startNode.GetComponent<Node>().nodeName || node.GetComponent<Node>().nodeName == exitNode.GetComponent<Node>().nodeName)
                    {
                        //Debug.Log("Start or Exit Present!");
                        noStartOrExitPresent = false;
                    }

                }
                //Debug.Log("Continuing Onward!");
                if (noStartOrExitPresent)
                {
                    //Debug.Log("Line 107 If Statement Works!");
                    float xTransform = 0;
                    foreach (GameObject node in nodes)
                    {
                        node.transform.localPosition = new Vector3(xTransform, yTransform, 0);
                        xTransform += 2;
                    }
                }
             
                    foreach (GameObject node in nodes)
                    {
                        foreach (GameObject nodeComparison in nodes)
                        {
                            if (node.transform.localPosition == nodeComparison.transform.localPosition && nodeComparison != node)
                            {
                                float xTransform = nodeComparison.transform.localPosition.x + 1;
                                nodeComparison.transform.localPosition = new Vector3(xTransform, nodeComparison.transform.localPosition.y, 0);
                            }
                        }
                    }
                

                    yTransform = yTransform - 1;


            }

           

        }

        public void DestroyGraph()
        {
            foreach (KeyValuePair<string,GameObject> node in totalNodes)
            {
                Object.Destroy(node.Value);
            }
            foreach (GameObject edge in totalEdges)
            {
                Object.Destroy(edge);
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
}
*/