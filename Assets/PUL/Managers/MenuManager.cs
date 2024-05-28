using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;
using static Microsoft.MixedReality.Toolkit.Experimental.UI.KeyboardKeyFunc;

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
        public TextMeshPro CollectionSelectionText;

        // Holder for binary buttons
        public GridObjectCollection BinaryGridObjectCollection;
        public ScrollingObjectCollection BinaryScrollingObject;
        public TextMeshPro BinarySelectionText;

        // Holder for function buttons
        public GridObjectCollection FunctionGridObjectCollection;
        public ScrollingObjectCollection FunctionScrollingObject;
        public TextMeshPro FunctionSelectionText;

        public GameObject binaryProgressIndicator;
        public GameObject functionProgressIndicator;


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

        // The Tooltip prefeb we instantiate for function disassembly
        public GameObject tooltipPrefab;

        // The UI panel
        public GameObject UIPanel;

        //refers to the graph manager
        // public SpatialGraphManager graphManager;

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

        // Helper function to make a slate with common characteristics. 
        private GameObject makeASlate(string title, string contents)
        {
            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, GameManager.getSpawnPosition(), GameManager.getSpawnRotation());
            // slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = title;

            // Grab the content TMP
            TextMeshProUGUI contentTMP = slate.GetComponentInChildren<TextMeshProUGUI>();
            contentTMP.text = "";

            // -> Pulls and Sets information regarding the input field
            // Used for highlighting
            TMP_InputField inField = contentTMP.GetComponent<TMP_InputField>();
            inField.text = contentTMP.text;
            int numLines = contents.Split('\n').Length - 1;
            inField.text = contents;
            contentTMP.rectTransform.sizeDelta = new Vector2(contentTMP.rectTransform.sizeDelta.x, numLines * (contentTMP.fontSize + 1.5f));

            // Wire up copy button
            DynamicScrollbarHandler dynamicScrollbarHandler = slate.GetComponentInChildren<DynamicScrollbarHandler>();
            GameObject copyButton = slate.transform.Find("TitleBar/Buttons/CopyButton").gameObject;
            PressableButtonHoloLens2 buttonFunction = copyButton.GetComponent<PressableButtonHoloLens2>();
            buttonFunction.TouchBegin.AddListener(() => GameManager.textManager.TextCopyCallback(dynamicScrollbarHandler));
            Interactable distanceInteract = copyButton.GetComponent<Interactable>();
            distanceInteract.OnClick.AddListener(() => GameManager.textManager.TextCopyCallback(dynamicScrollbarHandler));

            return slate;
        }

        private GameObject makeAToolTip(string title, string contents, GameObject parentSlate)
        {
            // Make a new tooltip
            GameObject tooltip = Instantiate(tooltipPrefab, Vector3.zero, GameManager.getSpawnRotation(), parentSlate.transform);
            tooltip.name = "Tooltip_" + contents.Substring(0, 5);
            tooltip.transform.localPosition = Vector3.left * 0.15f;

            // Push in contents
            ToolTip ttContents = tooltip.GetComponent<ToolTip>();
            ttContents.ToolTipText = $"<color=yellow><u><b>{title}</b></u></color>\n{contents}";
            ttContents.FontSize = 45;

            // Point to the slate
            ToolTipConnector ttConnector = tooltip.GetComponent<ToolTipConnector>();
            ttConnector.Target = parentSlate;

            return tooltip;
        }

        private string createCollectionButtonText(OxideCollection collection)
        {
            return $"{collection.name}";        
        }

        private string createBinaryButtonText(OxideBinary binary)
        {
            return $"<size=145%><line-height=55%><b>{binary.name}</b>\n<size=55%>{binary.oid}\n<size=65%><b>Size:</b> {binary.size}";
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
            CollectionSelectionText.text = "none";
            BinarySelectionText.text = "none";
            FunctionSelectionText.text = "none";

            initialized = true;
        }

        public async void CollectionButtonCallback(OxideCollection collection, GameObject collectionButton)
        {

            if (!setBusy()) return;

            // Set the selected collection, binary, function
            selectedCollection = collection;
            CollectionSelectionText.text = collection.name;
            selectedBinary = null;
            BinarySelectionText.text = "none";
            selectedFunction = null;
            FunctionSelectionText.text = "none";

            // Remove highlights from all Collection buttons
            foreach (KeyValuePair<OxideCollection, GameObject> buttonPair in collectionButtonDict)
            {
                buttonPair.Value.GetComponentInChildren<TextMeshPro>(true).text = createCollectionButtonText(buttonPair.Key);
            }
            // Hightlight the selected button
            collectionButton.GetComponentInChildren<TextMeshPro>().text = $"<size=125%><color=#FFFF00>{createCollectionButtonText(collection)}";

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

            //set binary loading icon active
            binaryProgressIndicator.SetActive(true);
            // Ensure the collection info is populated, now that it is selected
            collection = await GameManager.nexusClient.EnsureCollectionInfo(collection);

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
            BinaryScrollingObject.UpdateContent();
            binaryProgressIndicator.SetActive(false);
            unsetBusy();
        }

        public async void BinaryButtonCallback(OxideBinary binary, GameObject binaryButton)
        {
            if (!setBusy()) return;

            // Set the selected binary and function
            selectedBinary = binary;
            BinarySelectionText.text = binary.name;
            selectedFunction = null;
            FunctionSelectionText.text = "none";

            // Remove highlights from all Binary buttons
            foreach (KeyValuePair<OxideBinary, GameObject> buttonPair in binaryButtonDict)
            {
                // Need "true" argument to include inactive components -- the TMP is inactive for some reason
                // TODO: Care more about why this is the case
                buttonPair.Value.GetComponentInChildren<TextMeshPro>(true).text = createBinaryButtonText(buttonPair.Key);
                // NOTE: alternate method to find and set TMP, active or not
                // TextMeshPro label = buttonPair.Value.transform.Find("TextMeshPro").gameObject.GetComponent<TextMeshPro>();
                // label.text = createBinaryButtonText(buttonPair.Key);
            }
            // Hightlight the selected button
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

            //make loading icon appear
            functionProgressIndicator.SetActive(true);
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
            functionProgressIndicator.SetActive(false);
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
            string contents = await GameManager.nexusClient.RetrieveTextForArbitraryModule("strings", selectedBinary.oid, "{}", true);

            // Make a new slate
            GameObject slate = makeASlate($"Strings for {selectedBinary.name}", contents);

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
            string contents = await GameManager.nexusClient.RetrieveTextForArbitraryModule("file_stats", selectedBinary.oid, "{}", true);

            // Make a new slate
            GameObject slate = makeASlate($"File stats for {selectedBinary.name}", contents);

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
            FunctionSelectionText.text = function.name;

            // Remove highlights from all Function buttons
            foreach (KeyValuePair<OxideFunction, GameObject> buttonPair in functionButtonDict)
            {
                // Need "true" argument to include inactive components -- the TMP is inactive for some reason
                // TODO: Care more about why this is the case
                buttonPair.Value.GetComponentInChildren<TextMeshPro>(true).text = createFunctionButtonText(buttonPair.Key);
            }

            // Hightlight the selected button
            if (functionButton != null) // TODO: Find and highlight selected button without needing to have it passed in as a parameter
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
            GameObject slate = makeASlate($"{binary.name} / {function.name} Disassembly\n{function.signature}", "");

            // Grab the content TMP
            TextMeshProUGUI contentTMP = slate.GetComponentInChildren<TextMeshProUGUI>();

            // -> Pulls and Sets information regarding the input field
            // Used for highlighting
            TMP_InputField inField = contentTMP.GetComponent<TMP_InputField>();
            inField.text = contentTMP.text;

            // -> Keeps track of the total number of lines, used for sizing text field for scroll rect.
            float contentSize = 0;
            float fontBuffer = 1.5f;
            
            // Walk through each basic block for this function and add instructions to text display
            foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
            {
                foreach (OxideInstruction instruction in basicBlock.instructionDict.Values)
                {
                    inField.text += $"<color=#777777>{instruction.offset} <color=#99FF99>{instruction.mnemonic} <color=#FFFFFF>{instruction.op_str}\n";
                    contentSize += contentTMP.fontSize + fontBuffer; 
                }
                inField.text += $"<color=#000000>------------------------------------\n"; // separate blocks
                contentSize += contentTMP.fontSize + fontBuffer;

                yield return new WaitForEndOfFrame(); // yield after each block instead of each instruction
            }

            // Adjust TMP transform to match content height
            contentTMP.rectTransform.sizeDelta = new Vector2(contentTMP.rectTransform.sizeDelta.x, contentSize);

            // Write to capa output
            FunctionCapaOutput(function, slate);

            unsetBusy();
        }

        // Outputs the capa_results call to a slate, used for better visualization of the data we are working with
        // -> Just using void right now while in the testing phases
        void FunctionCapaOutput(OxideFunction function, GameObject tooltipParent)
        {
            // Check to see if a slate should be made
            if (function.capaList.Count <= 0)
                return;

            // Log contents in capalist
            string contents = "";

            // Pull Capa information and spit out into the slate
            foreach (string capaOut in function.capaList)
                contents += capaOut;

            // Make a new tooltip
            GameObject tooltip = makeAToolTip("Capa Results", contents, tooltipParent);
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
            GameObject slate = makeASlate($"{binary.name} / {function.name} Decompilation", "");

            // Grab the content TMP
            TextMeshProUGUI contentTMP = slate.GetComponentInChildren<TextMeshProUGUI>();
            contentTMP.text = "";

            // -> Pulls and Sets information regarding the input field
            // Used for highlighting
            TMP_InputField inField = contentTMP.GetComponent<TMP_InputField>();
            inField.text = contentTMP.text;

            // -> Keeps track of the total number of lines, used for sizing text field for scroll rect.
            float contentSize = 0;
            float fontBuffer = 1.5f;

            // Walk through decompilation and create text display
            int indentLevel = 0;
            foreach (KeyValuePair<int, OxideDecompLine> item in function.decompDict)
            {
                string code = item.Value.code;
                if (code.Contains('}')) indentLevel--; // Q&D indenting
                inField.text += $"<color=#777777>{item.Key}: ";
                for (int i = 0; i < indentLevel; i++) inField.text += "    ";  // Q&D indenting
                inField.text += $"<color=#FFFFFF>{code}";
                foreach (int offset in item.Value.associatedInstructionDict.Keys)
                {
                    inField.text += $"<color=#AAAA00> |{offset}|";        
                }
                inField.text += "\n";
                contentSize += contentTMP.fontSize + fontBuffer;
                if (code.Contains('{')) indentLevel++; // Q&D indenting

                yield return new WaitForEndOfFrame(); 
            }

            // Adjust TMP transform to match content height
            contentTMP.rectTransform.sizeDelta = new Vector2(contentTMP.rectTransform.sizeDelta.x, contentSize);

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




        // HUGE BLOCK OF COMMENTED-OUT CODE BELOW: Generates a slate full of buttons, one button per token,
        // for a disassembly listing. It's terribly inefficient and tanks the frame rate but it does
        // let us select individual tokens in the text. 
        // Assuming we can get the text input field approach working, this code will never 
        // be live again. 

        // // HELPER FUNCTION FOR FunctionDisassemblyButtonCallbackCoroutine_ALT
        // // Make a button with the provided token string (and color) using the given prefab. 
        // // Size the button to fit the provided token. Attach the button to the given Slate
        // // and Container. Use and update the offset values that determine button placement relative 
        // // to the Slate object. 
        // GameObject MakeTokenButton(string token, string color, GameObject tokenButtonPrefab,
        //                            GameObject tokenButtonSlate, GameObject tokenButtonsContainer,
        //                            ref float xOffset, float yOffset, float zOffset, ref float maxYSize)
        // {
        //     // Make a new token button
        //     GameObject newButton = Instantiate(tokenButtonPrefab);
        //     newButton.transform.SetParent(tokenButtonsContainer.transform);

        //     // Find preferred TMP size for this token
        //     TMP_Text tmp = newButton.GetComponentInChildren<TMP_Text>(); 
        //     Vector2 prefTextSize = tmp.GetPreferredValues(token);
        //     float xSize = prefTextSize.x * 1.2f;
        //     float ySize = prefTextSize.y * 1.2f;

        //     // Resize a bunch of stuff in the button. This sucks but I'm too dumb to find a more elegant way.
        //     tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(xSize, prefTextSize.y);
        //     GameObject buttonVis = newButton.transform.Find("CompressableButtonVisuals").gameObject;
        //     buttonVis.transform.localScale = new Vector3(xSize, buttonVis.transform.localScale.y, buttonVis.transform.localScale.z);
        //     GameObject backplate = newButton.transform.Find("BackPlate").gameObject;
        //     backplate.transform.localScale = new Vector3(xSize, backplate.transform.localScale.y, backplate.transform.localScale.z);
        //     BoxCollider buttonCollider = newButton.GetComponent<BoxCollider>();
        //     buttonCollider.size = new Vector3(xSize, buttonCollider.size.y, buttonCollider.size.z);

        //     // Set the text and update mesh
        //     tmp.text = $"<color={color}>{token}";
        //     tmp.ForceMeshUpdate();

        //     // Place the new button relative to the container
        //     newButton.transform.localPosition = new Vector3(xOffset + (xSize / 2.0f), (yOffset - (ySize / 2.0f)), zOffset);
        //     newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * tokenButtonSlate.transform.localScale.x, newButton.transform.localScale.y * tokenButtonSlate.transform.localScale.y, newButton.transform.localScale.z * tokenButtonSlate.transform.localScale.z);
        //     newButton.transform.localEulerAngles = Vector3.zero;
        //     newButton.transform.name = $"{token} {newButton.transform.localPosition.x} {newButton.transform.localPosition.y}";

        //     // Update the coordinates for the next token
        //     xOffset += xSize + 0.02f;
        //     if (ySize > maxYSize) maxYSize = ySize;

        //     return newButton;
        // }

        // // HELPER FUNCTION FOR FunctionDisassemblyButtonCallbackCoroutine_ALT
        // // Given a disassembly token button and context info, set its callback method. 
        // void SetDisassemblyTokenButtonCallback(GameObject newButton, OxideBinary binary, OxideFunction function, string token)
        // {
        //     // Set button functions
        //     // -> Physical Press
        //     PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
        //     buttonFunction.TouchEnd.AddListener(() => DisassemblyTokenButtonCallback(binary, function, token));
        //     // -> Ray Press
        //     Interactable distanceInteract = newButton.GetComponent<Interactable>();
        //     distanceInteract.OnClick.AddListener(() => DisassemblyTokenButtonCallback(binary, function, token));
        // }

        // // HELPER FUNCTION FOR FunctionDisassemblyButtonCallbackCoroutine_ALT
        // // Callback method for disassembly token buttons.
        // public void DisassemblyTokenButtonCallback(OxideBinary binary, OxideFunction function, string token)
        // {
        //     statusText.text = $"Token: <B>{token}</B> from Binary {binary.name} / Function {function.name}";
        // }

        // // ALTERNATE version of the Disassembly callback that creates a scrolling object collection of buttons
        // // where the buttons are labelled with the tokens of the disassembly
        // IEnumerator FunctionDisassemblyButtonCallbackCoroutine_ALT(OxideBinary binary, OxideFunction function)
        // {
        //     // Make a new slate
        //     GameObject tokenButtonSlatePrefab = Resources.Load("Prefabs/TokenButtonSlate") as GameObject;
        //     GameObject tokenButtonSlate = Instantiate(tokenButtonSlatePrefab, GameManager.getSpawnPosition(), GameManager.getSpawnRotation());

        //     // Set title
        //     TextMeshPro titleBarTMP = tokenButtonSlate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
        //     TextMeshPro contentTMP = tokenButtonSlate.transform.Find("ContentTMP").gameObject.GetComponent<TextMeshPro>();
        //     titleBarTMP.text = $"{binary.name} / {function.name} Disassembly\n{function.signature}";
        //     contentTMP.text = "";

        //     // Find critical slate components
        //     ScrollingObjectCollection tokenButtonScrollingObjectCollection = tokenButtonSlate.GetComponentInChildren<ScrollingObjectCollection>();
        //     GameObject tokenButtonsContainer = tokenButtonScrollingObjectCollection.transform.Find("Container").gameObject;           
        //     GameObject tokenButtonPrefab = Resources.Load("Prefabs/TokenButton") as GameObject;

        //     // Walk through each basic block for this function and add token buttons to the slate
        //     float yOffset = 0.0f; 
        //     float zOffset = -0.01f;
        //     foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
        //     {
        //         foreach (OxideInstruction instruction in basicBlock.instructionDict.Values)
        //         {
        //             float xOffset = 0f; 
        //             float maxYSize = 0f;
        //             GameObject newButton = MakeTokenButton(instruction.offset, "#777777", tokenButtonPrefab, tokenButtonSlate, tokenButtonsContainer, ref xOffset, yOffset, zOffset, ref maxYSize);
        //             SetDisassemblyTokenButtonCallback(newButton, binary, function, instruction.offset);
        //             newButton = MakeTokenButton(instruction.mnemonic, "#99FF99", tokenButtonPrefab, tokenButtonSlate, tokenButtonsContainer, ref xOffset, yOffset, zOffset, ref maxYSize);
        //             SetDisassemblyTokenButtonCallback(newButton, binary, function, instruction.mnemonic);
        //             string[] tokens = instruction.op_str.Split(new char[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);
        //             foreach (string token in tokens)
        //             {
        //                 newButton = MakeTokenButton(token, "#FFFFFF", tokenButtonPrefab, tokenButtonSlate, tokenButtonsContainer, ref xOffset, yOffset, zOffset, ref maxYSize);
        //                 SetDisassemblyTokenButtonCallback(newButton, binary, function, token);
        //             }
        //             yOffset -= (maxYSize * 1.2f); // move down for next line
        //         }

        //         yield return new WaitForEndOfFrame(); // yield after each block instead of each instruction
        //     }
        //     tokenButtonScrollingObjectCollection.UpdateContent();
            
        //     unsetBusy();
        // }        
    }
}

