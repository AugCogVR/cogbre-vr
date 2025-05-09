using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Globalization;
using LitJson;

namespace PUL
{
    [System.Serializable]
    public class NexusClient : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Nexus Client object

        public List<string> ignoreCollection = new List<string>(); // List of collection names to ignore when booting. Debugging tool used to exempt big collections early in development

        public float secondsBetweenUserTelemetryUpdates = 0.5f;

        public float secondsBetweenEnvironmentTelemetryUpdates = 1.0f;

        // END: These values are wired up in the Unity Editor -> Nexus Client object
        // ====================================

        private static NexusClient _instance; // this manager is a singleton

        public static NexusClient Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("NexusClient is NULL");
                return _instance;
            }
        }

        // All the Oxide data we pull from Nexus
        public OxideData oxideData;

        // Track last time we updated user and environment telemetry
        private DateTime lastUserUpdateTime;
        private DateTime lastEnvironmentUpdateTime;

        // List of collections to include in the session and exclude all others. Set in config file. Overrides "ignoreCollection."
        public List<string> includeOnlyTheseCollections = null;
        // List of binaries to include in the session and exclude all others. Set in config file. 
        public List<string> includeOnlyTheseBinaries = null; 


        // Did we initialize the session in Nexus yet?
        private bool sessionInitialized = false;

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

            lastUserUpdateTime = DateTime.Now;
            lastEnvironmentUpdateTime = DateTime.Now;
        }

        // Start is called before the first frame update
        void Start()
        {
            NexusSessionInit();

            // Read values fron config data
            string value = ConfigManager.Instance.GetGeneralProperty("seconds_between_user_telemetry_updates");
            if (value != null) secondsBetweenUserTelemetryUpdates = float.Parse(value);
            value = ConfigManager.Instance.GetGeneralProperty("seconds_between_environment_telemetry_updates");
            if (value != null) secondsBetweenEnvironmentTelemetryUpdates = float.Parse(value);
            // Debug.Log($"user updates: {secondsBetweenUserTelemetryUpdates} -- environment updates: {secondsBetweenEnvironmentTelemetryUpdates}");
            value = ConfigManager.Instance.GetFeatureSetProperty("include_only_these_collections");
            if (value != null) 
                includeOnlyTheseCollections = new List<string>(value.Split(','));
            else
                includeOnlyTheseCollections = null;
            value = ConfigManager.Instance.GetFeatureSetProperty("include_only_these_binaries");
            if (value != null) 
                includeOnlyTheseBinaries = new List<string>(value.Split(','));
            else
                includeOnlyTheseBinaries = null;
        }

        // Update is called once per frame
        void Update()
        {
            // Skip the rest of this if Nexus session isn't initialized
            if (!sessionInitialized) return;

            DateTime currTime = DateTime.Now;
            TimeSpan elapsedUserUpdateTime = currTime - lastUserUpdateTime;
            if (elapsedUserUpdateTime.TotalSeconds > secondsBetweenUserTelemetryUpdates)
            {
                lastUserUpdateTime = currTime;
                NexusSessionUpdate(GameManager.Instance.GetUserTelemetryJSON());
            }

            TimeSpan elapsedEnvironmentUpdateTime = currTime - lastEnvironmentUpdateTime;
            if (elapsedEnvironmentUpdateTime.TotalSeconds > secondsBetweenEnvironmentTelemetryUpdates)
            {
                lastEnvironmentUpdateTime = currTime;

                string slateTelemetryJSON = SlateManager.Instance.GetSlateTelemetryJSON();
                if (slateTelemetryJSON != "") NexusSessionUpdate(slateTelemetryJSON);

                string graphTelemetryJSON = GraphManager.Instance.GetGraphTelemetryJSON();
                if (graphTelemetryJSON != "") NexusSessionUpdate(graphTelemetryJSON);

                string notepadTelemetryJSON = TextManager.Instance.GetNotepadTelemetryJSON();
                if (notepadTelemetryJSON != "") NexusSessionUpdate(notepadTelemetryJSON);
            }
        }

        public async void NexusSessionInit()
        {
            Debug.Log("Nexus Session Init is Running!");
            string configJsonString = JsonConvert.SerializeObject(ConfigManager.Instance.GetSettingsAsDict());
            string sessionInitResult = await NexusSyncTask($"[\"session_init\", {configJsonString}]");
            
            // Retrieve collection info
            string collectionNames = await NexusSyncTask("[\"oxide_collection_names\"]");
            // Store found CIDS in a temporary list then parse into OxideCollection type
            IList<string> collectionNameList = JsonConvert.DeserializeObject<IList<string>>(collectionNames);

            // Create OxideData and OxideCollection objects
            oxideData = new OxideData();
            foreach (string collectionName in collectionNameList)
            {
                // If we have an include list, ensure this collection is in that list.
                if ((includeOnlyTheseCollections != null) && 
                    !(includeOnlyTheseCollections.Contains(collectionName)))
                {
                    Debug.Log($"NexusClient -> Skipping collection not in include list: {collectionName}");
                    continue;
                }

                // Ignore collection for debug purposes.
                if (ignoreCollection.Contains(collectionName))
                {
                    Debug.Log($"NexusClient -> Ignoring collection {collectionName}");
                    continue;
                }

                string collectionId = await NexusSyncTask($"[\"oxide_get_cid_from_name\", \"{collectionName}\"]");

                collectionId = collectionId.Replace("\"", ""); // remove extraneous quotes 
                // Build collection object with missing info. We'll fill it in 
                // if user ever selects this collection.
                oxideData.collectionList.Add(new OxideCollection(collectionId, collectionName, null, null));
            }

            // Sort collection list by name
            oxideData.collectionList.Sort((x, y) => x.name.CompareTo(y.name));

            // Provide oxide data to menu manager and initiatize it
            MenuManager.Instance.oxideData = oxideData;
            Debug.Log(oxideData);
            MenuManager.Instance.MenuInit();

            sessionInitialized = true;
        }

        // Handle an individual session update command (e.g., telemetry update) 
        // and process its response. 
        // Command example: $"[\"session_update\", \"objectTelemetry\", " followed by
        // comma-separated object name, position, and orientation.
        public async void NexusSessionUpdate(string command)
        {
            // Send session update to Nexus and await response
            string responseJSONString = await NexusSyncTask(command);
            // if (responseJSONString != "{}")
            //     Debug.Log($"NexusClient: Session update response JSON: {responseJSONString}");

            // Process the response. 
            JsonData responseJSON = JsonMapper.ToObject(responseJSONString);

            // Check for AI payload
            try
            {
                // If there is an AI payload, pass it to the AI Manager.
                string payload = (string)responseJSON["ai_payload"];
                // Debug.Log($"NexusClient: Received AI payload: {payload}");
                AIAssistantManager.Instance.AcceptAIPayload(payload);
            }
            catch (Exception e)
            {
                // Fail silently; assume JSON was missing key "ai_payload" and do nothing further.
                // Newer versions of LitJson include "ContainsKey()" that we could use to check for 
                // the key instead of doing this try...catch dance, but we have an old 
                // version of LitJson that Viveport depends on.
            }

            // Check for config update
            try
            {
                // If there is a config update, pass it to the Config Manager.
                JsonData configJsonData = responseJSON["config_update"];
                ConfigManager.Instance.SetConfigFromJSON(configJsonData);
            }
            catch (Exception e)
            {
                // Fail silently; assume JSON was missing key "config_update" and do nothing further.
                // Newer versions of LitJson include "ContainsKey()" that we could use to check for 
                // the key instead of doing this try...catch dance, but we have an old 
                // version of LitJson that Viveport depends on.
            }
        }

        // Create and async Task to call the Nexus API and return the response
        private async Task<string> NexusSyncTask(string command)
        {
            try
            {
                // TO DO: fix hardcoded IP address
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:5000/client_sync");
                request.ContentType = "application/json";
                request.Method = "POST";
                StreamWriter writer = new StreamWriter(await request.GetRequestStreamAsync());
                string jsonRequest = "{\"sessionId\":\"" + ConfigManager.Instance.sessionId + "\", \"command\":" + command + "}";
                // Debug.Log("JSON REQUEST: " + jsonRequest);
                writer.Write(jsonRequest);
                writer.Close();

                HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string responseStringJson = reader.ReadToEnd();
                reader.Close();
                response.Close();

                // Log the JSON response before deserialization for debugging
                // Debug.Log(jsonRequest + "\nNexusSync response JSON: " + responseStringJson);

                string responseString = JsonConvert.DeserializeObject<string>(responseStringJson);

                // Log the deserialized response string for debugging
                // Debug.Log(jsonRequest + "\nNexusSync response string: " + responseString);

                return responseString;
            }
            catch (Exception e)
            {
                // Log any exceptions that occur during the request
                Debug.LogError("Exception during NexusSyncTask: " + e.Message);

                return null; // Return null or an empty string as a default value on error
            }
        } 

        // Given an arbitray string, sanitize it so we can send it as JSON.
        public string SanitizeStringForJSON(string fixMe)
        {
            // TODO: santize contents for JSON -- see https://stackoverflow.com/questions/3020094/how-should-i-escape-strings-in-json
            // ...Or just use Base64 for now
            string encoded = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fixMe));
            return encoded;
        }

        // Ensure that this collection has its associated binary info collected.
        // Return the collection, but with info filled in.
        // This method does NOT fill in all the info for each binary -- that is performed,
        // as needed, by EnsureBinaryInfo. 
        // NOTE: This really should be a member function of the OxideCollection class but "get" functions can't be async
        public async Task<OxideCollection> EnsureCollectionInfo(OxideCollection collection)
        {
            // This async approach courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
            return await Task.Run<OxideCollection>(async () =>
            {
                if (collection.binaryList == null)
                {
                    // Pull the list of (presumably) binary OIDs info for the given Collection ID
                    collection.binaryList = new List<OxideBinary>();
                    string oidListJson = await NexusSyncTask($"[\"oxide_get_oids_with_cid\", \"{collection.collectionId}\"]");
                    // Debug.Log($"OID_pull (Collection: {cid}): {oidPull}");
                    IList<string> oidList = JsonConvert.DeserializeObject<IList<string>>(oidListJson);
                    foreach (string oid in oidList)
                    {
                        // -> Grab binary name for OID
                        string binaryNameListJson = await NexusSyncTask($"[\"oxide_get_names_from_oid\", \"{oid}\"]");
                        IList<string> binaryNameList = JsonConvert.DeserializeObject<IList<string>>(binaryNameListJson);
                        string binaryName = "Nameless Binary";
                        if (binaryNameList.Count > 0) binaryName = binaryNameList[0];
                        //Debug.Log($"BINARY NAME: {binaryName}");

                        // If we have an include list, ensure this binary is in that list.
                        if ((includeOnlyTheseBinaries != null) && 
                            !(includeOnlyTheseBinaries.Contains(binaryName)))
                        {
                            Debug.Log($"NexusClient -> Skipping binary not in include list: {binaryName}");
                            continue;
                        }

                        // -> Grab binary size
                        string size = await NexusSyncTask($"[\"oxide_get_oid_file_size\", \"{oid}\"]");

                        // Build binary object with basic info. Additional info will be filled in later
                        // if user ever selects this binary.
                        OxideBinary binary = new OxideBinary(oid, binaryName, size);
                        binary.parentCollection = collection;

                        // -> Log binary
                        collection.binaryList.Add(binary);
                    }

                    // Sort binary list by name
                    collection.binaryList.Sort((x, y) => x.name.CompareTo(y.name));
                }
                Debug.Log($"=== For collection {collection.name}: {collection.binaryList.Count} binaries.");
                return collection;
            });
        }

        // Pull (almost) all the info for a binary from Oxide if not already populated. 
        // Return the binary, but with info filled in.
        // Includes disassembly but NOT decompilation -- that's in another method.
        // This approach lets us pull the info on an as-needed basis 
        // (we'll never pull it for a binary no one selects).
        // NOTE: This really should be a member function(s) of the OxideBinary class but "get" functions can't be async
        public async Task<OxideBinary> EnsureBinaryInfo(OxideBinary binary)
        {
            return await Task.Run<OxideBinary>(async () =>
            {
                // INSTRUCTIONS
                if (binary.instructionDict == null)
                {
                    // Pull the disassembly into a dict of instructions, keyed by offset
                    binary.instructionDict = new SortedDictionary<int, OxideInstruction>();
                    string disassemblyJsonString = await NexusSyncTask("[\"oxide_get_disassembly\", \"" + binary.oid + "\"]");
                    if (disassemblyJsonString != null) 
                    {
                        JsonData disassemblyJson = JsonMapper.ToObject(disassemblyJsonString)[binary.oid]["instructions"];
                        foreach (KeyValuePair<string, JsonData> item in disassemblyJson)
                        {
                            // Create new instruction object and add to dictionary
                            string offset = (string)(item.Key);
                            string str = (string)(item.Value["str"]);
                            OxideInstruction instruction = new OxideInstruction(offset, str);
                            int instructionDictKey = Int32.Parse(item.Key);
                            binary.instructionDict[instructionDictKey] = instruction;

                            // Set additional values 
                            instruction.mnemonic = (string)(item.Value["mnemonic"]);
                            instruction.op_str = (string)(item.Value["op_str"]);
                        }
                    }
                }

                // BASIC BLOCKS
                if (binary.basicBlockDict == null)
                {
                    // Pull the basic block info
                    binary.basicBlockDict = new SortedDictionary<int, OxideBasicBlock>();
                    string basicBlocksJsonString = await NexusSyncTask("[\"oxide_get_basic_blocks\", \"" + binary.oid + "\"]");
                    if (basicBlocksJsonString != null) 
                    {
                        // Parse basic block info from JSON
                        JsonData basicBlocksJson = JsonMapper.ToObject(basicBlocksJsonString)[binary.oid];
                        foreach (KeyValuePair<string, JsonData> item in basicBlocksJson)
                        {
                            // Create new basic block object and add to dictionary
                            string offset = item.Key;
                            OxideBasicBlock basicBlock = new OxideBasicBlock(offset);
                            int basicBlockDictKey = Int32.Parse(item.Key);
                            binary.basicBlockDict[basicBlockDictKey] = basicBlock;

                            // Set additional values
                            basicBlock.instructionDict = new SortedDictionary<int, OxideInstruction>();
                            foreach (JsonData addr in item.Value["members"])
                            {
                                int instructionOffset = Int32.Parse($"{addr}");
                                if (binary.instructionDict.ContainsKey(instructionOffset))
                                {
                                    OxideInstruction instruction = binary.instructionDict[instructionOffset];
                                    basicBlock.instructionDict[instructionOffset] = instruction;
                                }
                                else
                                {
                                    Debug.Log($"For binary {binary.name}: instruction offset {instructionOffset} not in instructionDict");
                                }
                            }
                            basicBlock.destinationAddressList = new List<string>();
                            foreach (JsonData addr in item.Value["dests"])
                            {
                                basicBlock.destinationAddressList.Add($"{addr}");
                            }
                            basicBlock.sourceBasicBlockDict = new SortedDictionary<int, OxideBasicBlock>();
                            basicBlock.targetBasicBlockDict = new SortedDictionary<int, OxideBasicBlock>();
                        }

                        // Now walk through each block and identify source and target blocks
                        foreach (KeyValuePair<int, OxideBasicBlock> item in binary.basicBlockDict)
                        {
                            // Identify source block
                            OxideBasicBlock sourceBlock = item.Value;
                            foreach (string destinationAddress in sourceBlock.destinationAddressList)
                            {
                                // Check if the destination is a valid offset
                                int targetOffset = -1;
                                try 
                                { 
                                    targetOffset = Int32.Parse(destinationAddress);
                                }
                                catch {}
                                // If valid, update source and target dicts
                                if (binary.basicBlockDict.ContainsKey(targetOffset))
                                {
                                    // Identify target block
                                    OxideBasicBlock targetBlock = binary.basicBlockDict[targetOffset];
                                    // Update source block's targets
                                    sourceBlock.targetBasicBlockDict[targetOffset] = targetBlock;
                                    // Update target block's sources
                                    targetBlock.sourceBasicBlockDict[item.Key] = sourceBlock;
                                }
                            }
                        }
                    }
                }

                // FUNCTIONS
                if (binary.functionDict == null)
                {
                    // Pull the function info
                    binary.functionDict = new SortedDictionary<int, OxideFunction>();
                    string functionsJsonString = await NexusSyncTask($"[\"oxide_retrieve\", \"function_extract\", [\"{binary.oid}\"], {{}}]");
                    if (functionsJsonString != null) 
                    {
                        int dummyOffset = Int32.MaxValue - 1;
                        JsonData functionsJson = JsonMapper.ToObject(functionsJsonString);
                        foreach (KeyValuePair<string, JsonData> item in functionsJson)
                        {
                            // Get initial values, create new function object, add to dictionary
                            string name = (string)(item.Key);

                            int offsetInt = -1;
                            if (item.Value["start"] != null)
                            {
                                offsetInt = (int)(item.Value["start"]); 
                            }
                            else
                            {
                                // HACK: Use dummy offset for functions with null starting offset. 
                                // External functions? This is common in ELF binaries.
                                offsetInt = dummyOffset;
                                dummyOffset--;
                            }
                            string signature = (string)(item.Value["signature"]);
                            OxideFunction function = new OxideFunction(name, $"{offsetInt}", signature);
                            binary.functionDict[offsetInt] = function;
                            
                            // Set additional values
                            function.vaddr = (string)(item.Value["vaddr"]);
                            function.retType = (string)(item.Value["retType"]);
                            function.returning = ((string)(item.Value["returning"]) == "true");
                            function.basicBlockDict = new SortedDictionary<int, OxideBasicBlock>();
                            foreach (JsonData block in item.Value["blocks"])
                            {
                                int blockOffset = (int)block;
                                function.basicBlockDict[blockOffset] = binary.basicBlockDict[blockOffset];
                            }
                            function.paramsList = new List<string>();
                            foreach (JsonData param in item.Value["params"])
                            {
                                function.paramsList.Add($"{param}");
                            }
                            function.sourceFunctionDict = new SortedDictionary<int, OxideFunction>();
                            function.targetFunctionDict = new SortedDictionary<int, OxideFunction>();
                            function.capaList = new List<string>(); // create the list here, but fill it in later
                        }
                    }
                }

                // CLEANUP 1
                // Walk through the instructions, basic blocks, and functions to set parent references.
                // First, walk through the basic blocks and set instruction parents.
                // This *should* cover all the instructions. 
                foreach (OxideBasicBlock basicBlock in binary.basicBlockDict.Values)
                {
                    foreach (OxideInstruction instruction in basicBlock.instructionDict.Values)
                    {
                        instruction.parentBlock = basicBlock;
                    }
                }
                // Next, set parents of functions and basic blocks. 
                foreach (OxideFunction function in binary.functionDict.Values)
                {
                    function.parentBinary = binary;
                    foreach (OxideBasicBlock basicBlock in function.basicBlockDict.Values)
                    {
                        basicBlock.parentFunction = function;
                    }
                }
                // KNOWN ISSUE: Not all basic blocks are covered by functions in the 
                // data returned by function_extract! e.g., basic block 47058 in regedit.exe
                // HACK: Create dummy function to contain these blocks. 
                int mainDummyOffset = Int32.MaxValue;
                OxideFunction mainDummyFunction = new OxideFunction("_orphaned_blocks", $"{mainDummyOffset}", "artificial container");
                binary.functionDict[mainDummyOffset] = mainDummyFunction;
                mainDummyFunction.parentBinary = binary;
                mainDummyFunction.vaddr = "0";
                mainDummyFunction.retType = "unknown";
                mainDummyFunction.returning = false;
                mainDummyFunction.sourceFunctionDict = new SortedDictionary<int, OxideFunction>();
                mainDummyFunction.targetFunctionDict = new SortedDictionary<int, OxideFunction>();
                mainDummyFunction.basicBlockDict = new SortedDictionary<int, OxideBasicBlock>();
                // Set parent and child relationships for all "orphaned" basic blocks. 
                foreach (KeyValuePair<int, OxideBasicBlock> blockItem in binary.basicBlockDict)
                {
                    if (blockItem.Value.parentFunction == null)
                    {
                        blockItem.Value.parentFunction = mainDummyFunction;
                        mainDummyFunction.basicBlockDict[blockItem.Key] = blockItem.Value;
                    }
                }

                // CLEANUP 2
                // Update function-level sources and targets now that we have bi-directional
                // links between function <-> block <-> instruction.
                // First pull function_calls info from Nexus/Oxide.
                string functionCallsJsonString = await NexusSyncTask($"[\"oxide_retrieve\", \"function_calls\", [\"{binary.oid}\"], {{}}]");
                if (functionCallsJsonString != null) 
                {
                    JsonData functionCallsJson = JsonMapper.ToObject(functionCallsJsonString);
                    foreach (KeyValuePair<string, JsonData> item in functionCallsJson)
                    {
                        // First find the "source" function by finding out what function
                        // holds the calling instruction at the given offset
                        int sourceOffset = Int32.Parse(item.Key);
                        if (binary.instructionDict.ContainsKey(sourceOffset))
                        {
                            OxideInstruction instruction = binary.instructionDict[sourceOffset];
                            OxideBasicBlock basicBlock = instruction.parentBlock;
                            OxideFunction sourceFunction = basicBlock.parentFunction;

                            // Next find the "target" function in a similar manner
                            // and update the source and target's lists of targets and sources
                            int targetOffset = (int)(item.Value["func_addr"]);
                            if (binary.functionDict.ContainsKey(targetOffset))
                            {
                                OxideFunction targetFunction = binary.functionDict[targetOffset];
                                sourceFunction.targetFunctionDict[targetOffset] = targetFunction;
                                targetFunction.sourceFunctionDict[sourceOffset] = sourceFunction;
                                // Debug.Log($"INFO: Function {sourceFunction.name} calls {targetFunction.name}");
                            }
                            else 
                            {
                                // Debug.Log($"WARNING: reported function call target at offset {targetOffset} not found in instruction dict");
                            }
                        }
                        else 
                        {
                            // For some reason, some reported offsets for calls aren't in the disassembly...???
                            // e.g., "2595" in TASKMAN.EXE
                            // Debug.Log($"WARNING: reported function call at offset {sourceOffset} not found in instruction dict");
                        }
                    }
                }                    

                // ADDITIONAL ACTIVITIES 
                // Load Capa-identified capability strings into respective function objects
                string capaJsonString = await NexusSyncTask($"[\"oxide_retrieve\", \"capa_results\", [\"{binary.oid}\"], {{}}]");
                if (capaJsonString != null) 
                {
                    JsonData capaJson = JsonMapper.ToObject(capaJsonString)[binary.oid]["capa_capabilities"];
                    foreach (KeyValuePair<string, JsonData> item in capaJson)
                    {
                        string capability = (string)item.Key;
                        // Debug.Log($"CAPA capability identified: {capability}");
                        foreach (JsonData offset in item.Value)
                        {
                            int offsetInt = (int)offset;
                            // Debug.Log($"CAPA func offset: {offsetInt}");
                            if (binary.functionDict.ContainsKey(offsetInt))
                                binary.functionDict[offsetInt].capaList.Add(item.Key);
                            else
                                Debug.Log($"CAPA capability \"{capability}\" at func offset {offsetInt}: FUNCTION NOT FOUND AT OFFSET!");
                        }
                    }
                }

                Debug.Log($"=== For binary {binary.name}: {binary.functionDict.Keys.Count} functions, {binary.basicBlockDict.Keys.Count} basic blocks, {binary.instructionDict.Keys.Count} instructions.");
                return binary; 
            });
        }

        // Pull the decompilation for a binary from Oxide if not already populated. 
        // Populate both the binary-level decomp dict and each function-level decomp dict.
        public async Task<OxideBinary> EnsureBinaryDecompilation(OxideBinary binary)
        {
            // Make sure we have populated this baseline data of this binary object. 
            binary = await EnsureBinaryInfo(binary);

            return await Task.Run<OxideBinary>(async () =>
            {
                if (binary.decompMapDict == null)
                {
                    // Pull the decompilation info for the entire binary
                    string decompJsonString = await NexusSyncTask($"[\"oxide_retrieve\", \"ghidra_decmap\", [\"{binary.oid}\"], {{\"org_by_func\":true}}]");
                    if (decompJsonString != null) 
                    {
                        JsonData decompJson = JsonMapper.ToObject(decompJsonString)["decompile"];

                        // Create the binary-level decomp line dict
                        binary.decompMapDict = new SortedDictionary<int, SortedDictionary<int, OxideDecompLine>>();

                        // Walk through the functions in the JSON data
                        foreach (KeyValuePair<string, JsonData> funcItem in decompJson)
                        {
                            // We only have function name. Search for the function object instance.
                            string functionName = funcItem.Key;
                            OxideFunction function = null;
                            foreach (OxideFunction candidateFunction in binary.functionDict.Values)
                            {
                                if (candidateFunction.name == functionName)
                                {
                                    function = candidateFunction;
                                    break;
                                }
                            }
                            if (function == null)
                            {
                                Debug.Log($"WARNING: Could not find function object for name {functionName}");
                                continue;
                            }

                            // Create function-level line dict
                            function.decompDict = new SortedDictionary<int, OxideDecompLine>();

                            // Walk through the offsets and populate the decomp lines and 
                            // associated offsets
                            foreach (KeyValuePair<string, JsonData> offsetItem in funcItem.Value)
                            {
                                // Get the integer offset. If the key is not a number (e.g., "None")
                                // just leave it as -1.
                                int offset = -1;
                                try 
                                {
                                    offset = Int32.Parse(offsetItem.Key);
                                }
                                catch {}

                                // For this offset, walk through the lines to add to the decomp line dict
                                foreach (JsonData lineJson in offsetItem.Value["line"])
                                {
                                    // Extract the line number and code text 
                                    string line = (string)lineJson;
                                    int split = line.IndexOf(": ");
                                    string lineNoStr = line.Substring(0, split);
                                    int lineNo = Int32.Parse(lineNoStr);
                                    string code = line.Substring(split + 2);

                                    // Find the decomp line for this line number. 
                                    // Create it if not existing.
                                    OxideDecompLine decompLine = null;
                                    if (function.decompDict.ContainsKey(lineNo))
                                    {
                                        decompLine = function.decompDict[lineNo];
                                    }
                                    else
                                    {
                                        decompLine = new OxideDecompLine(code);
                                        function.decompDict[lineNo] = decompLine;
                                    }

                                    // Create the associated instruction dict for this decomp line 
                                    // if it doesn't exist already.
                                    if (decompLine.associatedInstructionDict == null)
                                    {
                                        decompLine.associatedInstructionDict = new SortedDictionary<int, OxideInstruction>();
                                    }

                                    // For meaningful offsets, perform several actions.
                                    if (offset >= 0)
                                    {
                                        // Look up the instruction associated with this offset
                                        // and add it to the decompLine's associated instruction dict
                                        if (binary.instructionDict.ContainsKey(offset))
                                        {
                                            decompLine.associatedInstructionDict[offset] = binary.instructionDict[offset];
                                        }

                                        // In binary-level dict, create dict for this offset if not already there.
                                        if (!binary.decompMapDict.ContainsKey(offset))
                                        {
                                            binary.decompMapDict[offset] = new SortedDictionary<int, OxideDecompLine>();
                                        }
                                        // and add the line to the binary-level dict for this offset.
                                        binary.decompMapDict[offset][lineNo] = decompLine;
                                    }
                                }
                            }
                        }
                    }

                    Debug.Log($"=== For binary {binary.name}: {binary.decompMapDict.Keys.Count} instructions with decompiled code.");
                }
                return binary; 
            });
        }

        // Given an Oxide module name and an OID, run the module on the OID and return the results as a string.
        public async Task<string> RetrieveTextForArbitraryModule(string moduleName, string oid, string parameters, bool firstOIDOnly)
        {
            return await Task.Run<string>(async () =>
            {
                string returnMe = "";

                string retrievedJsonString = await NexusSyncTask($"[\"oxide_retrieve\", \"{moduleName}\", [\"{oid}\"], {parameters}]");

                if (retrievedJsonString != null) 
                {
                    JsonData retrievedJson = JsonMapper.ToObject(retrievedJsonString);
                    if (firstOIDOnly) retrievedJson = retrievedJson[oid];
                    foreach (KeyValuePair<string, JsonData> item in retrievedJson)
                    {
                        returnMe += $"{item.Key}: {item.Value.ToString()}\n";
                    }
                }

                return returnMe;
            });
        }
    }
}
