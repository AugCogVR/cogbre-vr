using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NexusClient  // : MonoBehaviour  // Later we might want to make this a MonoBehavior again...?
{
    public class FileStats
    {
        public string whatever;
    }

    GameManager _gameManager;

    public int uselessCounter;

    public NexusClient(GameManager gameManager)
    {
        Debug.Log("NexusClient Constructor");

        _gameManager = gameManager;

        uselessCounter = 0; 

        PingNexus();
    }

    // OnUpdate is called by Game Manager Update
    public void OnUpdate()
    {
        uselessCounter++;
        int uselessCounterLimit = 1000;
        if (uselessCounter > uselessCounterLimit)
        {
            Debug.Log("NexusClient ONUPDATE / " + uselessCounterLimit);
            uselessCounter = 0;
            PingNexus();
        }
        
    }

    public async void PingNexus()
    {
        FileStats fileStats = await GetFileStats(12345);
        Debug.Log("NexusClient DATA: " + fileStats.whatever);
        Debug.Log("NexusClient COUNTER: " + uselessCounter);
    }

    private async Task<FileStats> GetFileStats(int userId)
    {
        //Debug.Log("NexusClient GETFILESTATS ENTER");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:5000/filestats");
        // request.keepAlive = false;
        request.ContentType = "application/json";
        request.Method = "POST";
        StreamWriter writer = new StreamWriter(await request.GetRequestStreamAsync());
        string jsonRequest = "{\"userId\":\"" + userId + "\"}";
        //Debug.Log("REQUEST:" + jsonRequest);
        writer.Write(jsonRequest);
        writer.Close();

        HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
        StreamReader reader = new StreamReader(response.GetResponseStream());
        string jsonResponse = reader.ReadToEnd();
        reader.Close();
        response.Close();

        FileStats fileStats = JsonUtility.FromJson<FileStats>(jsonResponse);

        //Debug.Log("NexusClient GETFILESTATS EXIT");
        return fileStats;
    }
}
