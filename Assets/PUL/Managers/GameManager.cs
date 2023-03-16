using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    public class GameManager : MonoBehaviour
    {
        //#region Singleton

        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("GameManager is NULL");
                }
                
                return _instance;
            }
        }

        // TODO: We need to support more than a single graph of a single type, obviously, but we're here for now.
        public RandomGraph codeGraph;

        public NexusClient nexusClient;

        private void Awake()
        {
            // https://bergstrand-niklas.medium.com/setting-up-a-simple-game-manager-in-unity-24b080e9516c
            // If another game manager exists, destroy that game object. If no other game manager exists, 
            // initialize the instance to itself. As a game manager needs to exist throughout all scenes, 
            // call the function DontDestroyOnLoad.
            if (_instance)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            DontDestroyOnLoad(this);
        }

        // Start is called before the first frame update
        void Start()
        {
            //Debug.Log("GameManager START");

            // Initialize scene objects
            codeGraph = new RandomGraph();

            // Initialize Nexus client
            nexusClient = new NexusClient(this);
        }

        // Update is called once per frame
        void Update()
        {
            // Sync with Nexus
            nexusClient.OnUpdate();

            // Update the graph
            codeGraph.OnUpdate();
        }

        //#endregion
    }
}
