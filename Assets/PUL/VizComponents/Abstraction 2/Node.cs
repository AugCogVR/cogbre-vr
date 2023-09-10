/*
using System.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PUL
{
    public class Node : MonoBehaviour
    {
        //first value stores a parent node, the second value stores the prefab information for the second node.
        public IDictionary<GameObject, GameObject> parentNodes = new Dictionary<GameObject, GameObject>();
        //first value stores a child node, the second value stores the prefab information for the second node.
        public IDictionary<GameObject, GameObject> childNodes = new Dictionary<GameObject, GameObject>();
        
        public GameManager gameManager;
        public string nodeName;
        public GameObject AttachPoint;
        public GameObject ExtendPoint;
        public GameObject AttachPointLeft;
        public GameObject ExtendPointRight;
        public List<string> nodeValue;

        void UpdateEdges()
        {
            foreach (KeyValuePair<GameObject, GameObject> node in parentNodes)
            {
                //draws line from parent's attach point to child's extend point
                node.Value.transform.GetChild(0).position = node.Key.GetComponent<Node>().ExtendPoint.transform.position;
                node.Value.transform.GetChild(1).position = this.AttachPoint.transform.position;
            }
        }
        //displays all node information on UI text in scene. DEPENDENT ON nodeValue BEING FILLED. Also associates each GameObject with their line number, cause why not?
        //Also, we need game manager in order to store it into all of the buttons on the graph so we can access their functionality.
        public void DisplayTextOnNode(GameManager gameManager)
        {
            this.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshPro>().text = nodeName;

            if (nodeValue == null)
            {
                Debug.LogWarning("nodeValue is empty!");
            }
            else
            {
                Vector3 offset = new Vector3(0f, 0.01f, 0f);
                for (int i = 0; i < nodeValue.Count; i++)
                {
                    if (i == 0)
                    {
                        this.transform.GetChild(4).GetChild(3).gameObject.GetComponent<TextMeshPro>().text = nodeValue[0];

                        this.transform.GetChild(4).gameObject.GetComponent<GraphCodeLineController>().gameManager = gameManager;
                      
                    }
                    else
                    {
                        Transform originalGameObject = this.transform.GetChild(4);
                        GameObject newTextObject = Object.Instantiate(originalGameObject.gameObject);
                        newTextObject.GetComponent<GraphCodeLineController>().gameManager = gameManager;
                        

                        newTextObject.transform.parent = this.transform;

                        newTextObject.transform.localPosition = new Vector3(originalGameObject.localPosition.x, originalGameObject.localPosition.y - (float)(i * 0.025f), originalGameObject.localPosition.z);
                        newTextObject.transform.localScale = originalGameObject.localScale;

                        newTextObject.transform.GetChild(3).gameObject.GetComponent<TextMeshPro>().text = nodeValue[i];
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
            DisplayTextOnNode(gameManager);
        }

        void Update()
        {
            //updates based on position of all nodes every frame.
            UpdateEdges();
        }
    }
}
*/