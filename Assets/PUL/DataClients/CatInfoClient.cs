using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    public class CatInfoClient
    {
        public class CatInfo
        {
            public string fact;
            public int length;
        }

        GameManager _gameManager;

        public int uselessCounter;

        public CatInfoClient(GameManager gameManager)
        {
            Debug.Log("CatInfoClient Constructor");

            _gameManager = gameManager;

            uselessCounter = 0; 

            ReportCatFact();
        }

        // OnUpdate is called by Game Manager Update
        public void OnUpdate()
        {
            uselessCounter++;
            int uselessCounterLimit = 1000;
            if (uselessCounter > uselessCounterLimit)
            {
                Debug.Log("CatInfoClient ONUPDATE / " + uselessCounterLimit);
                uselessCounter = 0;
                ReportCatFact();
                //Debug.Log("CatInfoClient ONUPDATE EXIT");
            }
            
        }

        public async void ReportCatFact()
        {
            CatInfo catInfo = await GetCatInfo();
            Debug.Log("CatInfoClient REPORT: " + catInfo.fact);
            Debug.Log("CatInfoClient COUNTER: " + uselessCounter);
        }

        private async Task<CatInfo> GetCatInfo()
        {
            //Debug.Log("CatInfoClient GETCATINFO START");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://catfact.ninja/fact");
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string jsonResponse = reader.ReadToEnd();
            CatInfo catInfo = JsonUtility.FromJson<CatInfo>(jsonResponse);
            //Debug.Log("CatInfoClient GETCATINFO END");
            return catInfo;
        }
    }
}
