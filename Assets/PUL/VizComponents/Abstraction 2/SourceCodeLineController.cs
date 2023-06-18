using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using System.Linq;

namespace PUL
{
    public class SourceCodeLineController : MonoBehaviour
    {
        //assigned in GameManager in DisplayTextInPanelView
        public string blockID;
        public GameManager gameManager;
        public Interactable sourceCodeLineButton;

        private void OnClick()
        {
            Debug.Log("Destroying Previous Flow Graph");
            DestroyPreviousFlowGraph();
            Debug.Log("Generating Graph");
           // GenerateLineGraph();
        }

        public void DestroyPreviousFlowGraph()
        {
            
                foreach (GameObject line in gameManager.currentFlowGraphEdges)
                {
                    Object.Destroy(line);
                }
         gameManager.currentFlowGraphEdges.Clear(); 
            
        }
        //not working. Add in a debug statement to see what's going on. GLHF future julian
        /*
        public void GenerateLineGraph()
        {
            Debug.Log("Generating Line Graph!");
            List<GameObject> path = gameManager.totalPaths[blockID];
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.Log("Path at i is: " + path[i]);
                GameObject transformationEdge = Object.Instantiate(gameManager.transformationEdgePrefab);
                gameManager.currentFlowGraphEdges.Add(transformationEdge);

                transformationEdge.transform.GetChild(0).position = path[i].GetComponent<Node>().ExtendPointRight.transform.localPosition;
                transformationEdge.transform.GetChild(1).position = path[i + 1].GetComponent<Node>().AttachPointLeft.transform.localPosition;
            }
        }
        */

        //This function is not working correctly. Something about an object reference exception. it probably has to do with the asynchronus destruction of currentFlowGraphEdges. Fix this. GLHF, future julian.
        void UpdateEdges()
        {
            List<GameObject> path = gameManager.totalPaths[blockID];
            for (int i = 0; i < path.Count - 1; i++)
            {
                gameManager.currentFlowGraphEdges[i].transform.GetChild(0).position = path[i].GetComponent<Node>().ExtendPointRight.transform.localPosition;
                gameManager.currentFlowGraphEdges[i].transform.GetChild(1).position = path[i + 1].GetComponent<Node>().AttachPointLeft.transform.localPosition;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            sourceCodeLineButton = gameObject.GetComponent<Interactable>();
            sourceCodeLineButton.OnClick.AddListener(OnClick);
        }

        // Update is called once per frame
        void Update()
        {
            if (blockID != null && gameManager.currentFlowGraphEdges.Any())
            {
                UpdateEdges();
            }
        }
        }
    }

