using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public CubeManager cubeManager;

    //public CatInfoClient catInfoClient;

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
        Debug.Log("GameManager START");

        // Initialize scene objects
        cubeManager = GameObject.Find("Cube").GetComponent<CubeManager>();
        cubeManager.OnStart(this);

        // Initialize data client
        nexusClient = new NexusClient(this);

        //catInfoClient = new CatInfoClient(this);
    }

    // Update is called once per frame
    void Update()
    {
        //catInfoClient.OnUpdate();

        // Sync with Nexus
        nexusClient.OnUpdate();

        // Update scene
        cubeManager.OnUpdate();
    }

    //#endregion
}
