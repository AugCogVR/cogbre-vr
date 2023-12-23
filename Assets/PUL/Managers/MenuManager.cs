using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

// CONVENTION IN THIS FILE:
// CID or cid = oxide collection
// OID or oid = oxide binary

namespace PUL
{
    public class MenuManager : MonoBehaviour
    {
        //refers to the storage of the data actively being returned from Oxide.
        public OxideData oxideData = null;
        private bool isInitialized = false;
        //refers to the CID Grid Object Collection, stored inside the Scrolling Object Collection
        public GridObjectCollection CIDGridObjectCollection;
        //refers to the same thing, but for OIDs
        public GridObjectCollection OIDGridObjectCollection;
        //refers to the menu button prefabs that will be instantiated on the menu.
        public GameObject MenuButtonPrefab;
        public GameObject ObjectButtonPrefab;
        public GameManager GameManager;
        //refers to the transform of the UI panel
        public Transform UIPanel;
        //refers to the graph manager
        public SpatialGraphManager graphManager;

        public TextMeshPro disasmContainer;

        //store all active buttons in a list
        private List<GameObject> activeOIDButtons = new List<GameObject>();

        public bool initialized = false;

        // DEBUG GRAPH
       public void MenuInit()
        {
            foreach (OxideCollection collection in oxideData.collectionList)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(MenuButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = CIDGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = collection.name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = collection.name;

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchBegin.AddListener(() => BuildButton(collection));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => BuildButton(collection));
            }

            CIDGridObjectCollection.UpdateCollection();
            initialized = true;
        }

        public async void BuildButton(OxideCollection collection)
        {
            // Get binary list for this collection. Must use this method instead of a normal getter due to ... reasons.
            collection = await GameManager.nexusClient.EnsureCollectionInfo(collection);

            // Resets oid information  // ???
            // ResetBinaryInformation();

            // Builds Button
            StartCoroutine(BuildButtonEnum(collection.binaryList));
        }

        // Function that creates the objects that are associated with given collection
        IEnumerator BuildButtonEnum(IList<OxideBinary> binaryList)
        {   
            //destroy all active buttons
            foreach (GameObject button in activeOIDButtons)
            {
                Object.Destroy(button);
            }
            //clear list space
            activeOIDButtons = new List<GameObject>();

            foreach (OxideBinary binary in binaryList)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(ObjectButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = OIDGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = binary.name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = $"<size=145%><line-height=55%><b>{binary.name}</b>\n<size=60%>{binary.oid}\n<b>Size:</b> {binary.size}";

                activeOIDButtons.Add(newButton);

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(async () => SetBinaryInformation(binary));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(async () => SetBinaryInformation(binary));

                yield return new WaitForEndOfFrame();
            }

            OIDGridObjectCollection.UpdateCollection();
        }

        // Sets information about an oid and builds a graph
        public async void SetBinaryInformation(OxideBinary binary)
        {
            // // Reset OID information  // ???
            // ResetBinaryInformation();

            // DGB: Commented out for now -- we'll re-look at 2D or 3D graphs later
            // Builds a graph based on information contained
            // -> NOTE! CURRENTLY GENERATES A RANDOM GRAPH
            // graphManager.CreateGraph(binary);

            // Tell the user we're doing something that won't happen instantaneously
            disasmContainer.text = $"Retrieving disassembly for {binary.name}";

            // Ensure we have all the info for this binary. 
            binary = await GameManager.nexusClient.EnsureBinaryInfo(binary);

            // Set the text. This is SLOW so make it a coroutine. 
            StartCoroutine(SetAllDisassemblyText(binary.instructionDict));
        }

        IEnumerator SetAllDisassemblyText(Dictionary<string, OxideInstruction> instructionDict)
        {
            // var sb = new System.Text.StringBuilder(); // StringBuilder approach is commented out but left for reference
            disasmContainer.text = "";
            if (instructionDict != null)
            {
                int count = 0;
                foreach (KeyValuePair<string, OxideInstruction> item in instructionDict)
                {
                    // sb.AppendLine(item.Key + " " + item.Value);
                    disasmContainer.text += item.Key + " " + item.Value.instructionString + "\n";
                    if (++count > 100) break;  // ONLY USE SOME INSTRUCTIONS TO MAKE TESTING BEARABLE
                    yield return new WaitForEndOfFrame();
                }
            }
            else 
            {
                // sb.AppendLine("null... Check for 500 error.");
                disasmContainer.text += "null... Check for 500 error.";
            }
            // disasmContainer.text = sb.ToString();
        }

        // void ResetBinaryInformation()
        // {
        //     // Resets current graph
        //     graphManager.DisableGraphs();
        // }
    }
}

