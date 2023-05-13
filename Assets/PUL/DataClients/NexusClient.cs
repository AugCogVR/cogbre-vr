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
    public class OxideBasicBlocks
    {
        public IDictionary<string, OxideBasicBlock> basic_blocks { get; set; }
    }


    [System.Serializable]
    public class CompVizBlock
    {
        public IList<string> lines { get; set; }
        public IList<string> targets { get; set; }
    }


    [System.Serializable]
    public class CompVizStage
    {
        public string type { get; set; }
        public string id { get; set; }
        public IDictionary<string, string> code { get; set; }
        public IDictionary<string, CompVizBlock> blocks { get; set; }
    }


    [System.Serializable]
    public class CompVizStages
    {
        public IList<CompVizStage> stages { get; set; }
        public IList<IList<IList<string>>> blockRelations { get; set; }
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

      public async void NexusSessionInit()
        {
            string sessionInitResult = await NexusSyncTask(userId, "session_init");

            // OXIDE
            // Get basic program blocks from Nexus
            //string oxideBasicBlocksJSON = await NexusSyncTask(userId, "get_oxide_program elf_fib_recursive");
            //OxideBasicBlocks oxideBasicBlocks = new OxideBasicBlocks();
            //oxideBasicBlocks.basic_blocks = JsonConvert.DeserializeObject<IDictionary<string, OxideBasicBlock>>(oxideBasicBlocksJSON);


            // COMPILER VISUALIZATION

            string compVizStagesJSON = await NexusSyncTask(userId, "get_compviz_stages perfect-func");
            //Debug.Log("COMP VIZ " + compVizStagesJSON);
            CompVizStages cvs = JsonConvert.DeserializeObject<CompVizStages>(compVizStagesJSON);
            this.gameManager.cvs = cvs;
            setNumberOfGraphs(cvs);
        }

        public void setNumberOfGraphs(CompVizStages cvs)
        {
        this.gameManager.graphTotal = cvs.stages.Count;
        }

        public void buildGraph2FromOxideBlocks(CompVizStages oxideBlockDict, ref List<GameObject> graphHolders)
        {
           
            int graphHolderIndex = 0;
          
            
            foreach (CompVizStage stage in oxideBlockDict.stages) { 
                foreach(KeyValuePair<string, CompVizBlock> node in stage.blocks)
                {
                    graphHolders[graphHolderIndex].GetComponent<Graph>().nodePrefab = Resources.Load("Prefabs/GraphNode") as GameObject;
                    graphHolders[graphHolderIndex].GetComponent<Graph>().edgePrefab = Resources.Load("Prefabs/Edge") as GameObject;

                    List<string> codeValues = new List<string>();
                    foreach (string lineNumber in node.Value.lines)
                    {
                        codeValues.Add(stage.code[lineNumber]);
                    }

                    graphHolders[graphHolderIndex].GetComponent<Graph>().AddNodeToGraph(node.Key, codeValues);
                    graphHolders[graphHolderIndex].GetComponent<Graph>().totalNodes[node.Key].transform.parent = graphHolders[graphHolderIndex].transform;
                    graphHolders[graphHolderIndex].GetComponent<Graph>().totalNodes[node.Key].transform.position = graphHolders[graphHolderIndex].GetComponent<Graph>().totalNodes[node.Key].transform.parent.position;
                }
            
                graphHolderIndex += 1;
            }

            //this is an absolutely disgusting function. we'll see if this works but lets see if we can come back to this and make it cleaner.
            //get every graph inside graphHolders
            int i = 0;
            
            foreach (GameObject graphHolder in graphHolders)
            {
                //create a list to store whether we have visited this node before.
                //initialize to contain every node, and ensure that no other node has been traveled to before.
                Dictionary<GameObject, bool> hasVisited = new Dictionary<GameObject, bool>();
                foreach(KeyValuePair<string, GameObject> node in graphHolder.GetComponent<Graph>().totalNodes)
                {
                    hasVisited.Add(node.Value, false);
                }
                
                //then, get every information block from the json
                foreach (CompVizStage stage in oxideBlockDict.stages)
                {
                    foreach (KeyValuePair<string, CompVizBlock> node in stage.blocks)
                    {
                        //next, compare the information stored in the node game objects with the information blocks
                        foreach (KeyValuePair<string, GameObject> nodeComparison in graphHolder.GetComponent<Graph>().totalNodes)
                        {
                            //Debug.LogWarning(nodeComparison.Key);
                            //if this node targets another node, add it to the list.
                            if (node.Value.targets.Contains(nodeComparison.Key))
                            {
                                Debug.LogWarning("Line 171 If Statement Works!");
                                GameObject parentNode = graphHolder.GetComponent<Graph>().totalNodes[node.Key];
                                GameObject edgePrefab = Object.Instantiate(graphHolder.GetComponent<Graph>().edgePrefab);
                                parentNode.GetComponent<Node>().AddChild(nodeComparison.Value, edgePrefab);
                                nodeComparison.Value.GetComponent<Node>().AddParent(parentNode, edgePrefab);
                                hasVisited[parentNode] = true;
                            }
                        }
                    
                    }
                }
                
                foreach(KeyValuePair<string, GameObject> node in graphHolder.GetComponent<Graph>().totalNodes)
                {
                    List<GameObject> childNodes = new List<GameObject>();
                    foreach (KeyValuePair<GameObject, GameObject> childNode in node.Value.GetComponent<Node>().childNodes) 
                    {
                        Debug.Log(childNode.Key);
                        childNodes.Add(childNode.Value);
                    }
                    //graphHolder.GetComponent<Graph>().AssignChildrenToNode(node.Value, childNodes);

                }

                GameObject StartNode = graphHolder.transform.GetChild(graphHolder.transform.childCount - 1).gameObject;
                GameObject ExitNode = graphHolder.transform.GetChild(0).gameObject;

                graphHolder.GetComponent<Graph>().ArrangeGraph(ExitNode, StartNode);
            }
        }

        private string getOxideCodeString(OxideBasicBlock oxideBlock)
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
