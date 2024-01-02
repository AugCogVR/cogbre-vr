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
        GameManager gameManager;
        public int pacingCounter; // braindead dumb mechanism to throttle polling
        private string userId;
        public OxideData oxideData;

        public NexusClient(GameManager gameManager)
        {
            //Debug.Log("NexusClient Constructor");
            this.gameManager = gameManager;
            pacingCounter = 0;
            userId = "User123"; // LATER: allow for multiple user IDs when we have multiple users!
            NexusSessionInit();
        }

        // OnUpdate is called by Game Manager Update
        public void OnUpdate()
        {
            // pacingCounter = braindead dumb mechanism to throttle polling
            pacingCounter++;
            int pacingCounterLimit = 1000;
            if (pacingCounter > pacingCounterLimit)
            {
                pacingCounter = 0;
                NexusUpdate();
            }
        }

        public async void NexusSessionInit()
        {
            Debug.Log("Nexus Session Init is Running!");
            string sessionInitResult = await NexusSyncTask("[\"session_init\"]");

            // Retrieve collection info
            string collectionNames = await NexusSyncTask("[\"oxide_collection_names\"]");
            // Store found CIDS in a temporary list then parse into OxideCollection type
            IList<string> collectionNameList = JsonConvert.DeserializeObject<IList<string>>(collectionNames);

            // Create OxideData and OxideCollection objects
            oxideData = new OxideData();
            foreach (string collectionName in collectionNameList)
            {
                string collectionId = await NexusSyncTask($"[\"oxide_get_cid_from_name\", \"{collectionName}\"]");
                collectionId = collectionId.Replace("\"", ""); // remove extraneous quotes 
                // Build collection object with missing info. We'll fill it in 
                // if user ever selects this collection.
                oxideData.collectionList.Add(new OxideCollection(collectionId, collectionName, null, null));
            }
            gameManager.menuManager.oxideData = oxideData;
            Debug.Log(oxideData);

            // Once all information is pulled initialize the menu
            gameManager.menuManager.MenuInit();
        }

        private async void NexusUpdate()
        {
            string whatever = await NexusSyncTask("[\"get_session_update\"]");
        }

        private async Task<string> NexusSyncTask(string command)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:5000/sync_portal");
                request.ContentType = "application/json";
                request.Method = "POST";
                StreamWriter writer = new StreamWriter(await request.GetRequestStreamAsync());
                string jsonRequest = "{\"userId\":\"" + userId + "\", \"command\":" + command + "}";
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

        // Return a list of binaries given a collection
        // Build and store them in the given collection, if not already there
        // This really should be a member function of the OxideCollection class but "get" functions can't be async
        public async Task<OxideCollection> EnsureCollectionInfo(OxideCollection collection)
        {
            // This async approach courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
            return await Task.Run<OxideCollection>(async () =>
            {
                if (collection.binaryList == null)
                {
                    // -> Get OIDs
                    string oidListJson = await NexusSyncTask($"[\"oxide_get_oids_with_cid\", \"{collection.collectionId}\"]");
                    // Debug.Log($"OID_pull (Collection: {cid}): {oidPull}");
                    // -> Format OIDs
                    IList<string> oidList = JsonConvert.DeserializeObject<IList<string>>(oidListJson);
                    IList<OxideBinary> binaryList = new List<OxideBinary>();
                    // Roll through each OID found, assign information
                    foreach (string oid in oidList)
                    {
                        // -> Grab binary name for OID
                        string binaryNameListJson = await NexusSyncTask($"[\"oxide_get_names_from_oid\", \"{oid}\"]");
                        IList<string> binaryNameList = JsonConvert.DeserializeObject<IList<string>>(binaryNameListJson);
                        // --> Make sure binaryNameList has contents
                        if (binaryNameList.Count <= 0)
                            binaryNameList.Add("Nameless Binary");
                        //Debug.Log($"BINARY NAME: {binaryNameList[0]}");

                        // -> Grab binary size
                        string size = await NexusSyncTask($"[\"oxide_get_oid_file_size\", \"{oid}\"]");

                        // Build binary object with missing info. We'll fill it in 
                        // if user ever selects this binary.
                        OxideBinary binary = new OxideBinary(oid, binaryNameList[0], size);
                        // -> Log binary
                        binaryList.Add(binary);
                    }
                    collection.binaryList = binaryList;
                }
                Debug.Log($"=== For collection {collection.name}: {collection.binaryList.Count} binaries.");
                return collection;
            });
        }

        // Pull (almost) all the info for a binary from Oxide if not already populated. 
        // Includes disassembly but NOT decompilation -- that's in another method.
        // This approach lets us pull the info on an as-needed basis 
        // (we'll never pull it for a binary no one selects).
        public async Task<OxideBinary> EnsureBinaryInfo(OxideBinary binary)
        {
            return await Task.Run<OxideBinary>(async () =>
            {
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

                if (binary.basicBlockDict == null)
                {
                    // Pull the basic block info
                    binary.basicBlockDict = new SortedDictionary<int, OxideBasicBlock>();
                    string basicBlocksJsonString = await NexusSyncTask("[\"oxide_get_basic_blocks\", \"" + binary.oid + "\"]");
                    if (basicBlocksJsonString != null) 
                    {
                        JsonData basicBlocksJson = JsonMapper.ToObject(basicBlocksJsonString)[binary.oid];
                        foreach (KeyValuePair<string, JsonData> item in basicBlocksJson)
                        {
                            // Create new basic block object and add to dictionary
                            string offset = item.Key;
                            OxideBasicBlock basicBlock = new OxideBasicBlock(offset);
                            int basicBlockDictKey = Int32.Parse(item.Key);
                            binary.basicBlockDict[basicBlockDictKey] = basicBlock;

                            // Set additional values
                            basicBlock.instructionAddressList = new List<string>();
                            foreach (JsonData addr in item.Value["members"])
                            {
                                basicBlock.instructionAddressList.Add($"{addr}");
                            }
                            basicBlock.destinationAddressList = new List<string>();
                            foreach (JsonData addr in item.Value["dests"])
                            {
                                basicBlock.destinationAddressList.Add($"{addr}");
                            }
                        }
                    }
                }

                if (binary.functionDict == null)
                {
                    // Pull the function info
                    binary.functionDict = new SortedDictionary<int, OxideFunction>();
                    string functionsJsonString = await NexusSyncTask($"[\"oxide_retrieve\", \"function_extract\", [\"{binary.oid}\"], {{}}]");
                    if (functionsJsonString != null) 
                    {
                        JsonData functionsJson = JsonMapper.ToObject(functionsJsonString);
                        foreach (KeyValuePair<string, JsonData> item in functionsJson)
                        {
                            // TODO: Skip functions with null starting offset for now;
                            // figure out later what to do with them... (have to move away from
                            // indexing functions by offset).
                            // This is common in ELF binaries.
                            if (item.Value["start"] == null) continue;

                            // Get initial values, create new function object, add to dictionary
                            string name = (string)(item.Key);
                            string offset = $"{item.Value["start"]}";
                            string signature = (string)(item.Value["signature"]);
                            OxideFunction function = new OxideFunction(name, offset, signature);
                            int functionDictKey = (int)(item.Value["start"]); 
                            binary.functionDict[functionDictKey] = function;

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
                        }
                    }
                }

                Debug.Log($"=== For binary {binary.name}: {binary.functionDict.Keys.Count} functions, {binary.basicBlockDict.Keys.Count} basic blocks, {binary.instructionDict.Keys.Count} instructions.");
                return binary; 
            });
        }

        // Pull the decompilation for a binary from Oxide if not already populated. 
        public async Task<OxideBinary> EnsureBinaryDecompilation(OxideBinary binary)
        {
            // Make sure we have populated this baseline data of this binary object. 
            binary = await EnsureBinaryInfo(binary);

            return await Task.Run<OxideBinary>(async () =>
            {
                if (binary.decompMapDict == null)
                {
                    // Pull the decompilation info for the entire binary
                    binary.decompMapDict = new SortedDictionary<int, SortedDictionary<int, OxideDecompLine>>();
                    string decompJsonString = await NexusSyncTask($"[\"oxide_retrieve\", \"ghidra_decmap\", [\"{binary.oid}\"], {{}}]");
                    if (decompJsonString != null) 
                    {
                        JsonData decompJson = JsonMapper.ToObject(decompJsonString)["decompile"];
                        // Walk through the offsets collecting decomp lines
                        foreach (KeyValuePair<string, JsonData> item in decompJson)
                        {
                            // Create line dict for this offset
                            int offset = Int32.Parse(item.Key);
                            SortedDictionary<int, OxideDecompLine> lineDict = new SortedDictionary<int, OxideDecompLine>();
                            binary.decompMapDict[offset] = lineDict;

                            // Fill line dict
                            foreach (JsonData lineJson in item.Value["line"])
                            {
                                string line = (string)lineJson;
                                int split = line.IndexOf(": ");
                                OxideDecompLine decompLine = new OxideDecompLine(line.Substring(split + 2));
                                string lineNoStr = line.Substring(0, split);
                                int lineNo = Int32.Parse(lineNoStr);
                                lineDict[lineNo] = decompLine;
                            }
                        }
                    }

                    // Now, associate decomp lines with functions. 
                    // The data returned by Nexus/Oxide/Ghidra only tells us the decomp code
                    // for each offset within the entire binary. Now we have to go through
                    // and find out what offsets are associated with each function
                    // in order to determine what code lines are associated with each function. So... 
                    // For each function...
                    foreach (OxideFunction function in binary.functionDict.Values)
                    {
                        function.decompDict = new SortedDictionary<int, OxideDecompLine>();

                        // For each basic block...                        
                        foreach (OxideBasicBlock block in function.basicBlockDict.Values)
                        {
                            // For each offset in the block...
                            foreach (string instructionAddress in block.instructionAddressList)
                            {
                                // See if that offset has associated decomp lines that we harvested
                                // in the first half of this function, and add them to 
                                // decompDict for this function. 
                                int offset = Int32.Parse(instructionAddress);
                                if (binary.decompMapDict.ContainsKey(offset))
                                {
                                    foreach (KeyValuePair<int, OxideDecompLine> item in binary.decompMapDict[offset])
                                    {
                                        int lineNo = item.Key;
                                        OxideDecompLine decompLine = item.Value;
                                        function.decompDict[lineNo] = decompLine;

                                        // Also, add this offset to the associatedOffsets list
                                        // if it's not already there.
                                        if (decompLine.associatedOffsets == null)
                                        {
                                            decompLine.associatedOffsets = new List<int>();
                                        }
                                        if (!decompLine.associatedOffsets.Contains(offset))
                                        {
                                            decompLine.associatedOffsets.Add(offset);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
 
                Debug.Log($"=== For binary {binary.name}: {binary.decompMapDict.Keys.Count} instructions with decompiled code.");
                return binary; 
            });
        }

        // Given an Oxide module name and an OID, run the module on the OID and return the results as a string.
        public async Task<string> RetrieveTextForArbitraryModule(string moduleName, string oid, string parameters, bool firstOIDOnly)
        {
            // This async approach courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
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
