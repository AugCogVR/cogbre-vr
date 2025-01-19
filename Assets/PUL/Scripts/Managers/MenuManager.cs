using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using static Microsoft.MixedReality.Toolkit.Experimental.UI.KeyboardKeyFunc;

namespace PUL
{
    public class MenuManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor

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

        // The Tooltip prefeb we instantiate for function disassembly
        public GameObject tooltipPrefab;

        // The UI panel
        public GameObject UIPanel;

        // END: These values are wired up in the Unity Editor
        // ====================================

        // Instance holder
        private static MenuManager _instance; // this manager is a singleton

        public static MenuManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("MenuManager is NULL");
                return _instance;
            }
        }

        // refers to the storage of the data actively being returned from Oxide.
        public OxideData oxideData = null;

        // Checked by the MenuManagerEditor
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


        void Awake()
        {
            // If another instance exists, destroy that game object. If no other game manager exists, 
            // initialize the instance to itself. As this manager needs to exist throughout all scenes, 
            // call the function DontDestroyOnLoad.
            if (_instance)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            DontDestroyOnLoad(this);
        }

        // Start is called before the first frame update
        void Start()
        {
            // Main menu enable/disable at startup based on config
            bool mainMenuEnabledOnStartup = true;
            string value = ConfigManager.Instance.GetFeatureSetProperty("main_menu_enabled_on_startup");
            if (value != null) mainMenuEnabledOnStartup = bool.Parse(value);
            UIPanel.SetActive(mainMenuEnabledOnStartup);

            // Main menu movement enable/disable at startup based on config
            bool mainMenuMoveable = true;
            string value2 = ConfigManager.Instance.GetFeatureSetProperty("main_menu_moveable");
            if (value2 != null) mainMenuMoveable = bool.Parse(value2);
            ObjectManipulator titleBarOM = UIPanel.transform.Find("TitleBar").gameObject.GetComponent<ObjectManipulator>();
            titleBarOM.enabled = mainMenuMoveable;
        }

        // Update is called once per frame
        void Update()
        {
        }

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

        private GameObject makeAToolTip(string title, string contents, GameObject parentSlate)
        {
            // Make a new tooltip
            GameObject tooltip = Instantiate(tooltipPrefab, Vector3.zero, GameManager.Instance.getSpawnRotation(), parentSlate.transform);
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
            
            // Wire up call graph button, unless it's disabled by the config file (then deactivate it)
            bool callGraphsEnabled = true;
            string value = ConfigManager.Instance.GetFeatureSetProperty("call_graphs_enabled");
            if (value != null) callGraphsEnabled = bool.Parse(value);
            if (!callGraphsEnabled)
            {
                binaryCallGraphButton.SetActive(false);
            }
            else
            {
                PressableButtonHoloLens2 bcbuttonFunction = binaryCallGraphButton.GetComponent<PressableButtonHoloLens2>();
                bcbuttonFunction.TouchBegin.AddListener(() => BinaryCallGraphButtonCallback());
                Interactable bcdistanceInteract = binaryCallGraphButton.GetComponent<Interactable>();
                bcdistanceInteract.OnClick.AddListener(() => BinaryCallGraphButtonCallback());
            }
            
            PressableButtonHoloLens2 fdbuttonFunction = functionDisassemblyButton.GetComponent<PressableButtonHoloLens2>();
            fdbuttonFunction.TouchBegin.AddListener(() => FunctionDisassemblyButtonCallback());
            Interactable fddistanceInteract = functionDisassemblyButton.GetComponent<Interactable>();
            fddistanceInteract.OnClick.AddListener(() => FunctionDisassemblyButtonCallback());
            
            PressableButtonHoloLens2 fd2buttonFunction = functionDecompilationButton.GetComponent<PressableButtonHoloLens2>();
            fd2buttonFunction.TouchBegin.AddListener(() => FunctionDecompilationButtonCallback());
            Interactable fd2distanceInteract = functionDecompilationButton.GetComponent<Interactable>();
            fd2distanceInteract.OnClick.AddListener(() => FunctionDecompilationButtonCallback());

            // Wire up control flow graph button, unless it's disabled by the config file (then deactivate it)
            bool controlFlowGraphsEnabled = true;
            string value2 = ConfigManager.Instance.GetFeatureSetProperty("control_flow_graphs_enabled");
            if (value2 != null) controlFlowGraphsEnabled = bool.Parse(value2);
            if (!controlFlowGraphsEnabled)
            {
                functionControlFlowGraphButton.SetActive(false);
            }
            else
            {
                PressableButtonHoloLens2 fcbuttonFunction = functionControlFlowGraphButton.GetComponent<PressableButtonHoloLens2>(); 
                fcbuttonFunction.TouchBegin.AddListener(() => FunctionControlFlowGraphButtonCallback());
                Interactable fcdistanceInteract = functionControlFlowGraphButton.GetComponent<Interactable>();
                fcdistanceInteract.OnClick.AddListener(() => FunctionControlFlowGraphButtonCallback());
            }

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
            collection = await NexusClient.Instance.EnsureCollectionInfo(collection);

            // Build buttons without blocking the UI
            StartCoroutine(CollectionButtonCallbackCoroutine(collection.binaryList));
        }

        // Function that creates the objects that are associated with given collection
        IEnumerator CollectionButtonCallbackCoroutine(List<OxideBinary> binaryList)
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
            binary = await NexusClient.Instance.EnsureBinaryInfo(binary);

            
            // Build buttons without blocking the UI
            StartCoroutine(BinaryButtonCallbackCoroutine(binary));
            
        }

        // Function that creates the objects that are associated with given binary
        IEnumerator BinaryButtonCallbackCoroutine(OxideBinary binary)
        {
            int count = 0;

            // Create list of functions sorted by namne
            List<OxideFunction> sortedFunctionList = new List<OxideFunction>(binary.functionDict.Values);
            sortedFunctionList.Sort((x, y) => x.name.CompareTo(y.name));

            // Create a button for each function
            foreach (OxideFunction function in sortedFunctionList)
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

                // if (++count > 10) break; // low limit for testing
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
            string contents = await NexusClient.Instance.RetrieveTextForArbitraryModule("strings", selectedBinary.oid, "{}", true);

            // Make a new slate
            GameObject slate = SlateManager.Instance.MakeASlate($"Strings for {selectedBinary.name}", contents);

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
            string contents = await NexusClient.Instance.RetrieveTextForArbitraryModule("file_stats", selectedBinary.oid, "{}", true);

            // Make a new slate
            GameObject slate = SlateManager.Instance.MakeASlate($"File stats for {selectedBinary.name}", contents);

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
            GraphManager.Instance.BuildBinaryCallGraph(selectedBinary);
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
            // Use StringBuilders to build up slate contents
            StringBuilder sbMarkup = new StringBuilder();
            StringBuilder sbPlainText = new StringBuilder();

            int numLines = 0;
            
            // Walk through each basic block for this function and add instructions to text display
            foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
            {
                foreach (OxideInstruction instruction in basicBlock.instructionDict.Values)
                {
                    sbMarkup.Append($"<color=#777777>{instruction.offset} <color=#99FF99>{instruction.mnemonic} <color=#FFFFFF>{instruction.op_str}\n");
                    sbPlainText.Append($"{instruction.offset} {instruction.mnemonic} {instruction.op_str}\n");
                    numLines++;
                }
                sbMarkup.Append($"<color=#000000>------------------------------------\n");
                numLines++;

                yield return new WaitForEndOfFrame(); // yield after each block instead of each instruction
            }

            // Make a new slate
            GameObject slate = SlateManager.Instance.MakeASlate($"{binary.name} / {function.name} Disassembly\n{function.signature}", sbMarkup.ToString());

            // Grab the content TMP
            TextMeshProUGUI contentTMP = slate.GetComponentInChildren<TextMeshProUGUI>();

            // Adjust TMP transform to match content height
            float contentSize = numLines * (contentTMP.fontSize + 1.5f); // 1.5 = "font buffer"
            contentTMP.rectTransform.sizeDelta = new Vector2(contentTMP.rectTransform.sizeDelta.x, contentSize);

            // Write to capa output
            //FunctionCapaOutput(function, slate); -- REMOVED REFERENCE. Replacing this with a reference to the graph handle of the Call Graph, where CAPA is more applicable. 

            unsetBusy();
        }

        // Outputs the capa_results call to a slate, used for better visualization of the data we are working with
        // -> Just using void right now while in the testing phases
        void FunctionCapaOutput(OxideFunction function, GameObject CurrentSlate)
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
            GameObject Slate = SlateManager.Instance.MakeASlate("Capa Results", contents);
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
            selectedBinary = await NexusClient.Instance.EnsureBinaryDecompilation(selectedBinary);

            // Build text without blocking the UI
            StartCoroutine(FunctionDecompilationButtonCallbackCoroutine(selectedBinary, selectedFunction));
        }

        IEnumerator FunctionDecompilationButtonCallbackCoroutine(OxideBinary binary, OxideFunction function)
        {
            // Use StringBuilders to build up slate contents
            StringBuilder sbMarkup = new StringBuilder();
            StringBuilder sbPlainText = new StringBuilder();

            int numLines = 0;

            // Walk through decompilation and create text display
            int indentLevel = 0;
            foreach (KeyValuePair<int, OxideDecompLine> item in function.decompDict)
            {
                string code = item.Value.code;
                if (code.Contains('}')) indentLevel--; // Q&D indenting
                sbMarkup.Append($"<color=#777777>{item.Key}: ");
                for (int i = 0; i < indentLevel; i++) 
                {
                    sbMarkup.Append("    "); // Q&D indenting
                    sbPlainText.Append("    "); // Q&D indenting
                }
                sbMarkup.Append($"<color=#FFFFFF>{code}");
                sbPlainText.Append($"{code}");
                foreach (int offset in item.Value.associatedInstructionDict.Keys)
                {
                    sbMarkup.Append($"<color=#AAAA00> |{offset}|");
                }
                sbMarkup.Append("\n");
                sbPlainText.Append("\n");
                numLines++;
                if (code.Contains('{')) indentLevel++; // Q&D indenting

                yield return new WaitForEndOfFrame(); 
            }

            // Make a new slate
            GameObject slate = SlateManager.Instance.MakeASlate($"{binary.name} / {function.name} Decompilation", sbMarkup.ToString());

            // Grab the content TMP
            TextMeshProUGUI contentTMP = slate.GetComponentInChildren<TextMeshProUGUI>();

            // Adjust TMP transform to match content height
            float contentSize = numLines * (contentTMP.fontSize + 1.5f); // 1.5 = "font buffer"
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

            string graph_type = ConfigManager.Instance.GetGeneralProperty("cfg_graph_type");
            if ((graph_type != null) && (graph_type == "sugiyama"))
                GraphManager.Instance.BuildFunctionControlFlowGraph(selectedFunction); // Sugiyamai is default
            else if ((graph_type != null) && (graph_type == "FDG"))
                GraphManager.Instance.BuildFunctionControlFlowGraphFDG(selectedFunction);
            else 
            {
                Debug.Log("No graph type specified; using default");
                GraphManager.Instance.BuildFunctionControlFlowGraph(selectedFunction);                
            }
        }
    }
}

