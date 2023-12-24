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
        // NOTE: The values of these fields are wired up in the Unity Editor -> MenuManager object

        //refers to the storage of the data actively being returned from Oxide.
        public OxideData oxideData = null;
        private bool isInitialized = false;
        //refers to the (Oxide)Collection Grid Object Collection, stored inside the Scrolling Object Collection
        public GridObjectCollection CollectionGridObjectCollection;
        //refers to the same thing, but for binaries
        public GridObjectCollection BinaryGridObjectCollection;
        public GridObjectCollection FunctionGridObjectCollection;
        //refers to the menu button prefabs that will be instantiated on the menu.
        public GameObject MenuButtonPrefab;
        public GameObject ObjectButtonPrefab;
        public GameManager GameManager;
        //refers to the transform of the UI panel
        public Transform UIPanel;
        //refers to the graph manager
        public SpatialGraphManager graphManager;
        public TextMeshPro disasmTitle;
        public TextMeshPro disasmContainer;

        // Buttons for binaries will change based on collection selected, so 
        // store the active ones here so we can destroy them when a new collection is selected
        private List<GameObject> currentBinaryButtonList = new List<GameObject>();

        // Buttons for functions will change based on function selected, so 
        // store the active ones here so we can destroy them when a new binary is selected
        private List<GameObject> currentFunctionButtonList = new List<GameObject>();

        public bool initialized = false;

        public void MenuInit()
        {
            foreach (OxideCollection collection in oxideData.collectionList)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(MenuButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = CollectionGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = collection.name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = collection.name;

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchBegin.AddListener(() => CollectionButtonCallback(collection));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => CollectionButtonCallback(collection));
            }

            disasmTitle.text = "";
            disasmContainer.text = "";

            CollectionGridObjectCollection.UpdateCollection();
            initialized = true;
        }

        public async void CollectionButtonCallback(OxideCollection collection)
        {
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
            disasmTitle.text = "";
            disasmContainer.text = "";

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
        }

        public async void BinaryButtonCallback(OxideBinary binary)
        {
            // Clear buttons and text display
            foreach (GameObject button in currentFunctionButtonList)
            {
                UnityEngine.Object.Destroy(button);
            }
            FunctionGridObjectCollection.UpdateCollection();
            currentFunctionButtonList = new List<GameObject>();
            disasmTitle.text = "";
            disasmContainer.text = "";

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
        }

        public async void FunctionButtonCallback(OxideBinary binary, OxideFunction function)
        {
            disasmTitle.text = $"{binary.name} / {function.name}\n{function.signature}";

            // Tell the user we're doing something that won't happen instantaneously
            disasmContainer.text = $"Retrieving disassembly for {function.name}";

            // Build text without blocking the UI
            StartCoroutine(FunctionButtonCallbackCoroutine(binary, function));
        }

        IEnumerator FunctionButtonCallbackCoroutine(OxideBinary binary, OxideFunction function)
        {
            disasmContainer.text = "";

            // Walk through each basic block for this function and add instructions to text display
            int count = 0;
            foreach (OxideBasicBlock block in function.basicBlockDict.Values)
            {
                foreach (string instructionAddress in block.instructionAddressList)
                {
                    int addr = Int32.Parse(instructionAddress);
                    OxideInstruction insn = binary.instructionDict[addr];
                    disasmContainer.text += $"{insn.offset} {insn.instructionString}\n";
                    count++;
                }
                if (count > 100) break;  // ONLY USE FIRST FEW INSTRUCTIONS TO MAKE TESTING BEARABLE

                yield return new WaitForEndOfFrame(); // yield after each block instead of each instruction
            }
        }


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

