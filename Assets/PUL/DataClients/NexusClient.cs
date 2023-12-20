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
using PUL2;

namespace PUL2
{

    [System.Serializable]
    public class ActiveOxideData
    {
        public IList<Collection> CIDs { get; set; }

        public ActiveOxideData()
        {
            CIDs = new List<Collection>();
        }

        public override string ToString()
        {
            string output = "__Collections__\n";

            //  Print out all CIDs
            foreach (Collection collection in CIDs)
            {
                output += collection.ToString() + "\n";
            }

            return output;
        }
    }


    [System.Serializable]
    public class Collection
    {
        public string CID { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public IList<NexusObject> OIDs { get; set; }

        public Collection(string id, string name, string notes, IList<NexusObject> OIDs) 
        { 
            CID = id;
            Name = name;
            Notes = notes;
            this.OIDs = OIDs;
        }

        public override string ToString()
        {
            string output = $"CID: {CID} || Name: {Name} || Notes: {Notes}";

            // Print out oids
            if ((OIDs != null) && (OIDs.Count > 0)) 
            {
                output += "\n\t->OIDs\n";
                foreach (NexusObject nObj in OIDs) 
                { 
                    output += "\t" + nObj.ToString() + "\n";
                }
            }

            return output;
        }
    }

    [System.Serializable]
    public class Operand
    {
        public class OperandMemory
        {
            public string memoryBase;
            public string displacement;
        }

        public string type_reg;
        public OperandMemory type_mem;
        public string type_imm;
        public int size;
        public string access;
    }

    [System.Serializable]
    public class NexusValue
    {
        public int id;
        public string mnemonic;
        public int address;
        public string op_str;
        public int size;
        public string str;
        public string[] groups;
        public string[] regs_read;
        public string[] regs_write;
        public List<List<string>> regs_access;
        public int[] prefix;
        public int[] opcode;
        public int rex;
        public int operand_size;
        public int modrm;
        public List<Operand> operands;
    }

    [System.Serializable]
    public class NexusObject
    {
        // DGB: This class really should be named something like "BinaryInfo" since it's very specific 
        // to that use case while "NexusObject" has a much broader meaning

        public string OID { get; set; }
        public string Name { get; set; }
        public IList<string> originalPaths { get; set; }
        public string Size { get; set; }

        public string dissasemblyPath { get; set; }
        public Dictionary<string, string> dissasembly { get; set; }
        public string dissasemblyOut { get; set; }


        public NexusObject(string oID, string name, IList<string> originalPaths, string dissasemblyPath, string size)
        {
            OID = oID;
            Name = name;
            this.originalPaths = originalPaths;
            this.dissasemblyPath = dissasemblyPath;
            Size = size;

            dissasembly = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            string output = $"OID: {OID} || Name: {Name}";

            if(originalPaths.Count > 0) output += "\n\t\t-- > Original Paths\n";
            // print out original paths
            foreach (string path in originalPaths)
            {
                output += "\t\t\t" + path + "\n";
            }

            output += $"\n\t\t-- > Disassembly: {dissasemblyPath}";

            return output;
        }
    }

    [System.Serializable]
    public class NexusClient : MonoBehaviour
    {
        GameManager gameManager;

        public int pacingCounter;

        private string userId;

        public ActiveOxideData aod;
        

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
            string activeCollectionNames = await NexusSyncTask("[\"oxide_collection_names\"]");
            // Store found CIDS in a temporary list then parse into Collection type
            IList<string> collectionNames = JsonConvert.DeserializeObject<IList<string>>(activeCollectionNames);

            // Create aod and Collection objects
            aod = new ActiveOxideData();
            foreach (string collectionName in collectionNames)
            {
                string cid = await NexusSyncTask($"[\"oxide_get_cid_from_name\", \"{collectionName}\"]");
                cid = cid.Replace("\"", "");
                aod.CIDs.Add(new Collection(cid, collectionName, null, null));
            }
            gameManager.menuManager.aod = aod;
            Debug.Log(aod);

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
        public async Task<IList<NexusObject>> GetBinaryInfoForCollection(Collection collection)
        {
            // This async method courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
            return await Task.Run<IList<NexusObject>>(async () =>
            {
                // -> Get OIDs
                string oidPull = await NexusSyncTask($"[\"oxide_get_oids_with_cid\", \"{collection.CID}\"]");
                // Debug.Log($"OID_pull (Collection: {cid}): {oidPull}");
                // -> Format OIDs
                IList<string> oids = JsonConvert.DeserializeObject<IList<string>>(oidPull);
                IList<NexusObject> OIDs = new List<NexusObject>();
                // Roll through each OID found, assign information
                foreach (string oid in oids)
                {
                    // -> Grab OID name
                    string oNamePull = await NexusSyncTask($"[\"oxide_get_names_from_oid\", \"{oid}\"]");
                    IList<string> oName = JsonConvert.DeserializeObject<IList<string>>(oNamePull);
                    // --> Make sure oName has contents
                    if (oName.Count <= 0)
                        oName.Add("Nameless OID");

                    //Debug.Log($"OID NAME: {oName[0]}");
                    // -> Grab OID paths
                    IList<string> paths = new List<string>();

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
                    // NexusObject finalOID = new NexusObject(oid, oName[0], paths, disamDirectory + "/" + fileName, size);
                    // // -> Format the information within the oid
                    // sw.Close();
                    // gameManager.disassemblyFormatter.ParseDisassembly(finalOID);


                    NexusObject finalOID = new NexusObject(oid, oName[0], paths, null, size);                    
                    // -> Log oid
                    OIDs.Add(finalOID);
                }

                return OIDs;
            });
        }

        // Return a string containing the human-readable disassembly of the given binary
        public async Task<string> GetDisassemblyText(NexusObject binaryInfo)
        {
            // This async method courtesy of https://stackoverflow.com/questions/25295166/async-method-is-blocking-ui-thread-on-which-it-is-executing
            return await Task.Run<string>(async () =>
            {
                string disasmJSON = await NexusSyncTask("[\"oxide_get_disassembly_strings_only\", \"" + binaryInfo.OID + "\"]");
                string returnText = $"Disassembly for {binaryInfo.Name}\n";
                if (disasmJSON != null) 
                {
                    JsonData instructions = JsonMapper.ToObject(disasmJSON)[binaryInfo.OID]["instructions"];
                    int arbitraryLimit = 200; // This limit is TEMPORARY and used for troubleshooting
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
