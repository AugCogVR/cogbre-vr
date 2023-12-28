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
                        OxideBinary binary = new OxideBinary(oid, binaryNameList[0], size, null, null, null);                    
                        // -> Log binary
                        binaryList.Add(binary);
                    }
                    collection.binaryList = binaryList;
                }
                Debug.Log($"=== For collection {collection.name}: {collection.binaryList.Count} binaries.");
                return collection;
            });
        }

        // Pull all the info for a binary from Oxide if not already populated. 
        // This approach lets us pull the info on an as-needed basis 
        // (we'll never pull it for a binary no one selects).
        public async Task<OxideBinary> EnsureBinaryInfo(OxideBinary binary)
        {
            // This async approach courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
            return await Task.Run<OxideBinary>(async () =>
            {
                // Pull the disassembly into a dict of instructions, keyed by offset
                if (binary.instructionDict == null)
                {
                    binary.instructionDict = new SortedDictionary<int, OxideInstruction>();
                    string disassemblyJsonString = await NexusSyncTask("[\"oxide_get_disassembly\", \"" + binary.oid + "\"]");
                    if (disassemblyJsonString != null) 
                    {
                        JsonData disassemblyJson = JsonMapper.ToObject(disassemblyJsonString)[binary.oid]["instructions"];
                        foreach (KeyValuePair<string, JsonData> item in disassemblyJson)
                        {
                            OxideInstruction oxideInstruction = new OxideInstruction((string)(item.Key));
                            oxideInstruction.mnemonic = (string)(item.Value["mnemonic"]);
                            oxideInstruction.op_str = (string)(item.Value["op_str"]);
                            oxideInstruction.str = (string)(item.Value["str"]);
                            int key = Int32.Parse(item.Key);
                            binary.instructionDict[key] = oxideInstruction;
                        }
                    }
                    else 
                    {
                        binary.instructionDict[0] = new OxideInstruction("0");
                    }
                }

                // Pull the function info
                if (binary.functionDict == null)
                {
                    binary.functionDict = new SortedDictionary<int, OxideFunction>();
                    string functionsJsonString = await NexusSyncTask("[\"oxide_get_function_info\", \"" + binary.oid + "\"]");
                    if (functionsJsonString != null) 
                    {
                        JsonData functionsJson = JsonMapper.ToObject(functionsJsonString)[binary.oid];
                        foreach (KeyValuePair<string, JsonData> item in functionsJson)
                        {
                            int key = (int)(item.Value["offset"]);
                            string name = (string)(item.Key);
                            string offset = $"{item.Value["offset"]}";
                            string signature = (string)(item.Value["signature"]);
                            binary.functionDict[key] = new OxideFunction(name, offset, signature, null);
                        }
                    }
                }

                // Pull the basic block info
                if (binary.basicBlockDict == null)
                {
                    binary.basicBlockDict = new SortedDictionary<int, OxideBasicBlock>();
                    string basicBlocksJsonString = await NexusSyncTask("[\"oxide_get_basic_blocks\", \"" + binary.oid + "\"]");
                    if (basicBlocksJsonString != null) 
                    {
                        JsonData basicBlocksJson = JsonMapper.ToObject(basicBlocksJsonString)[binary.oid];
                        foreach (KeyValuePair<string, JsonData> item in basicBlocksJson)
                        {
                            List<string> instructionAddressList = new List<string>();
                            foreach (JsonData addr in item.Value["members"])
                            {
                                instructionAddressList.Add($"{addr}");
                            }
                            List<string> destinationAddressList = new List<string>();
                            foreach (JsonData addr in item.Value["dests"])
                            {
                                destinationAddressList.Add($"{addr}");
                            }
                            int key = Int32.Parse(item.Key);
                            binary.basicBlockDict[key] = new OxideBasicBlock(item.Key, instructionAddressList, destinationAddressList);
                        }
                    }
                }

                // Annotate which basic blocks belong to which functions
                // First, find at which addresses each function starts and (notionally) ends.
                // Build a temporary Dictionary with this info
                // TODO: (maybe) save this info in the binary object 
                Dictionary<OxideFunction, IList<int>> funcStartEndDict = new Dictionary<OxideFunction, IList<int>>();
                List<OxideFunction> tmpFuncList = new List<OxideFunction>(binary.functionDict.Values);
                int numFuncs = tmpFuncList.Count;
                for (int currFunc = 0; currFunc < numFuncs; currFunc++)
                {
                    OxideFunction func = tmpFuncList[currFunc];
                    func.basicBlockDict = new SortedDictionary<int, OxideBasicBlock>();
                    int funcStart = Int32.Parse(func.offset);
                    int funcEnd = Int32.MaxValue;
                    if (currFunc < (numFuncs - 1))
                    {
                        funcEnd = Int32.Parse(tmpFuncList[currFunc + 1].offset) - 1;
                    }
                    funcStartEndDict[func] = new List<int>(){ funcStart, funcEnd };
                    // Debug.Log($"##### Func {func.offset}: {funcStart}, {funcEnd}");
                }
                // Next, find which basic blocks fit within each function and record in func objects
                // using a terrible double loop (at least the inner loop is the smaller one!)
                foreach (KeyValuePair<int, OxideBasicBlock> blockItem in binary.basicBlockDict)
                {
                    int blockStart = blockItem.Key;
                    foreach (KeyValuePair<OxideFunction, IList<int>> funcItem in funcStartEndDict)
                    {
                        OxideFunction func = funcItem.Key;
                        int funcStart = funcItem.Value[0];
                        int funcEnd = funcItem.Value[1];
                        if ((funcStart <= blockStart) && (blockStart <= funcEnd))
                        {
                            func.basicBlockDict[blockItem.Key] = blockItem.Value;
                            // Debug.Log($"##### Func {func.offset}, {funcStart}, {funcEnd} has basic block {blockStart}");
                        }
                    }
                }

                Debug.Log($"=== For binary {binary.name}: {binary.functionDict.Keys.Count} functions, {binary.basicBlockDict.Keys.Count} basic blocks, {binary.instructionDict.Keys.Count} instructions.");
                return binary; 
            });
        }

        // Given an Oxide module name and an OID, run the module on the OID and return the results as a string.
        public async Task<string> RetrieveTextForArbitraryModule(string moduleName, string oid, string parameters)
        {
            string returnMe = "";

            // This async approach courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
            return await Task.Run<string>(async () =>
            {
                returnMe = await NexusSyncTask($"[\"oxide_retrieve\", \"{moduleName}\", [\"{oid}\"], {parameters}]");
                return returnMe;
            });
        }
    }
}
