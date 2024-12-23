using UnityEngine;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using System.Collections.Generic;
using System.Collections;

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

        public ConfigManager configManager;

        public ControllerManager controllerManager;

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
        public float slateSpawnZone = 1; // Marks the region in which physic simulation is allowed for slates. 
        public bool simulatingMovement = false;

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

            if(simulatingMovement)
                SimulateSlateMovement();
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
            AddSlate(sd);
        }
        // Add a slate to the log
        public void AddSlate(SlateData sd)
        {
            // Create a new slate
            activeSlates.Add(sd);

            // Flag slates that need to be moved for spawning
            Vector3 center = sd.GetSphereCenter();
            sd.simulateMovement = true;

            foreach(SlateData slate in activeSlates)
            {
                // Check distance from center, if close enough flag for movement
                if(Vector3.Distance(center, slate.GetSphereCenter()) < slateSpawnZone)
                {
                    slate.simulateMovement = true;
                }
            }

            // Simulate movement
            simulatingMovement = true;
        }

        private bool CheckSimulationState()
        {
            foreach (SlateData slate in activeSlates)
            {
                if (slate.simulateMovement)
                    return true;
            }
            return false;
        }
        private void SimulateSlateMovement()
        {
            // Simulate movement

            foreach (SlateData slate in activeSlates)
            {
                if(slate.simulateMovement)
                    foreach (SlateData otherSlate in activeSlates)
                    {
                        slate.SimulateCollision(otherSlate);
                    }
            }

            // Check if simulation is done
            simulatingMovement = CheckSimulationState();
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

        // Starts a coroutine on the gameManager
        // - This ensures that any routine can't be cut off by objects being set to inactive.
        public void StartPersistentCoroutine(IEnumerator routine)
        {
            StartCoroutine(routine);
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



        private string getJSONFragmentForIdPosAndDir(string id, Vector3 pos, Vector3 dir)
        {
            string returnMe = "";
            returnMe += $"\"objectTelemmetry\", \"{id}\",";
            returnMe += $"\"{pos.x}\", \"{pos.y}\", \"{pos.z}\", ";
            returnMe += $"\"{dir.x}\", \"{dir.y}\", \"{dir.z}\"";
            return returnMe;
        }

        public string GetAllTelemetryJSON()
        {
            string returnMe = $"[\"session_update\", ";

            // ref: https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input/input-state?view=mrtkunity-2022-05

            UnityEngine.Ray headRay = InputRayUtils.GetHeadGazeRay();
            returnMe += getJSONFragmentForIdPosAndDir("head", headRay.origin, headRay.direction);

            // Get the right hand ray
            if (InputRayUtils.TryGetHandRay(Handedness.Right, out UnityEngine.Ray rightHandRay))
            {
                returnMe += ", " + getJSONFragmentForIdPosAndDir("rhand", rightHandRay.origin, rightHandRay.direction);
            }

            // Get the left hand ray
            if (InputRayUtils.TryGetHandRay(Handedness.Left, out UnityEngine.Ray leftHandRay))
            {
                returnMe += ", " + getJSONFragmentForIdPosAndDir("lhand", leftHandRay.origin, leftHandRay.direction);
            }

            returnMe += "]";
            // Debug.Log("TELEMMETRY: " + returnMe);

            return returnMe;
        }
    }
}
