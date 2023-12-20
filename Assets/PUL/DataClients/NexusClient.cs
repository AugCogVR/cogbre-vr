using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel;
using Unity.VisualScripting;
using System.Globalization;
using static UnityEngine.UI.Image;
using Mono.Reflection;
using LitJson;
using PUL;

namespace PUL
{

    [System.Serializable]
    public class OxideData
    {
        public IList<OxideCollection> collectionList { get; set; }

        public OxideData()
        {
            collectionList = new List<OxideCollection>();
        }

        public override string ToString()
        {
            string output = "__OxideCollections__\n";

            //  Print out all collections
            foreach (OxideCollection collection in collectionList)
            {
                output += collection.ToString() + "\n";
            }

            return output;
        }
    }


    [System.Serializable]
    public class OxideCollection
    {
        public string collectionId { get; set; }
        public string name { get; set; }
        public string notes { get; set; }
        public IList<OxideBinary> binaryList { get; set; }

        public OxideCollection(string collectionId, string name, string notes, IList<OxideBinary> binaryList) 
        { 
            this.collectionId = collectionId;
            this.name = name;
            this.notes = notes;  // ???
            this.binaryList = binaryList;
        }

        public override string ToString()
        {
            string output = $"CID: {collectionId} || Name: {name} || Notes: {notes}";

            // Print out oids
            if ((binaryList != null) && (binaryList.Count > 0)) 
            {
                output += "\n\t->Binaries\n";
                foreach (OxideBinary binary in binaryList) 
                { 
                    output += "\t" + binary.ToString() + "\n";
                }
            }

            return output;
        }
    }

    // [System.Serializable]
    // public class Operand
    // {
    //     public class OperandMemory
    //     {
    //         public string memoryBase;
    //         public string displacement;
    //     }

    //     public string type_reg;
    //     public OperandMemory type_mem;
    //     public string type_imm;
    //     public int size;
    //     public string access;
    // }

    // [System.Serializable]
    // public class NexusValue
    // {
    //     public int id;
    //     public string mnemonic;
    //     public int address;
    //     public string op_str;
    //     public int size;
    //     public string str;
    //     public string[] groups;
    //     public string[] regs_read;
    //     public string[] regs_write;
    //     public List<List<string>> regs_access;
    //     public int[] prefix;
    //     public int[] opcode;
    //     public int rex;
    //     public int operand_size;
    //     public int modrm;
    //     public List<Operand> operands;
    // }

    [System.Serializable]
    public class OxideBinary
    {
        public string oid { get; set; }
        public string name { get; set; }
        public string size { get; set; }

        // public IList<string> originalPaths { get; set; }
        // Keep the next fields so that DisassemblyFormatter will compile
        public string dissasemblyPath { get; set; }
        public Dictionary<string, string> dissasembly { get; set; }
        public string dissasemblyOut { get; set; }


        public OxideBinary(string oid, string name, string size /*IList<string> originalPaths, string dissasemblyPath,*/)
        {
            this.oid = oid;
            this.name = name;
            this.size = size;

            // this.originalPaths = originalPaths;
            // this.dissasemblyPath = dissasemblyPath;
            // dissasembly = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            string output = $"OID: {oid} || Name: {name} || Size: {size}";

            // if(originalPaths.Count > 0) output += "\n\t\t-- > Original Paths\n";
            // // print out original paths
            // foreach (string path in originalPaths)
            // {
            //     output += "\t\t\t" + path + "\n";
            // }
            // output += $"\n\t\t-- > Disassembly: {dissasemblyPath}";

            return output;
        }
    }

    [System.Serializable]
    public class NexusClient : MonoBehaviour
    {
        GameManager gameManager;

        public int pacingCounter;

        private string userId;

        public OxideData oxideData;
        

        public NexusClient(GameManager gameManager)
        {
            //Debug.Log("NexusClient Constructor");

            this.gameManager = gameManager;

            pacingCounter = 0;

            userId = "User123";

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
        public async Task<IList<OxideBinary>> GetBinaryListForCollection(OxideCollection collection)
        {
            // This async method courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
            return await Task.Run<IList<OxideBinary>>(async () =>
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

                    // -> Grab OID paths
                    // IList<string> paths = new List<string>();

                    // -> Grab OID size
                    string size = await NexusSyncTask($"[\"oxide_get_oid_file_size\", \"{oid}\"]");

                    // DGB: Skip this version of obtaining disassembly for now.
                    // Call GetDisassemblyText on demand instead.
                    // Open up a file stream
                    // StreamWriter sw = null;
                    // // -> Get disassembly via Nexus
                    // string disasm = await NexusSyncTask("[\"oxide_get_disassembly\", \"" + oid + "\"]");
                    // if (disasm == null) disasm = "null... Check for 500 error.";
                    // // Debug.Log("DISASM: " + disasm);
                    // // Chop out unnessesary information
                    // int startIndex = disasm.IndexOf("\"instructions\"") + 16;
                    // disasm = disasm.Substring(startIndex, disasm.Length - 2 - startIndex);
                    // // IList<string> dissasmPull = JsonConvert.DeserializeObject<IList<string>>(dissasm);
                    // // -> Store disassem in a file
                    // // storedData/collectionID/objectID.txt
                    // // Application.persistentDataPath should be something like C:\Users\<you>\AppData\LocalLow\DefaultCompany\cogbre\storedData
                    // string disamDirectory = Application.persistentDataPath + $"/storedData/{cid}";
                    // string fileName = $"{oid}.json";
                    // // If directory does not exist, create one
                    // if (!Directory.Exists(disamDirectory))
                    //     Directory.CreateDirectory(disamDirectory);
                    // // Write info to file
                    // sw = new StreamWriter(disamDirectory + "/" + fileName);
                    // await sw.WriteAsync(disasm);
                    // // Compile information together into a new OID object
                    // // -> Create oid
                    // OxideBinary finalOID = new OxideBinary(oid, oName[0], paths, disamDirectory + "/" + fileName, size);
                    // // -> Format the information within the oid
                    // sw.Close();
                    // gameManager.disassemblyFormatter.ParseDisassembly(finalOID);

                    OxideBinary binary = new OxideBinary(oid, binaryNameList[0], size);                    
                    // -> Log binary
                    binaryList.Add(binary);
                }

                return binaryList;
            });
        }

        // Return a string containing the human-readable disassembly of the given binary
        public async Task<string> GetDisassemblyTextForBinary(OxideBinary binary)
        {
            // This async method courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
            return await Task.Run<string>(async () =>
            {
                string disasmJson = await NexusSyncTask("[\"oxide_get_disassembly_strings_only\", \"" + binary.oid + "\"]");
                string returnText = $"Disassembly for {binary.name}\n";
                if (disasmJson != null) 
                {
                    JsonData instructions = JsonMapper.ToObject(disasmJson)[binary.oid]["instructions"];
                    int arbitraryLimit = 50; // This limit is TEMPORARY and used for troubleshooting
                    int count = 0;
                    foreach (KeyValuePair<string, JsonData> item in instructions)
                    {
                        returnText += item.Key + " " + item.Value["str"] + "\n";
                        if (++count > arbitraryLimit) break;
                    }
                }
                else 
                {
                    returnText += "null... Check for 500 error.";
                }

                // TODO: Fill in the disassembly dictionary in the object 

                return returnText;
            });
        }


    }
}
