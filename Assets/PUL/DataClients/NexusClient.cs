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

        public Collection(string id,  string name, string notes, IList<NexusObject> OIDs) 
        { 
            CID = id;
            Name = name;
            Notes = notes;
            this.OIDs = OIDs;
        }

        public override string ToString()
        {
            string output = $"CID: {CID} || Name: {Name} || Notes: {Notes}";

            if(OIDs.Count > 0) output += "\n\t->OIDs\n";
            // Print out oids
            foreach (NexusObject nObj in OIDs) 
            { 
                output += "\t" + nObj.ToString() + "\n";
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
        public string OID { get; set; }
        public string Name { get; set; }
        public IList<string> originalPaths { get; set; }
        public string dissasemblyPath { get; set; }
        public string Size { get; set; }
        public IList<NexusValue> instructions { get; set; }

        public NexusObject(string oID, string name, IList<string> originalPaths, string dissasemblyPath, string size)
        {
            OID = oID;
            Name = name;
            this.originalPaths = originalPaths;
            this.dissasemblyPath = dissasemblyPath;
            Size = size;
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
        

        public NexusClient (GameManager gameManager)
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
            Debug.Log("NExus Session Init is Running!");
            string sessionInitResult = await NexusSyncTask(userId,  "[\"session_init\"]");
            string activeCollectionNames = await NexusSyncTask(userId, "[\"oxide_collection_names\"]");

            // Store found CIDS in a temporary list then parse into Collection type
            IList<string> collectionNames = JsonConvert.DeserializeObject<IList<string>>(activeCollectionNames);

            // Open up a file stream
            StreamWriter sw = null;

            // Set aod
            aod = new ActiveOxideData();
            // Run through collectionNames and convert to CIDs
            foreach (string collectionName in collectionNames)
            {
                // Make sure collection is set properly
                string cid = await NexusSyncTask(userId, $"[\"oxide_get_cid_from_name\", \"{collectionName}\"]");
                cid = cid.Replace("\"", "");

                // UNUSED - Plan to pull notes out of information
                //string collectionInfo = await NexusSyncTask(userId, $"[\"oxide_get_collection_info\", \"{collectionName}\", \"all\"]");
                //Debug.Log("\t" + collectionInfo);

                // -> Get OIDs
                string oidPull = await NexusSyncTask(userId, $"[\"oxide_get_oids_with_cid\", \"{cid}\"]");
                // Debug.Log($"OID_pull (Collection: {cid}): {oidPull}");
                // -> Format OIDs
                IList<string> oids = JsonConvert.DeserializeObject<IList<string>>(oidPull);
                IList<NexusObject> OIDs = new List<NexusObject>();
                // Roll through each OID found, assign information
                foreach (string oid in oids)
                {
                    // -> Grab OID name
                    string oNamePull = await NexusSyncTask(userId, $"[\"oxide_get_names_from_oid\", \"{oid}\"]");
                    IList<string> oName = JsonConvert.DeserializeObject<IList<string>>(oNamePull);
                    // --> Make sure oName has contents
                    if (oName.Count <= 0)
                        oName.Add("Nameless OID");

                    //Debug.Log($"OID NAME: {oName[0]}");
                    // -> Grab OID paths
                    IList<string> paths = new List<string>();

                    // -> Grab OID size
                    string size = await NexusSyncTask(userId, $"[\"oxide_get_oid_file_size\", \"{oid}\"]");
                    
                    // -> DISSAM is not working (returning null)
                    string disasm = await NexusSyncTask(userId, "[\"oxide_get_disassembly\", [\"" + oid + "\"]]");
                    if (disasm == null) disasm = "null... Check for 500 error.";
                    // Chop out unnessesary information
                    int startIndex = disasm.IndexOf("\"instructions\"") + 16;
                    disasm = disasm.Substring(startIndex, disasm.Length - 2 - startIndex);
                    // IList<string> dissasmPull = JsonConvert.DeserializeObject<IList<string>>(dissasm);

                    // -> Store disassem in a file
                    // storedData/collectionID/objectID.txt
                    string disamDirectory = Application.persistentDataPath + $"/storedData/{cid}";
                    string fileName = $"{oid}.json";
                    // If directory does not exist, create one
                    if (!Directory.Exists(disamDirectory))
                        Directory.CreateDirectory(disamDirectory);
                    // Write info to file
                    sw = new StreamWriter(disamDirectory + "/" + fileName);
                    await sw.WriteAsync(disasm);

                    // Compile information together into a new OID object
                    // -> Create oid
                    NexusObject finalOID = new NexusObject(oid, oName[0], paths, disamDirectory + "/" + fileName, size);
                    // -> Format the information within the oid
                    sw.Close();
                    gameManager.disassemblyFormatter.ParseDisassembly(finalOID);
                    // -> Log oid
                    OIDs.Add(finalOID);
                }

                // Add collection to list
                aod.CIDs.Add(new Collection(cid, collectionName, "", new List<NexusObject>(OIDs)));
            }

            gameManager.menuManager.aod = aod;


            Debug.Log(aod);

            // Once all information is pulled initialize the menu
            gameManager.menuManager.MenuInit();
        }


        private async void NexusUpdate()
        {
            string whatever = await NexusSyncTask(userId, "[\"get_session_update\"]");
        }

        private async Task<string> NexusSyncTask(string userId, string command)
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
                Debug.Log(jsonRequest + "\nNexusSync response JSON: " + responseStringJson);

                string responseString = JsonConvert.DeserializeObject<string>(responseStringJson);

                // Log the deserialized response string for debugging
                Debug.Log(jsonRequest + "\nNexusSync response string: " + responseString);

                return responseString;
            }
            catch (Exception e)
            {
                // Log any exceptions that occur during the request
                Debug.LogError("Exception during NexusSyncTask: " + e.Message);
                return null; // Return null or an empty string as a default value on error
            }
        }

    }
}
