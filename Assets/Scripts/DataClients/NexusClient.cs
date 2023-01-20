using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NexusClient  // : MonoBehaviour  // Later we might want to make this a MonoBehavior again...?
{
    GameManager _gameManager;

    public int pacingCounter;

    public NexusClient(GameManager gameManager)
    {
        //Debug.Log("NexusClient Constructor");

        _gameManager = gameManager;

        pacingCounter = 0;

        NexusSync("initial");
    }

    // OnUpdate is called by Game Manager Update
    public void OnUpdate()
    {
        pacingCounter++;
        int pacingCounterLimit = 1000;
        if (pacingCounter > pacingCounterLimit)
        {
            pacingCounter = 0;

            NexusSync("update");       
        }
    }

    public async void NexusSync(string command)
    {
        string jsonResponse = await NexusSyncTask(12345, command);
        Debug.Log("NexusSync JSON response: " + jsonResponse);
        Debug.Log("NexusSync cycles used: " + pacingCounter);
    }

    private async Task<string> NexusSyncTask(int userId, string command)
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
        string jsonResponse = reader.ReadToEnd();
        reader.Close();
        response.Close();

        //Debug.Log("NexusSyncTask EXIT");
        return jsonResponse;
    }
}
