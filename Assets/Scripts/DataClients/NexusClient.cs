using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using TMPro;

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
        // Get basic program blocks from Nexus
        string basicBlocksJSON = await NexusSyncTask(userId, "session_init");
        IList<IDictionary<string, IDictionary<string, BasicBlock>>> pointlessList = 
            JsonConvert.DeserializeObject<IList<IDictionary<string, IDictionary<string, BasicBlock>>>>(basicBlocksJSON);
        IDictionary<string, BasicBlock> blockDict = pointlessList[0]["basic_blocks"];
        // Debug.Log("XXXXXXXXXXX " + blocks.Count);   
        // Debug.Log("XXXXXXXXXXX " + blocks.ElementAt(2).Key);   
        // BasicBlock bb = blocks.ElementAt(2).Value;
        // Debug.Log("XXXXXXXXXXX " + bb.last_insn);   

        // Build a scene of nonsense from the block data
        foreach (string blockKey in blockDict.Keys)
        {
            // Create a new cube
            GameObject prefab = Resources.Load("Prefabs/Cube") as GameObject;
            var position = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(2.0f, 5.0f), Random.Range(-5.0f, 5.0f));
            GameObject newCube = Instantiate(prefab, position, Quaternion.identity);
            gameManager.cubeDict[blockKey] = newCube;
            Debug.Log("ADD KEY " + blockKey);
            newCube.GetComponent<CubeManager>().OnStart(gameManager);

            // Set random color
            var cubeRenderer = newCube.GetComponent<Renderer>();
            Color newColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1.0f);
            cubeRenderer.material.SetColor("_Color", newColor);

            // Add object to hold text 
            GameObject textHolder = new GameObject();
            textHolder.transform.parent = newCube.transform;

            // Create text mesh and attach to text holder object; position above cube
            TextMeshPro textObject = textHolder.AddComponent<TextMeshPro>();
            RectTransform rectTransform = textHolder.GetComponent<RectTransform>();
            rectTransform.localPosition = new Vector3(0, 1.0f, 0);
            //rectTransform.sizeDelta = new Vector2(400, 200);

            // Set text contents and style
            textObject.font = Resources.Load("Fonts/LiberationSans", typeof(TMP_FontAsset)) as TMP_FontAsset;
            textObject.color = new Color(0,0,0,1.0f);
            textObject.text = blockKey;
            textObject.fontSize = 1;  
            //textObject.autoSizeTextContainer = true;
            textObject.alignment = TextAlignmentOptions.Center;
        }

        foreach (string blockKey in blockDict.Keys)
        {
            Debug.Log("BLOCK CHECK KEY " + blockKey);
            BasicBlock block1 = blockDict[blockKey];
            Debug.Log("CUBE CHECK KEY " + blockKey);
            GameObject cube1 = gameManager.cubeDict[blockKey];

            // Scale size of cube per number of members
            cube1.transform.localScale += (new Vector3(1,1,1) * block1.members.Count * 0.05f);

            // Draw lines from blocks to target blocks
            foreach(int target in block1.targets)
            {
                Debug.Log("LINE CHECK KEY " + target);
                if (gameManager.cubeDict.ContainsKey("" + target))
                {
                    GameObject cube2 = gameManager.cubeDict["" + target];
                    
                    // Add object to hold line
                    GameObject lineHolder = new GameObject();
                    lineHolder.transform.parent = cube1.transform;

                    // Make line
                    LineRenderer lr = lineHolder.AddComponent<LineRenderer>();
                    lr.material.SetColor("_Color", Color.red);
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.05f;
                    lr.SetPosition(0, cube1.transform.position);
                    lr.SetPosition(1, cube2.transform.position);
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
}
