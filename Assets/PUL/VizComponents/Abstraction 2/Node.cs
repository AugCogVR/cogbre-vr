using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Node : MonoBehaviour
{
    //first value stores a parent node, the second value stores the prefab information for the second node.
    public IDictionary<GameObject, GameObject> parentNodes = new Dictionary<GameObject, GameObject>();
    //first value stores a child node, the second value stores the prefab information for the second node.
    public IDictionary<GameObject, GameObject> childNodes = new Dictionary<GameObject, GameObject>();
    public string nodeName;
    public string nodeType;
    public string nodeValue;
    
    void UpdateEdges()
    {
        foreach(KeyValuePair<GameObject, GameObject> node in parentNodes)
        {
            //draws line from parent's attach point to child's extend point
            node.Value.GetComponent<LineRenderer>().SetPosition(0, node.Key.transform.GetChild(4).position);
            node.Value.GetComponent<LineRenderer>().SetPosition(1, this.transform.GetChild(3).position);
        }
    }
    //displays all node information on UI text in scene.
    public void DisplayTextOnNode()
    {
        this.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshPro>().text = nodeName;
        this.transform.GetChild(2).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = nodeValue;
    }
    //sets the name of the gameobject to the name of the node
    public void SetNameOfGameObject()
    {
        this.name = nodeName;
    }
    
    //Adds child to node dictionary
    public void AddChild(GameObject childNode, GameObject edgePrefab)
    {
        this.childNodes.Add(childNode, edgePrefab);
    }
    //Adds parent to node dictionary.
    public void AddParent(GameObject parentNode, GameObject edgePrefab)
    {
        this.parentNodes.Add(parentNode, edgePrefab);
    }

    void Start()
    {
        //on start, sets the name of the game object to the node and displays information on node
        SetNameOfGameObject();
        DisplayTextOnNode();
    } 

    void Update()
    {
        //updates based on position of all nodes every frame.
        UpdateEdges();
    }
}
