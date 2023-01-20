using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.UI;
using TMPro;

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


    List<GameObject> cubeList;

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
        //Debug.Log("GameManager START");

        // Initialize scene objects
        
        cubeList = new List<GameObject>();
        for (int i = 0; i < 5; i++)
        {
            //Debug.Log("MAKE A CUBE");

            // Create a new cube
            GameObject prefab = Resources.Load("Prefabs/Cube") as GameObject;
            var position = new Vector3(Random.Range(-1.0f, 1.0f), 0.01f, Random.Range(-1.0f, 1.0f));
            GameObject newCube = Instantiate(prefab, position, Quaternion.identity);
            cubeList.Add(newCube);
            newCube.GetComponent<CubeManager>().OnStart(this);

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
            textObject.text = "CUBE#" + i;
            textObject.fontSize = 1;  
            //textObject.autoSizeTextContainer = true;
            textObject.alignment = TextAlignmentOptions.Center;
        }

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
        foreach(GameObject currCube in cubeList)
        {
            currCube.GetComponent<CubeManager>().OnUpdate();
        }

    }

    //#endregion
}
