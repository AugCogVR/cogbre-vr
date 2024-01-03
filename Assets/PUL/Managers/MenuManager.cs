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

        public GameManager GameManager;

        // Holder for collection buttons
        public GridObjectCollection CollectionGridObjectCollection;

        // Holder for binary buttons
        public GridObjectCollection BinaryGridObjectCollection;

        // Holder for function buttons
        public GridObjectCollection FunctionGridObjectCollection;

        public GameObject binaryStringsButton;

        public GameObject binaryFilestatsButton;

        public GameObject functionDisassemblyButton;

        public GameObject functionDecompilationButton;

        public TextMeshPro statusText;

        //refers to the menu button prefabs that will be instantiated on the menu.
        public GameObject MenuButtonPrefab;

        public GameObject ObjectButtonPrefab;

        // The Slate prefeb we instantiate for function disassembly
        public GameObject slatePrefab;

        //refers to the transform of the UI panel
        public Transform UIPanel;

        //refers to the graph manager
        public SpatialGraphManager graphManager;

        // END: These values are wired up in the Unity Editor -> MenuManager object
        // ====================================


        // refers to the storage of the data actively being returned from Oxide.
        public OxideData oxideData = null;

        // ???
        public bool initialized = false;

        // Track the collection buttons we create
        private Dictionary<OxideCollection, GameObject> collectionButtonDict = new Dictionary<OxideCollection, GameObject>();

        // Track the binary buttons we create
        private Dictionary<OxideBinary, GameObject> binaryButtonDict = new Dictionary<OxideBinary, GameObject>();

        // Track the function buttons we create
        private Dictionary<OxideFunction, GameObject> functionButtonDict = new Dictionary<OxideFunction, GameObject>();

        // Keep a list of instantiated Slates
        private List<GameObject> slateList = new List<GameObject>();

        // Which Collection is selected?
        private OxideCollection selectedCollection = null;

        // Which Binary is selected?
        private OxideBinary selectedBinary = null;

        // Which Function is selected?
        private OxideFunction selectedFunction = null;

        // Is the UI busy? (should we ignore button presses?)
        private bool isBusy = false;

        // Use this status text when nothing is happening
        private string defaultStatusText = "Waiting for user activity";

        // Use this status text when UI is busy
        private string busyText = "<color=\"red\">PLEASE WAIT FOR CURRENT PROCESSING TO COMPLETE";


        private string createCollectionButtonText(OxideCollection collection)
        {
            return $"{collection.name}";        
        }

        private string createBinaryButtonText(OxideBinary binary)
        {
            return $"<size=145%><line-height=55%><b>{binary.name}</b>\n<size=60%>{binary.oid}\n<b>Size:</b> {binary.size}";
        }

        private string createFunctionButtonText(OxideFunction function)
        {
            return $"<size=145%><line-height=55%><b>{function.name}</b>\n<size=60%>{function.signature}\n<b>Offset:</b> {function.offset}";
        }

        public void MenuInit()
        {
            // Build GridObjectCollection for the user to select an OxideCollection
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
                newButton.GetComponentInChildren<TextMeshPro>().text = createCollectionButtonText(collection);

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchBegin.AddListener(() => CollectionButtonCallback(collection, newButton));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => CollectionButtonCallback(collection, newButton));
            }

            // Set activity button callbacks
            PressableButtonHoloLens2 bsbuttonFunction = binaryStringsButton.GetComponent<PressableButtonHoloLens2>();
            bsbuttonFunction.TouchBegin.AddListener(() => BinaryStringsButtonCallback());
            Interactable bsdistanceInteract = binaryStringsButton.GetComponent<Interactable>();
            bsdistanceInteract.OnClick.AddListener(() => BinaryStringsButtonCallback());
            PressableButtonHoloLens2 bfbuttonFunction = binaryFilestatsButton.GetComponent<PressableButtonHoloLens2>();
            bfbuttonFunction.TouchBegin.AddListener(() => BinaryFilestatsButtonCallback());
            Interactable bfdistanceInteract = binaryFilestatsButton.GetComponent<Interactable>();
            bfdistanceInteract.OnClick.AddListener(() => BinaryFilestatsButtonCallback());
            PressableButtonHoloLens2 fdbuttonFunction = functionDisassemblyButton.GetComponent<PressableButtonHoloLens2>();
            fdbuttonFunction.TouchBegin.AddListener(() => FunctionDisassemblyButtonCallback());
            Interactable fddistanceInteract = functionDisassemblyButton.GetComponent<Interactable>();
            fddistanceInteract.OnClick.AddListener(() => FunctionDisassemblyButtonCallback());
            PressableButtonHoloLens2 fd2buttonFunction = functionDecompilationButton.GetComponent<PressableButtonHoloLens2>();
            fd2buttonFunction.TouchBegin.AddListener(() => FunctionDecompilationButtonCallback());
            Interactable fd2distanceInteract = functionDecompilationButton.GetComponent<Interactable>();
            fd2distanceInteract.OnClick.AddListener(() => FunctionDecompilationButtonCallback());

            statusText.text = defaultStatusText;
            isBusy = false;
            CollectionGridObjectCollection.UpdateCollection();
            initialized = true;
        }

        public async void CollectionButtonCallback(OxideCollection collection, GameObject collectionButton)
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            isBusy = true;

            // Set the selected collection, binary, function
            selectedCollection = collection;
            selectedBinary = null;
            selectedFunction = null;

            // Remove highlights from all Collection buttons and hightlight the selected button
            foreach (KeyValuePair<OxideCollection, GameObject> buttonPair in collectionButtonDict)
            {
                buttonPair.Value.GetComponentInChildren<TextMeshPro>(true).text = createCollectionButtonText(buttonPair.Key);
            }
            collectionButton.GetComponentInChildren<TextMeshPro>().text = $"<size=125%><color=#FFFF00>{createCollectionButtonText(collection)}";
            // // DGB: This code changes the color of the button's backplate but only for a few frames
            // GameObject crap = collectionButton.transform.Find("BackPlate/Quad").gameObject;
            // crap.GetComponent<Renderer>().material.color = Color.red;

            // Clear buttons and text display
            foreach (GameObject button in binaryButtonDict.Values)
            {
                UnityEngine.Object.Destroy(button);
            }
            foreach (GameObject button in functionButtonDict.Values)
            {
                UnityEngine.Object.Destroy(button);
            }
            binaryButtonDict = new Dictionary<OxideBinary, GameObject>();
            functionButtonDict = new Dictionary<OxideFunction, GameObject>();
            BinaryGridObjectCollection.UpdateCollection();
            FunctionGridObjectCollection.UpdateCollection();
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
                binaryButtonDict[binary] = newButton;

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = BinaryGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = binary.name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = createBinaryButtonText(binary);
                binaryButtonDict[binary] = newButton;

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(async () => BinaryButtonCallback(binary, newButton));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(async () => BinaryButtonCallback(binary, newButton));

                yield return new WaitForEndOfFrame();
            }
            BinaryGridObjectCollection.UpdateCollection();
            statusText.text = defaultStatusText;
            isBusy = false;
        }

        public async void BinaryButtonCallback(OxideBinary binary, GameObject binaryButton)
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            isBusy = true;

            // Set the selected binary and function
            selectedBinary = binary;
            selectedFunction = null;

            // Remove highlights from all Binary buttons and hightlight the selected button
            foreach (KeyValuePair<OxideBinary, GameObject> buttonPair in binaryButtonDict)
            {
                // Need "true" argument to include inactive components -- the TMP is inactive for some reason
                // TODO: Care more about why this is the case
                buttonPair.Value.GetComponentInChildren<TextMeshPro>(true).text = createBinaryButtonText(buttonPair.Key);
                // NOTE: alternate method to find and set TMP, active or not
                // TextMeshPro label = buttonPair.Value.transform.Find("TextMeshPro").gameObject.GetComponent<TextMeshPro>();
                // label.text = createBinaryButtonText(buttonPair.Key);
            }
            binaryButton.GetComponentInChildren<TextMeshPro>().text = $"<color=#FFFF00>{createBinaryButtonText(binary)}";

            // Clear buttons and text display
            foreach (GameObject button in functionButtonDict.Values)
            {
                UnityEngine.Object.Destroy(button);
            }
            functionButtonDict = new Dictionary<OxideFunction, GameObject>();
            FunctionGridObjectCollection.UpdateCollection();
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
                newButton.GetComponentInChildren<TextMeshPro>().text = createFunctionButtonText(function);
                functionButtonDict[function] = newButton;

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(async () => FunctionButtonCallback(binary, function, newButton));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(async () => FunctionButtonCallback(binary, function, newButton));

                yield return new WaitForEndOfFrame();

                if (++count > 10) break; // low limit for testing
            }
            FunctionGridObjectCollection.UpdateCollection();
            statusText.text = defaultStatusText;
            isBusy = false;
        }

        public async void BinaryStringsButtonCallback()
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            if (selectedBinary == null)
            {
                statusText.text = "<color=#FF0000>Please select a binary first!";
                return;
            }
            isBusy = true;

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Retrieving strings for {selectedBinary.name}";

            // Get the info
            string contentString = await GameManager.nexusClient.RetrieveTextForArbitraryModule("strings", selectedBinary.oid, "{}", true);
            // string contentString = await GameManager.nexusClient.RetrieveTextForArbitraryModule("ghidra_decmap", selectedBinary.oid, "{}", false);

            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, new Vector3(0.82f, 0, 0.77f), Quaternion.identity);
            slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro contentTMP = slate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = $"Strings for {selectedBinary.name}";
            contentTMP.text = contentString;

            statusText.text = defaultStatusText;
            isBusy = false;
        }

        public async void BinaryFilestatsButtonCallback()
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            if (selectedBinary == null)
            {
                statusText.text = "<color=#FF0000>Please select a binary first!";
                return;
            }
            isBusy = true;

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Retrieving file stats for {selectedBinary.name}";

            // Get the info
            string contentString = await GameManager.nexusClient.RetrieveTextForArbitraryModule("file_stats", selectedBinary.oid, "{}", true);

            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, new Vector3(0.82f, 0, 0.77f), Quaternion.identity);
            slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro contentTMP = slate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = $"File stats for {selectedBinary.name}";
            contentTMP.text = contentString;

            statusText.text = defaultStatusText;
            isBusy = false;
        }

        public async void FunctionButtonCallback(OxideBinary binary, OxideFunction function, GameObject functionButton)
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            isBusy = true;

            // Set the selected function
            selectedFunction = function;

            // Remove highlights from all Function buttons and hightlight the selected button
            foreach (KeyValuePair<OxideFunction, GameObject> buttonPair in functionButtonDict)
            {
                // Need "true" argument to include inactive components -- the TMP is inactive for some reason
                // TODO: Care more about why this is the case
                buttonPair.Value.GetComponentInChildren<TextMeshPro>(true).text = createFunctionButtonText(buttonPair.Key);
            }
            functionButton.GetComponentInChildren<TextMeshPro>().text = $"<color=#FFFF00>{createFunctionButtonText(function)}";

            isBusy = false;
        }

        public async void FunctionDisassemblyButtonCallback()
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            if (selectedFunction == null)
            {
                statusText.text = "<color=#FF0000>Please select a function first!";
                return;
            }
            isBusy = true;

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Retrieving disassembly for {selectedBinary.name} / {selectedFunction.name}";

            // Build text without blocking the UI
            StartCoroutine(FunctionDisassemblyButtonCallbackCoroutine(selectedBinary, selectedFunction));
        }

        IEnumerator FunctionDisassemblyButtonCallbackCoroutine(OxideBinary binary, OxideFunction function)
        {
            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, new Vector3(0.82f, 0, 0.77f), Quaternion.identity);
            slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro contentTMP = slate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = $"{binary.name} / {function.name}\n{function.signature}";
            contentTMP.text = "";

            // Walk through each basic block for this function and add instructions to text display
            // int count = 0;
            foreach (OxideBasicBlock block in function.basicBlockDict.Values)
            {
                foreach (string instructionAddress in block.instructionAddressList)
                {
                    int addr = Int32.Parse(instructionAddress);
                    OxideInstruction insn = binary.instructionDict[addr];
                    contentTMP.text += $"<color=#777777>{insn.offset} <color=#99FF99>{insn.mnemonic} <color=#FFFFFF>{insn.op_str}\n";
                    // count++;
                }
                contentTMP.text += $"<color=#000000>------------------------------------\n"; // separate blocks
                // if (count > 100) break;  // ONLY USE FIRST FEW INSTRUCTIONS TO MAKE TESTING BEARABLE

                yield return new WaitForEndOfFrame(); // yield after each block instead of each instruction
            }
            statusText.text = defaultStatusText;
            isBusy = false;
        }

        public async void FunctionDecompilationButtonCallback()
        {
            if (isBusy)
            {
                statusText.text = busyText;
                return;
            }
            if (selectedFunction == null)
            {
                statusText.text = "<color=#FF0000>Please select a function first!";
                return;
            }
            isBusy = true;

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Retrieving decompilation for {selectedBinary.name} / {selectedFunction.name}";

            // Ensure we have the decompilation for this binary
            selectedBinary = await GameManager.nexusClient.EnsureBinaryDecompilation(selectedBinary);

            // Build text without blocking the UI
            StartCoroutine(FunctionDecompilationButtonCallbackCoroutine(selectedBinary, selectedFunction));
        }

        IEnumerator FunctionDecompilationButtonCallbackCoroutine(OxideBinary binary, OxideFunction function)
        {
            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, new Vector3(0.82f, 0, 0.77f), Quaternion.identity);
            slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro contentTMP = slate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = $"{function.name} Decompilation";
            contentTMP.text = "";

            // Walk through decompilation and create text display
            int indentLevel = 0;
            foreach (KeyValuePair<int, OxideDecompLine> item in function.decompDict)
            {
                string code = item.Value.code;
                if (code.Contains('}')) indentLevel--; // Q&D indenting
                contentTMP.text += $"<color=#777777>{item.Key}: ";
                for (int i = 0; i < indentLevel; i++) contentTMP.text += "    ";  // Q&D indenting
                contentTMP.text += $"<color=#FFFFFF>{code}";
                if (item.Value.associatedOffsets != null)
                {
                    foreach (int offset in item.Value.associatedOffsets)
                    {
                        contentTMP.text += $"<color=#AAAA00> |{offset}|";        
                    }
                }
                contentTMP.text += "\n";
                if (code.Contains('{')) indentLevel++; // Q&D indenting

                yield return new WaitForEndOfFrame(); 
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
        //     contentTMP.text = $"Retrieving disassembly for {binary.name}";

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
        //     contentTMP.text = "";
        //     if (instructionDict != null)
        //     {
        //         int count = 0;
        //         foreach (KeyValuePair<int, OxideInstruction> item in instructionDict)
        //         {
        //             // sb.AppendLine(item.Key + " " + item.Value);
        //             contentTMP.text += $"{item.Key} {item.Value.instructionString}\n";
        //             if (++count > 100) break;  // ONLY USE SOME INSTRUCTIONS TO MAKE TESTING BEARABLE
        //             yield return new WaitForEndOfFrame();
        //         }
        //     }
        //     else 
        //     {
        //         // sb.AppendLine("null... Check for 500 error.");
        //         contentTMP.text += "null... Check for 500 error.";
        //     }
        //     // contentTMP.text = sb.ToString();
        // }

        // void ResetBinaryInformation()
        // {
        //     // Resets current graph
        //     graphManager.DisableGraphs();
        // }
    }
}

