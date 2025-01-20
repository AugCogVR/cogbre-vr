using UnityEngine;
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
        // NOTE: These values are wired up in the Unity Editor

        public GameObject GenericSpawnPoint;

        public GameObject CameraStartPoint;

        public GameObject FixedSlateStartPoint;

        public GameObject FixedGraphStartPoint;

        public GameObject FixedLayoutCircleCenter;

        // END: These values are wired up in the Unity Editor
        // ====================================
        
        // Spawn point for new slates, graphs, etc. 
        private GameObject spawnPoint;

        private static GameManager _instance; // this manager is a singleton

        public static GameManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("GameManager is NULL");
                return _instance;
            }
        }

        private void Awake()
        {
            // If another instance exists, destroy that game object. If no other game manager exists, 
            // initialize the instance to itself. As this manager needs to exist throughout all scenes, 
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
            // // Create a spawn point relative to the UI Panel where new objects will appear
            // spawnPoint = new GameObject();
            // spawnPoint.transform.parent = MenuManager.Instance.UIPanel.transform;
            // spawnPoint.transform.localPosition = new Vector3(1.0f, 0.25f, 0);

            Camera.main.transform.position = CameraStartPoint.transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            // // Not the most elegant solution but it gets the job done... -L
            // GenericSpawnPoint.transform.LookAt(Camera.main.transform.position);
            // GenericSpawnPoint.transform.Rotate(Vector3.up * 180);
        }

        // Return the position of the spawn point.
        public Vector3 getSpawnPosition()
        {
            return GenericSpawnPoint.transform.position;
        }

        // Return the rotation of the spawn point.
        public Quaternion getSpawnRotation()
        {
            return GenericSpawnPoint.transform.rotation;
        }

        // Starts a coroutine on the gameManager
        // - This ensures that any routine can't be cut off by objects being set to inactive.
        public void StartPersistentCoroutine(IEnumerator routine)
        {
            StartCoroutine(routine);
        }

        private string getJSONFragmentForIdPosAndDir(string id, Vector3 pos, Vector3 dir)
        {
            string returnMe = "";
            returnMe += $"\"{id}\",";
            returnMe += $"\"{pos.x}\", \"{pos.y}\", \"{pos.z}\", ";
            returnMe += $"\"{dir.x}\", \"{dir.y}\", \"{dir.z}\"";
            return returnMe;
        }

        public string GetUserTelemetryJSON()
        {
            string returnMe = $"[\"session_update\", \"objectTelemetry\", ";

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
            // Debug.Log("USER TELEMETRY: " + returnMe);

            return returnMe;
        }
    }
}
