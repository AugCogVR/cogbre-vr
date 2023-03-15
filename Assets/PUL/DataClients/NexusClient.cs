using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using FDG;
using FDG.Demo;

namespace PUL
{
    [System.Serializable]
    public class BasicBlock
    {
        public int first_insn { get; set; }
        public int last_insn { get; set; }
        public int num_insns { get; set; }
        public IList<int> members { get; set; }
        public IList<int> targets { get; set; }
        public string hash { get; set; }
    }

    [System.Serializable]
    public class BasicBlocksDict
    {
        public IDictionary<string, BasicBlock> basic_blocks { get; set; }
    }

    public class NexusClient : MonoBehaviour
    {
        GameManager gameManager;

        public int pacingCounter;

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
            // Uncomment to throw a random force-directed graph into the world
            //fdgTest();
            //return;

            // Get basic program blocks from Nexus
            string basicBlocksJSON = await NexusSyncTask(userId, "session_init");
            IList<IDictionary<string, IDictionary<string, BasicBlock>>> pointlessList = 
                JsonConvert.DeserializeObject<IList<IDictionary<string, IDictionary<string, BasicBlock>>>>(basicBlocksJSON);
            IDictionary<string, BasicBlock> blockDict = pointlessList[0]["basic_blocks"];
            // Debug.Log("XXXXXXXXXXX " + blocks.Count);   
            // Debug.Log("XXXXXXXXXXX " + blocks.ElementAt(2).Key);   
            // BasicBlock bb = blocks.ElementAt(2).Value;
            // Debug.Log("XXXXXXXXXXX " + bb.last_insn);   

            // Build graph from program blocks
            buildGraphFromBlocks(blockDict);
        }

        private void buildGraphFromBlocks(IDictionary<string, BasicBlock> blockDict)
        {
            Dictionary<string, SimpleCubeNode> nodeDict = new Dictionary<string, SimpleCubeNode>();

            int counter = 0;
            foreach (string blockKey in blockDict.Keys)
            {
                SimpleCubeNode scn = SimpleCubeNode.New(blockKey);
                nodeDict.Add(blockKey, scn);
                gameManager.codeGraph.AddNodeToGraph(scn, ++counter); 
            }

            foreach (string blockKey in blockDict.Keys)
            {
                // Debug.Log("BLOCK CHECK KEY " + blockKey);
                BasicBlock block1 = blockDict[blockKey];
                // Debug.Log("CUBE CHECK KEY " + blockKey);
                SimpleCubeNode cube1 = nodeDict[blockKey];

                // Scale size of cube per number of members
                cube1.transform.localScale += (new Vector3(1,1,1) * block1.members.Count * 0.05f);

                // Draw lines from blocks to target blocks
                foreach(int target in block1.targets)
                {
                    if (nodeDict.ContainsKey("" + target))
                    {
                        SimpleCubeNode cube2 = nodeDict["" + target];
                        string edgeName = $"edge: {cube1.name} - {cube2.name}";
                        BasicEdge newEdge = BasicEdge.New(edgeName);
                        newEdge.transform.parent = cube1.transform;
                        newEdge.NodeA = cube1.transform;
                        newEdge.NodeB = cube2.transform;

                        gameManager.codeGraph.AddEdgeToGraph(cube1, cube2);

                        cube1.MyEdges.Add(newEdge);
                        cube2.MyEdges.Add(newEdge);
                    }
                }
            }
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
            Debug.Log("NexusSync JSON request:" + jsonRequest);
            writer.Write(jsonRequest);
            writer.Close();

            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string responseStringJson = reader.ReadToEnd();
            reader.Close();
            response.Close();
            Debug.Log("NexusSyncTask cycles used: " + pacingCounter);

            string responseString = JsonConvert.DeserializeObject<string>(responseStringJson);
            Debug.Log("NexusSync response string:" + responseString);

            return responseString;
        }










        private void fdgTest()
        {
            // This chunk of code builds a demo Force-Directed Graph
            // using the FDG code at https://github.com/atonalfreerider/Unity-FDG 
            // that has been downloaded locally to Assets/FDG. 
            //
            // It's INCREDIBLY sensitive to the parameters, number of nodes, and number of connections. 
            // Incorrect values will cause objects to fly off at distances Unity can't handle, 
            // and wouldn't be practical anyway. In particular, changing the number of nodes 
            // can easily break it, and in our application, number of nodes is not predictable. 
            
            GameObject whatever = new GameObject("whatever");
            ForceDirectedGraph forceDirectedGraph = whatever.AddComponent<ForceDirectedGraph>() as ForceDirectedGraph;
            
            forceDirectedGraph.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            forceDirectedGraph.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            forceDirectedGraph.UniversalRepulsiveForce = 100;
            forceDirectedGraph.UniversalSpringForce = 15;
            forceDirectedGraph.TimeStep = 2;
            forceDirectedGraph.ForceCalcBatch = 1;
            
            int NumRandNodes = 70;
            int NumRandConnections = 130;
            
            GameObject nodeContainer;
            GameObject edgeContainer;
            
            nodeContainer = new GameObject("Nodes");
            edgeContainer = new GameObject("Edges");
            
            DemoNode node0 = DemoNode.New("node0");
            node0.transform.SetParent(nodeContainer.transform);
            forceDirectedGraph.AddNodeToGraph(node0, 0, 1, node0.UpdateMyEdges);
            node0.transform.position = Vector3.zero;
            forceDirectedGraph.SetNodeMobility(node0, true);
            
            Dictionary<int, DemoNode> randomNodes = new Dictionary<int, DemoNode> { { 0, node0 } };

            for (int i = 1; i < NumRandNodes; i++)
            {
                DemoNode newNode = DemoNode.New($"node{i}");
                newNode.transform.SetParent(nodeContainer.transform);
                forceDirectedGraph.AddNodeToGraph(newNode, i, 1, newNode.UpdateMyEdges);
                newNode.transform.position = new Vector3(
                    Random.Range(-10.0f, 10.0f),
                    Random.Range(0.5f, 10.0f),
                    Random.Range(-10.0f, 10.0f));
                randomNodes.Add(i, newNode);
            }

            for (int i = 0; i < NumRandConnections; i++)
            {
                int randA = Random.Range(0, NumRandNodes);
                int randB = Random.Range(0, NumRandNodes);
                if (randA == randB) continue;
                DemoNode randNodeA = randomNodes[randA];
                DemoNode randNodeB = randomNodes[randB];
                string edgeName1 = $"edge: {randNodeA.name} - {randNodeA.name}";
                DemoEdge newDemoEdge = DemoEdge.New(edgeName1);
                newDemoEdge.transform.SetParent(edgeContainer.transform);
                newDemoEdge.NodeA = randNodeA.transform;
                newDemoEdge.NodeB = randNodeB.transform;
                forceDirectedGraph.AddEdgeToGraph(randNodeA, randNodeB);
                randNodeA.MyEdges.Add(newDemoEdge);
                randNodeB.MyEdges.Add(newDemoEdge);
            }

            forceDirectedGraph.StartGraph();

            return;
        }

    }
}
