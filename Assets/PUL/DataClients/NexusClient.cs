using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

namespace PUL
{
    [System.Serializable]
    public class OxideBasicBlock
    {
        public int first_insn { get; set; }
        public int last_insn { get; set; }
        public int num_insns { get; set; }
        public IList<int> members { get; set; }
        public IList<int> targets { get; set; }
        public string hash { get; set; }
        public IDictionary<string, string> insns {get; set; }
    }

    [System.Serializable]
    public class OxideBasicBlocksDict
    {
        public IDictionary<string, OxideBasicBlock> basic_blocks { get; set; }
    }

    public class NexusClient : MonoBehaviour
    {
        GameManager gameManager;

        public int pacingCounter;

        public float timeBeforeNodesAnchor = 5f;

        private string userId;

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

        private async void NexusSessionInit()
        {
            string sessionInitResult = await NexusSyncTask(userId, "session_init");

            // Get basic program blocks from Nexus
            string oxideBasicBlocksJSON = await NexusSyncTask(userId, "get_oxide_program elf_fib_recursive");
            IDictionary<string, OxideBasicBlock> oxideBlockDict = JsonConvert.DeserializeObject<IDictionary<string, OxideBasicBlock>>(oxideBasicBlocksJSON);
            // Build graph from program blocks
            buildGraphFromOxideBlocks(oxideBlockDict);
        }

        private void buildGraphFromOxideBlocks(IDictionary<string, OxideBasicBlock> oxideBlockDict)
        {
            Dictionary<string, SimpleCubeNode> nodeDict = new Dictionary<string, SimpleCubeNode>();

            int nodeCounter = 0;

            foreach (KeyValuePair<string, OxideBasicBlock> keyValue in oxideBlockDict)
            {
                string codeString = getCodeString(keyValue.Value);
                SimpleCubeNode scn = SimpleCubeNode.New(keyValue.Key, codeString);
                scn.transform.parent = gameManager.codeGraph.transform;
                scn.transform.localPosition = new Vector3(Random.Range(-15.0f, 8.0f), Random.Range(1f, 10.0f), Random.Range(-10.0f, 10.0f));
                nodeDict.Add(keyValue.Key, scn);
                gameManager.codeGraph.AddNodeToGraph(scn, nodeCounter, keyValue.Value.members.Count);
                nodeCounter++;
            }

            foreach (string blockKey in oxideBlockDict.Keys)
            {
                // Debug.Log("BLOCK CHECK KEY " + blockKey);
                OxideBasicBlock block1 = oxideBlockDict[blockKey];
                // Debug.Log("CUBE CHECK KEY " + blockKey);
                SimpleCubeNode cube1 = nodeDict[blockKey];

                // Scale size of cube per number of members
                cube1.transform.localScale += (new Vector3(1.0f, 1.0f, 1.0f) * block1.members.Count * 0.1f);

                // Create graph edges between nodes associated with connected blocks
                foreach(int target in block1.targets)
                {
                    if (nodeDict.ContainsKey("" + target))
                    {
                        SimpleCubeNode cube2 = nodeDict["" + target];
                        string edgeName = $"edge: {cube1.name} - {cube2.name}";
                        BasicEdge newEdge = BasicEdge.New(edgeName);
                        //newEdge.transform.parent = cube1.transform; // don't think we need to do this
                        newEdge.NodeA = cube1.transform;
                        newEdge.NodeB = cube2.transform;
                        gameManager.codeGraph.AddEdgeToGraph(cube1, cube2);
                        cube1.MyEdges.Add(newEdge);
                        cube2.MyEdges.Add(newEdge);
                    }
                }
            }

            gameManager.codeGraph.StartGraph();
            gameManager.codeGraph.setLoneValuesImmobile();
        }

        private string getCodeString(OxideBasicBlock oxideBlock)
        {
            string returnMe = "";

            foreach(string insn in oxideBlock.insns.Values)
            {
                returnMe = returnMe + insn + "\n";
            }

            // Debug.Log(returnMe);
            return returnMe;
        }
         
        private async void NexusUpdate()
        {
            string whatever = await NexusSyncTask(userId, "get_session_update");
        }

        private async Task<string> NexusSyncTask(string userId, string command)
        {
            //Debug.Log("NexusSyncTask ENTER");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:5000/sync_portal");
            // request.keepAlive = false;
            request.ContentType = "application/json";
            request.Method = "POST";
            StreamWriter writer = new StreamWriter(await request.GetRequestStreamAsync());
            string jsonRequest = "{\"userId\":\"" + userId 
                + "\", \"command\":\"" + command + "\"}";
            // Debug.Log("NexusSync JSON request:" + jsonRequest);
            writer.Write(jsonRequest);
            writer.Close();

            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string responseStringJson = reader.ReadToEnd();
            reader.Close();
            response.Close();
            // Debug.Log("NexusSyncTask cycles used: " + pacingCounter);

            string responseString = JsonConvert.DeserializeObject<string>(responseStringJson);
            // Debug.Log("NexusSync response string:" + responseString);

            return responseString;
        }
    }
}
