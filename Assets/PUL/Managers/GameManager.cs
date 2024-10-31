using UnityEngine;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using System.Collections.Generic;

namespace PUL
{
    // GameManager keeps track of relevant objects.
    // It is intended to be a singleton per the pattern described at:
    // https://bergstrand-niklas.medium.com/setting-up-a-simple-game-manager-in-unity-24b080e9516c
    public class GameManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Game Manager object

        public NexusClient nexusClient;

        public MenuManager menuManager;

        public GraphManager graphManager;

        public TextManager textManager;

        // END: These values are wired up in the Unity Editor -> Game Manager object
        // ====================================
        
        // Spawn point for new slates, graphs, etc. 
        private GameObject spawnPoint;

        private static GameManager _instance; // GameManager is a singleton

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

        // Values used for quick flag checks
        [Header("Quick Flags")]
        public bool runningSimulated = false;

        // Values for handling the keyboard
        [Header("Keyboard")]
        public float keyboardDist = 1;
        public float keyboardScale = 0.2f;
        public float keyboardVertOffset = -1;
        TMP_InputField kbInputField = null;

        [Header("Slate Logging")]
        public List<SlateData> activeSlates = new List<SlateData>(); // I later want this to be a list of a unique class structure, with gameobject as an element.
        public float slatePadding = 0.6f;

        private void Awake()
        {
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
            // Create a spawn point relative to the UI Panel where new objects will appear
            spawnPoint = new GameObject();
            spawnPoint.transform.parent = menuManager.UIPanel.transform;
            spawnPoint.transform.localPosition = new Vector3(0.9f, 0.25f, 0);

            // Start out looking at the menu -- very convenient for testing -- yell at me if it breaks things -- DGB
            Camera.main.transform.position = new Vector3(-7f, 2f, -1.5f);
        }

        // Update is called once per frame
        void Update()
        {
            // Not the most elegant solution but it gets the job done... -L
            spawnPoint.transform.LookAt(Camera.main.transform.position);
            spawnPoint.transform.Rotate(Vector3.up * 180);
        }

        // Return the position of the spawn point.
        public Vector3 getSpawnPosition()
        {
            return spawnPoint.transform.position;
        }

        // Return the rotation of the spawn point.
        public Quaternion getSpawnRotation()
        {
            return spawnPoint.transform.rotation;
        }

        // Opens up keyboard in view
        public void ShowKeyboard()
        {
            NonNativeKeyboard keyboard = NonNativeKeyboard.Instance;
            keyboard.PresentKeyboard();
            keyboard.RepositionKeyboard(Camera.main.transform.position + (Camera.main.transform.forward * keyboardDist) + (Vector3.down * keyboardVertOffset));
            keyboard.transform.localScale = Vector3.one * keyboardScale;
        }

        // Add a default slate to the log
        public void AddSlate(GameObject obj)
        {
            // Create a new slate
            SlateData sd = new SlateData(obj);
            activeSlates.Add(sd);
        }
        // Add a slate to the log
        public void AddSlate(SlateData sd)
        {
            // Create a new slate
            activeSlates.Add(sd);
        }



        // Removes a slate from the log using the object
        public void RemoveSlate(GameObject obj)
        {
            for (int i = 0; i < activeSlates.Count; i++)
            {
                if (activeSlates[i].obj.Equals(obj))
                {
                    activeSlates.RemoveAt(i);
                    return;
                }
            }
            Debug.LogError($"GameManager - RemoveSlate(obj) -> No object found matching {obj.name}");
        }
        // Removes a slate from the log using the Name
        public void RemoveSlate(string name)
        {
            for (int i = 0; i < activeSlates.Count; i++)
            {
                if (activeSlates[i].name.ToLower().Equals(name.ToLower()))
                {
                    activeSlates.RemoveAt(i);
                    return;
                }
            }
            Debug.LogError($"GameManager - RemoveSlate(obj) -> No name found matching {name}");
        }



        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            // Draw a circle around each slate, shows padding
            foreach (SlateData slate in activeSlates)
            {
                Gizmos.DrawWireSphere(slate.GetSphereCenter(), slate.radius);
            }
        }
    }
}
