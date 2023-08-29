using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;


namespace PUL2
{

    [System.Serializable]
    public class ActiveOxideData
    {
        public IList<string> CIDs { get; set; }
    }


    public class NexusClient : MonoBehaviour
    {
        GameManager gameManager;

        public int pacingCounter;

        private string userId;



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
            string sessionInitResult = await NexusSyncTask(userId, "session_init");
            string activeOxideData = await NexusSyncTask(userId, "oxide_collection_names");
            
            
            ActiveOxideData aod = new ActiveOxideData();
            aod.CIDs = JsonConvert.DeserializeObject<IList<string>>(activeOxideData);
            this.gameManager.aod = aod;


        }

        private async void NexusUpdate()
        {
            string whatever = await NexusSyncTask(userId, "get_session_update");
        }

        private async Task<string> NexusSyncTask(string userId, string command)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:5000/sync_portal");
                request.ContentType = "application/json";
                request.Method = "POST";
                StreamWriter writer = new StreamWriter(await request.GetRequestStreamAsync());
                string jsonRequest = "{\"userId\":\"" + userId + "\", \"command\":\"" + command + "\"}";
                writer.Write(jsonRequest);
                writer.Close();

                HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string responseStringJson = reader.ReadToEnd();
                reader.Close();
                response.Close();

                // Log the JSON response before deserialization for debugging
                Debug.Log("NexusSync response JSON: " + responseStringJson);

                string responseString = JsonConvert.DeserializeObject<string>(responseStringJson);

                // Log the deserialized response string for debugging
                Debug.Log("NexusSync response string: " + responseString);

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
