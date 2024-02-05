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
        // NOTE: These values are wired up in the Unity Editor -> Menu Manager object

        public GameManager GameManager;

        // Holder for collection buttons
        public GridObjectCollection CollectionGridObjectCollection;
        public ScrollingObjectCollection CollectionScrollingObject;

        // Holder for binary buttons
        public GridObjectCollection BinaryGridObjectCollection;
        public ScrollingObjectCollection BinaryScrollingObject;

        // Holder for function buttons
        public GridObjectCollection FunctionGridObjectCollection;
        public ScrollingObjectCollection FunctionScrollingObject;

        public GameObject binaryStringsButton;

        public GameObject binaryFileStatsButton;

        public GameObject binaryCallGraphButton;

        public GameObject functionDisassemblyButton;

        public GameObject functionDecompilationButton;

        public GameObject functionControlFlowGraphButton;

        public TextMeshPro statusText;

        //refers to the menu button prefabs that will be instantiated on the menu.
        public GameObject MenuButtonPrefab;

        public GameObject ObjectButtonPrefab;

        // The Slate prefeb we instantiate for function disassembly
        public GameObject slatePrefab;

        // The UI panel
        public GameObject UIPanel;

        //refers to the graph manager
        public SpatialGraphManager graphManager;

        // END: These values are wired up in the Unity Editor -> Menu Manager object
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

        // Attempt to set the menu manager state to "busy" -- called by a long-running
        // operation when it starts. In busy state, any menu operations
        // will be ignored. Return true if successful, false otherwise.
        public bool setBusy()
        {
            // If already busy, set a reminder message and return false.
            if (isBusy)
            {
                statusText.text = busyText;
                return false;
            }

            // Since we're not busy, we can be busy. 
            isBusy = true;
            return true; // Great Success
        }

        // Unset the busy state -- called by a long-running operation when it completes. 
        public void unsetBusy()
        {
            statusText.text = defaultStatusText;
            isBusy = false;
        }

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
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.transform.localScale.x, newButton.transform.localScale.y * UIPanel.transform.localScale.y, newButton.transform.localScale.z * UIPanel.transform.localScale.z);
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

            // Set activity button callbacks. Ugly wall of repetitive code!
            PressableButtonHoloLens2 bsbuttonFunction = binaryStringsButton.GetComponent<PressableButtonHoloLens2>();
            bsbuttonFunction.TouchBegin.AddListener(() => BinaryStringsButtonCallback());
            Interactable bsdistanceInteract = binaryStringsButton.GetComponent<Interactable>();
            bsdistanceInteract.OnClick.AddListener(() => BinaryStringsButtonCallback());

            PressableButtonHoloLens2 bfbuttonFunction = binaryFileStatsButton.GetComponent<PressableButtonHoloLens2>();
            bfbuttonFunction.TouchBegin.AddListener(() => BinaryFileStatsButtonCallback());
            Interactable bfdistanceInteract = binaryFileStatsButton.GetComponent<Interactable>();
            bfdistanceInteract.OnClick.AddListener(() => BinaryFileStatsButtonCallback());
            
            PressableButtonHoloLens2 bcbuttonFunction = binaryCallGraphButton.GetComponent<PressableButtonHoloLens2>();
            bcbuttonFunction.TouchBegin.AddListener(() => BinaryCallGraphButtonCallback());
            Interactable bcdistanceInteract = binaryCallGraphButton.GetComponent<Interactable>();
            bcdistanceInteract.OnClick.AddListener(() => BinaryCallGraphButtonCallback());
            
            PressableButtonHoloLens2 fdbuttonFunction = functionDisassemblyButton.GetComponent<PressableButtonHoloLens2>();
            fdbuttonFunction.TouchBegin.AddListener(() => FunctionDisassemblyButtonCallback());
            Interactable fddistanceInteract = functionDisassemblyButton.GetComponent<Interactable>();
            fddistanceInteract.OnClick.AddListener(() => FunctionDisassemblyButtonCallback());
            
            PressableButtonHoloLens2 fd2buttonFunction = functionDecompilationButton.GetComponent<PressableButtonHoloLens2>();
            fd2buttonFunction.TouchBegin.AddListener(() => FunctionDecompilationButtonCallback());
            Interactable fd2distanceInteract = functionDecompilationButton.GetComponent<Interactable>();
            fd2distanceInteract.OnClick.AddListener(() => FunctionDecompilationButtonCallback());

            PressableButtonHoloLens2 fcbuttonFunction = functionControlFlowGraphButton.GetComponent<PressableButtonHoloLens2>();
            fcbuttonFunction.TouchBegin.AddListener(() => FunctionControlFlowGraphButtonCallback());
            Interactable fcdistanceInteract = functionControlFlowGraphButton.GetComponent<Interactable>();
            fcdistanceInteract.OnClick.AddListener(() => FunctionControlFlowGraphButtonCallback());

            unsetBusy();
            CollectionGridObjectCollection.UpdateCollection();
            initialized = true;
        }

        public async void CollectionButtonCallback(OxideCollection collection, GameObject collectionButton)
        {
            if (!setBusy()) return;

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

            // Update grid objects and scrollable regions
            // -> Binary panel
            binaryButtonDict = new Dictionary<OxideBinary, GameObject>();
            BinaryGridObjectCollection.UpdateCollection();
            BinaryScrollingObject.UpdateContent();
            // -> Function panel
            functionButtonDict = new Dictionary<OxideFunction, GameObject>();
            FunctionGridObjectCollection.UpdateCollection();
            FunctionScrollingObject.UpdateContent();

            // Update Status text at the bottom of the panels
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
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.transform.localScale.x, newButton.transform.localScale.y * UIPanel.transform.localScale.y, newButton.transform.localScale.z * UIPanel.transform.localScale.z);
                newButton.transform.name = binary.name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = createBinaryButtonText(binary);
                binaryButtonDict[binary] = newButton;

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(() => BinaryButtonCallback(binary, newButton));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => BinaryButtonCallback(binary, newButton));

                yield return new WaitForEndOfFrame();
            }
            BinaryGridObjectCollection.UpdateCollection();
            unsetBusy();
        }

        public async void BinaryButtonCallback(OxideBinary binary, GameObject binaryButton)
        {
            if (!setBusy()) return;

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

            // Update grid objects and scrolling region
            // -> Function panel
            functionButtonDict = new Dictionary<OxideFunction, GameObject>();
            FunctionGridObjectCollection.UpdateCollection();
            FunctionScrollingObject.UpdateContent();

            // Update Status text at the bottom of the panels
            statusText.text = $"Loading function info for binary {binary.name}";

            // Ensure the binary info is populated, now that it is selected
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
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.transform.localScale.x, newButton.transform.localScale.y * UIPanel.transform.localScale.y, newButton.transform.localScale.z * UIPanel.transform.localScale.z);
                newButton.transform.name = $"{function.offset} ({function.name}) : Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = createFunctionButtonText(function);
                functionButtonDict[function] = newButton;

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(() => FunctionButtonCallback(binary, function, newButton));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => FunctionButtonCallback(binary, function, newButton));

                yield return new WaitForEndOfFrame();

                if (++count > 10) break; // low limit for testing
            }

            // Update grid objects and scrolling region
            FunctionGridObjectCollection.UpdateCollection();
            FunctionScrollingObject.UpdateContent();

            unsetBusy();
        }

        public async void BinaryStringsButtonCallback()
        {
            if (!setBusy()) return;
            if (selectedBinary == null)
            {
                unsetBusy();
                statusText.text = "<color=#FF0000>Please select a binary first!";
                return;
            }
            setBusy();

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Retrieving strings for {selectedBinary.name}";

            // Get the info
            string contentString = await GameManager.nexusClient.RetrieveTextForArbitraryModule("strings", selectedBinary.oid, "{}", true);
            // string contentString = await GameManager.nexusClient.RetrieveTextForArbitraryModule("ghidra_decmap", selectedBinary.oid, "{}", false);

            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, GameManager.getSpawnPosition(), GameManager.getSpawnRotation());
            slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro contentTMP = slate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = $"Strings for {selectedBinary.name}";
            contentTMP.text = contentString;

            unsetBusy();
        }

        public async void BinaryFileStatsButtonCallback()
        {
            if (!setBusy()) return;
            if (selectedBinary == null)
            {
                unsetBusy();
                statusText.text = "<color=#FF0000>Please select a binary first!";
                return;
            }
            setBusy();

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Retrieving file stats for {selectedBinary.name}";

            // Get the info
            string contentString = await GameManager.nexusClient.RetrieveTextForArbitraryModule("file_stats", selectedBinary.oid, "{}", true);

            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, GameManager.getSpawnPosition(), GameManager.getSpawnRotation());
            slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro contentTMP = slate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = $"File stats for {selectedBinary.name}";
            contentTMP.text = contentString;

            unsetBusy();
        }

        public void BinaryCallGraphButtonCallback()
        {
            if (!setBusy()) return;
            if (selectedBinary == null)
            {
                unsetBusy();
                statusText.text = "<color=#FF0000>Please select a binary first!";
                return;
            }
            setBusy();

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Building call graph for {selectedBinary.name}";

            //bool success = await 
            GameManager.graphManager.BuildBinaryCallGraph(selectedBinary);
        }

        public void FunctionButtonCallback(OxideBinary binary, OxideFunction function, GameObject functionButton)
        {
            if (!setBusy()) return;

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

            unsetBusy();
        }

        public void FunctionDisassemblyButtonCallback()
        {
            if (!setBusy()) return;
            if (selectedFunction == null)
            {
                unsetBusy();
                statusText.text = "<color=#FF0000>Please select a function first!";
                return;
            }
            setBusy();

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Retrieving disassembly for {selectedBinary.name} / {selectedFunction.name}";

            // Build text without blocking the UI
            StartCoroutine(FunctionDisassemblyButtonCallbackCoroutine(selectedBinary, selectedFunction));
        }

        IEnumerator FunctionDisassemblyButtonCallbackCoroutine(OxideBinary binary, OxideFunction function)
        {
            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, GameManager.getSpawnPosition(), GameManager.getSpawnRotation());
            slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro contentTMP = slate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = $"{binary.name} / {function.name} Disassembly\n{function.signature}";
            contentTMP.text = "";

            // Walk through each basic block for this function and add instructions to text display
            foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
            {
                foreach (OxideInstruction instruction in basicBlock.instructionDict.Values)
                {
                    contentTMP.text += $"<color=#777777>{instruction.offset} <color=#99FF99>{instruction.mnemonic} <color=#FFFFFF>{instruction.op_str}\n";
                }
                contentTMP.text += $"<color=#000000>------------------------------------\n"; // separate blocks

                yield return new WaitForEndOfFrame(); // yield after each block instead of each instruction
            }
            
            unsetBusy();
        }

        public async void FunctionDecompilationButtonCallback()
        {
            if (!setBusy()) return;
            if (selectedFunction == null)
            {
                unsetBusy();
                statusText.text = "<color=#FF0000>Please select a function first!";
                return;
            }
            setBusy();

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
            GameObject slate = Instantiate(slatePrefab, GameManager.getSpawnPosition(), GameManager.getSpawnRotation());
            slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            TextMeshPro contentTMP = slate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = $"{binary.name} / {function.name} Decompilation";
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
                foreach (int offset in item.Value.associatedInstructionDict.Keys)
                {
                    contentTMP.text += $"<color=#AAAA00> |{offset}|";        
                }
                contentTMP.text += "\n";
                if (code.Contains('{')) indentLevel++; // Q&D indenting

                yield return new WaitForEndOfFrame(); 
            }

            unsetBusy();
        }

        public void FunctionControlFlowGraphButtonCallback()
        {
            if (!setBusy()) return;
            if (selectedFunction == null)
            {
                unsetBusy();
                statusText.text = "<color=#FF0000>Please select a function first!";
                return;
            }
            setBusy();

            // Tell the user we're doing something that won't happen instantaneously
            statusText.text = $"Building control flow graph for {selectedBinary.name} / {selectedFunction.name}";

            GameManager.graphManager.BuildFunctionControlFlowGraph(selectedFunction);

            // Uncomment this line to test the Force-Directed Graph. 
            // GameManager.graphManager.BuildFunctionControlFlowGraphFDG(selectedFunction);
        }
    }
}

