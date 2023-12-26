using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

namespace PUL
{
    public class MenuManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> MenuManager object

        // Holder for collection buttons
        public GridObjectCollection CollectionGridObjectCollection;
        // Holder for binary buttons
        public GridObjectCollection BinaryGridObjectCollection;
        // Holder for function buttons
        public GridObjectCollection FunctionGridObjectCollection;
        //refers to the menu button prefabs that will be instantiated on the menu.
        public GameObject MenuButtonPrefab;
        public GameObject ObjectButtonPrefab;
        public GameManager GameManager;
        //refers to the transform of the UI panel
        public Transform UIPanel;
        //refers to the graph manager
        public SpatialGraphManager graphManager;
        public TextMeshPro statusText;
        // The Slate prefeb we instantiate for function disassembly
        public GameObject slatePrefab;

        // END: These values are wired up in the Unity Editor -> MenuManager object
        // ====================================


        // refers to the storage of the data actively being returned from Oxide.
        public OxideData oxideData = null;

        // ???
        public bool initialized = false;

        // Keep a list of the collection buttons we create
        private Dictionary<OxideCollection, GameObject> collectionButtonDict = new Dictionary<OxideCollection, GameObject>();

        // Buttons for binaries will change based on collection selected, so 
        // store the active ones here so we can destroy them when a new collection is selected
        private List<GameObject> currentBinaryButtonList = new List<GameObject>();

        // Buttons for functions will change based on binary selected, so 
        // store the active ones here so we can destroy them when a new binary is selected
        private List<GameObject> currentFunctionButtonList = new List<GameObject>();

        // Keep a list of instantiated Slates
        private List<GameObject> slateList = new List<GameObject>();

        // Is the UI busy? (should we ignore button presses?)
        private bool isBusy = false;

        // Use this status text when nothing is happening
        private string defaultStatusText = "Waiting for user activity";

        // Use this status text when UI is busy
        private string busyText = "<color=\"red\">PLEASE WAIT FOR CURRENT PROCESSING TO COMPLETE";


        public void MenuInit()
        {
            foreach (OxideCollection collection in oxideData.collectionList)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(MenuButtonPrefab);
                collectionButtonDict[collection] = newButton;

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = CollectionGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = collection.name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = collection.name;

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchBegin.AddListener(() => CollectionButtonCallback(newButton, collection));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => CollectionButtonCallback(newButton, collection));
            }

            statusText.text = defaultStatusText;
            isBusy = false;
            CollectionGridObjectCollection.UpdateCollection();
            initialized = true;
        }

        public async void CollectionButtonCallback(GameObject collectionButton, OxideCollection collection)
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            isBusy = true;

            // Remove highlights from all Collection buttons and hightlight the selected button
            foreach (KeyValuePair<OxideCollection, GameObject> buttonPair in collectionButtonDict)
            {
                buttonPair.Value.GetComponentInChildren<TextMeshPro>().text = $"{buttonPair.Key.name}";
            }
            collectionButton.GetComponentInChildren<TextMeshPro>().text = $"<size=125%><color=#FFFF00>{collection.name}";
            // // DGB: This code changes the color of the button's backplate but only for a few frames
            // GameObject crap = collectionButton.transform.Find("BackPlate/Quad").gameObject;
            // crap.GetComponent<Renderer>().material.color = Color.red;

            // Clear buttons and text display
            foreach (GameObject button in currentBinaryButtonList)
            {
                UnityEngine.Object.Destroy(button);
            }
            foreach (GameObject button in currentFunctionButtonList)
            {
                UnityEngine.Object.Destroy(button);
            }
            BinaryGridObjectCollection.UpdateCollection();
            FunctionGridObjectCollection.UpdateCollection();
            currentBinaryButtonList = new List<GameObject>();
            currentFunctionButtonList = new List<GameObject>();
            statusText.text = $"Loading binary info for collection {collection.name}";

            // Ensure the collection info is populated, now that it is selected
            collection = await GameManager.nexusClient.EnsureCollectionInfo(collection);

            // Resets oid information  // ???
            // ResetBinaryInformation();

            // Build buttons without blocking the UI
            StartCoroutine(CollectionButtonCallbackCoroutine(collection.binaryList));
        }

        // Function that creates the objects that are associated with given collection
        IEnumerator CollectionButtonCallbackCoroutine(IList<OxideBinary> binaryList)
        {   
            // Create a button for each binary
            foreach (OxideBinary binary in binaryList)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(ObjectButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = BinaryGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = binary.name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = $"<size=145%><line-height=55%><b>{binary.name}</b>\n<size=60%>{binary.oid}\n<b>Size:</b> {binary.size}";
                currentBinaryButtonList.Add(newButton);

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(async () => BinaryButtonCallback(binary));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(async () => BinaryButtonCallback(binary));

                yield return new WaitForEndOfFrame();
            }
            BinaryGridObjectCollection.UpdateCollection();
            statusText.text = defaultStatusText;
            isBusy = false;
        }

        public async void BinaryButtonCallback(OxideBinary binary)
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            isBusy = true;

            // Clear buttons and text display
            foreach (GameObject button in currentFunctionButtonList)
            {
                UnityEngine.Object.Destroy(button);
            }
            FunctionGridObjectCollection.UpdateCollection();
            currentFunctionButtonList = new List<GameObject>();
            statusText.text = $"Loading function info for binary {binary.name}";

            // Ensure the collection info is populated, now that it is selected
            binary = await GameManager.nexusClient.EnsureBinaryInfo(binary);

            // Build buttons without blocking the UI
            StartCoroutine(BinaryButtonCallbackCoroutine(binary));
        }

        // Function that creates the objects that are associated with given binary
        IEnumerator BinaryButtonCallbackCoroutine(OxideBinary binary)
        {
            int count = 0;

            // Create a button for each function
            foreach (OxideFunction function in binary.functionDict.Values)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(ObjectButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = FunctionGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = function.offset + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = $"<size=145%><line-height=55%><b>{function.name}</b>\n<size=60%>{function.signature}\n<b>Offset:</b> {function.offset}";
                currentFunctionButtonList.Add(newButton);

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(async () => FunctionButtonCallback(binary, function));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(async () => FunctionButtonCallback(binary, function));

                yield return new WaitForEndOfFrame();

                if (++count > 10) break; // low limit for testing
            }
            FunctionGridObjectCollection.UpdateCollection();
            statusText.text = defaultStatusText;
            isBusy = false;
        }

        public async void FunctionButtonCallback(OxideBinary binary, OxideFunction function)
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            isBusy = true;

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Retrieving disassembly for {binary.name} / {function.name}";

            // Build text without blocking the UI
            StartCoroutine(FunctionButtonCallbackCoroutine(binary, function));
        }

        IEnumerator FunctionButtonCallbackCoroutine(OxideBinary binary, OxideFunction function)
        {
            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, new Vector3(0.82f, 0, 0.77f), Quaternion.identity);
            slateList.Add(slate);
            TextMeshPro disasmTitle = slate.transform.Find("TitleBar/DisasmTitle").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro disasmContainer = slate.transform.Find("Disasm").gameObject.GetComponent<TextMeshPro>();
            disasmTitle.text = $"{binary.name} / {function.name}\n{function.signature}";
            disasmContainer.text = "";

            // Walk through each basic block for this function and add instructions to text display
            int count = 0;
            foreach (OxideBasicBlock block in function.basicBlockDict.Values)
            {
                foreach (string instructionAddress in block.instructionAddressList)
                {
                    int addr = Int32.Parse(instructionAddress);
                    OxideInstruction insn = binary.instructionDict[addr];
                    disasmContainer.text += $"<color=#777777>{insn.offset} <color=#99FF99>{insn.mnemonic} <color=#FFFFFF>{insn.op_str}\n";
                    count++;
                }
                disasmContainer.text += $"<color=#000000>------------------------------------\n"; // separate blocks
                if (count > 100) break;  // ONLY USE FIRST FEW INSTRUCTIONS TO MAKE TESTING BEARABLE

                yield return new WaitForEndOfFrame(); // yield after each block instead of each instruction
            }
            statusText.text = defaultStatusText;
            isBusy = false;
        }


        // DGB: OLD DEAD CODE KEPT FOR REFERENCE ONLY
        // // Sets information about an oid and builds a graph
        // public async void BinaryButtonCallbackOLD(OxideBinary binary)
        // {
        //     // // Reset OID information  // ???
        //     // ResetBinaryInformation();

        //     // DGB: Commented out for now -- we'll re-look at 2D or 3D graphs later
        //     // Builds a graph based on information contained
        //     // -> NOTE! CURRENTLY GENERATES A RANDOM GRAPH
        //     // graphManager.CreateGraph(binary);

        //     // Tell the user we're doing something that won't happen instantaneously
        //     disasmContainer.text = $"Retrieving disassembly for {binary.name}";

        //     // Ensure we have all the info for this binary. 
        //     binary = await GameManager.nexusClient.EnsureBinaryInfo(binary);

        //     // Set the text. This is SLOW so make it a coroutine. 
        //     StartCoroutine(BinaryButtonCallbackCoroutineOLD(binary.instructionDict));
        // }

        // // Coroutine to put the disassembly text into the container. Out of all the data
        // // pulling and moving and processing going on here, THIS is what takes the longest. 
        // // It goes faster using StringBuilder BUT!!! the whole UI locks up for a couple of seconds
        // // during that final "sb.ToString()" operation. Ugh! 
        // IEnumerator BinaryButtonCallbackCoroutineOLD(SortedDictionary<int, OxideInstruction> instructionDict)
        // {
        //     // var sb = new System.Text.StringBuilder(); // StringBuilder approach is commented out but left for reference
        //     disasmContainer.text = "";
        //     if (instructionDict != null)
        //     {
        //         int count = 0;
        //         foreach (KeyValuePair<int, OxideInstruction> item in instructionDict)
        //         {
        //             // sb.AppendLine(item.Key + " " + item.Value);
        //             disasmContainer.text += $"{item.Key} {item.Value.instructionString}\n";
        //             if (++count > 100) break;  // ONLY USE SOME INSTRUCTIONS TO MAKE TESTING BEARABLE
        //             yield return new WaitForEndOfFrame();
        //         }
        //     }
        //     else 
        //     {
        //         // sb.AppendLine("null... Check for 500 error.");
        //         disasmContainer.text += "null... Check for 500 error.";
        //     }
        //     // disasmContainer.text = sb.ToString();
        // }

        // void ResetBinaryInformation()
        // {
        //     // Resets current graph
        //     graphManager.DisableGraphs();
        // }
    }
}

