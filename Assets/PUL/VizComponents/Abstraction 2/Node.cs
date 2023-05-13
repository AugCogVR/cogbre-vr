using System.Collections;
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
    public List<string> nodeValue;
    
    void UpdateEdges()
    {
        foreach(KeyValuePair<GameObject, GameObject> node in parentNodes)
        {
            //draws line from parent's attach point to child's extend point
            node.Value.transform.GetChild(0).position = node.Key.transform.GetChild(4).position;
            node.Value.transform.GetChild(1).position = this.transform.GetChild(3).position;
        }
    }
    //displays all node information on UI text in scene.
    public void DisplayTextOnNode()
    {
        this.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshPro>().text = nodeName;
        
        if(nodeValue == null)
        {
            Debug.LogWarning("nodeValue is empty!");
        }
        else
        {
            Vector3 offset = new Vector3(0, 10f, 0);
            for (int i = 0; i < nodeValue.Count; i++)
            {
                if (i == 0)
                {
                    //Debug.Log(nodeValue[i]);
                    this.transform.GetChild(2).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = nodeValue[0];
                }
                else
                {
                    Transform originalRect = this.transform.GetChild(2).GetChild(0);
                    GameObject newTextObject = Object.Instantiate(originalRect.gameObject, this.transform.GetChild(2));
                    RectTransform newTextRectTransform = newTextObject.GetComponent<RectTransform>();
                    RectTransform originalRectTransform = originalRect.GetComponent<RectTransform>();

                    //newTextRectTransform.localPosition = originalRectTransform.anchoredPosition - (offset * i);
                    newTextRectTransform.anchoredPosition3D = originalRectTransform.anchoredPosition3D - (offset * i);
                    newTextRectTransform.localScale = originalRectTransform.localScale;
                    newTextRectTransform.rotation = originalRectTransform.rotation;

                    newTextRectTransform.pivot = originalRectTransform.pivot;
                    newTextRectTransform.anchorMin = originalRectTransform.anchorMin;
                    newTextRectTransform.anchorMax = originalRectTransform.anchorMax;

                    newTextObject.GetComponent<TextMeshProUGUI>().text = nodeValue[i];
                }
            }
        }
    }
    
    //sets the name of the gameobject to the name of the node
    public void SetNameOfGameObject()
    {
        this.name = nodeName;
    }
    
    //Adds child to node dictionary
    public void AddChild(GameObject childNode, GameObject edgePrefab)
    {
        Debug.LogWarning("Add Child is Running!!");
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
