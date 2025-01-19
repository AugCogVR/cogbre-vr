using PUL;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections.Generic;

namespace PUL
{
    [System.Serializable]
    public class SlateManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Graph Manager object

        // The Slate prefeb we instantiate for function disassembly
        public GameObject slatePrefab;

        [Header("Slates in Fixed Position")]
        public bool slatesMoveable = true; // can users move slates?
        public float layoutCircleCenterX = 0.0f;
        public float layoutCircleCenterZ = -1.0f;
        public float layoutCircleRadius = 2.0f;
        public float layoutYOffset = 0.0f;
        public float slateLayoutAngleScale = 1.0f;

        [Header("Slate Automatic Deconfliction")]
        public bool slateDeconflictionEnabled = true; // automatic physical separation of overlapping slates
        public float slatePadding = 0.6f;
        public float slateSpawnZone = 1; // Marks the region in which physic simulation is allowed for slates. 
        public bool simulatingMovement = false;

        // END: These values are wired up in the Unity Editor -> Menu Manager object
        // ====================================

        public List<SlateData> activeSlates = new List<SlateData>(); // LW: I later want this to be a list of a unique class structure, with gameobject as an element.

        // Instance holder
        private static SlateManager _instance; // this manager is a singleton

        public static SlateManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("SlateManager is NULL");
                return _instance;
            }
        }

        void Awake()
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
            // Read values fron config data
            string value = ConfigManager.Instance.GetFeatureSetProperty("slate_deconfliction_enabled");
            if (value != null) slateDeconflictionEnabled = bool.Parse(value);
            string value2 = ConfigManager.Instance.GetFeatureSetProperty("slates_moveable");
            if (value2 != null) slatesMoveable = bool.Parse(value2);
        }

        // Update is called once per frame
        void Update()
        {
            if (slateDeconflictionEnabled && simulatingMovement)
                SimulateSlateMovement();
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

        // Make a slate with common characteristics. 
        public GameObject MakeASlate(string title, string contents)
        {
            Debug.Log($"Make a slate: title:\n{title}\ncontents:\n{contents}");
            Debug.Log($"Slate at {GameManager.Instance.getSpawnPosition()}");

            // Make a new slate
            GameObject slate = Instantiate(slatePrefab, GameManager.Instance.getSpawnPosition(), GameManager.Instance.getSpawnRotation());
            // slateList.Add(slate);
            TextMeshPro titleBarTMP = slate.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>();
            titleBarTMP.text = title;

            // Grab the content TMP
            TextMeshProUGUI contentTMP = slate.GetComponentInChildren<TextMeshProUGUI>();
            contentTMP.text = "";

            // -> Pulls and Sets information regarding the input field
            // Used for highlighting
            TMP_InputField inField = contentTMP.GetComponent<TMP_InputField>();
            inField.text = contentTMP.text;
            int numLines = contents.Split('\n').Length - 1;
            inField.text = contents;
            contentTMP.rectTransform.sizeDelta = new Vector2(contentTMP.rectTransform.sizeDelta.x, numLines * (contentTMP.fontSize + 1.5f));

            // Wire up copy button
            DynamicScrollbarHandler dynamicScrollbarHandler = slate.GetComponentInChildren<DynamicScrollbarHandler>();
            GameObject copyButton = slate.transform.Find("TitleBar/Buttons/CopyButton").gameObject;
            PressableButtonHoloLens2 buttonFunction = copyButton.GetComponent<PressableButtonHoloLens2>();
            buttonFunction.TouchBegin.AddListener(() => TextManager.Instance.TextCopyCallback(dynamicScrollbarHandler));
            Interactable distanceInteract = copyButton.GetComponent<Interactable>();
            distanceInteract.OnClick.AddListener(() => TextManager.Instance.TextCopyCallback(dynamicScrollbarHandler));

            // Wire up close button
            GameObject closeButton = slate.transform.Find("TitleBar/Buttons/CloseButton").gameObject;
            PressableButtonHoloLens2 buttonFunction2 = closeButton.GetComponent<PressableButtonHoloLens2>();
            buttonFunction2.TouchBegin.AddListener(() => CloseSlateCallback(slate));
            Interactable distanceInteract2 = closeButton.GetComponent<Interactable>();
            distanceInteract2.OnClick.AddListener(() => CloseSlateCallback(slate));

            // Slate enable/disable movement based on config
            ObjectManipulator slateOM = slate.GetComponent<ObjectManipulator>();
            slateOM.enabled = slatesMoveable;
            ObjectManipulator titleBarOM = slate.transform.Find("TitleBar").gameObject.GetComponent<ObjectManipulator>();
            titleBarOM.enabled = slatesMoveable;

            // Track slate in ActiveSlates
            SlateData slateData = new SlateData(slate);
            activeSlates.Add(slateData);

            // If slates are not moveable, lay them out in a fixed pattern.
            if (!slatesMoveable)
            {
                positionUnmoveableSlates();
            }

            // Set up slate for automatic deconfliction-upon-spawn if enabled.
            // Recommend that this mode is disabled when slates are not moveable. 
            if (slateDeconflictionEnabled)
            {
                // Flag slates that need to be moved for spawning
                Vector3 center = slateData.GetSphereCenter();
                slateData.simulateMovement = true;

                foreach(SlateData otherSlate in activeSlates)
                {
                    // Check distance from center, if close enough flag for movement
                    if ((otherSlate.obj != slate) && 
                        (Vector3.Distance(center, otherSlate.GetSphereCenter()) < slateSpawnZone))
                    {
                        otherSlate.simulateMovement = true;
                    }
                }

                // Simulate movement
                simulatingMovement = true;
            }

            return slate;
        }

        // If slates are not moveable, lay them out in a fixed pattern.
        private void positionUnmoveableSlates()
        {
            // Position the slates in a circle around the user starting at the 
            // spawn point probided by GameManager (usually next to the main menu).

            // Do all of our calculations in 2D using x and z axes. Slates' y position
            // will remain unchanged. 

            // Find starting spawn position in 3D and 2D.
            Vector3 startingSpawnPosition = GameManager.Instance.getSpawnPosition();
            Vector2 start2D = new Vector2(startingSpawnPosition.x, startingSpawnPosition.z);
            float slateY = startingSpawnPosition.y + layoutYOffset;
            // Debug.Log($"SPAWN: {startingSpawnPosition} 2D {start2D}");

            // Find user position (center of circle) in 3D and 2D.
            // Vector3 userPosition = InputRayUtils.GetHeadGazeRay().origin;
            // Vector2 center = new Vector2(userPosition.x, userPosition.z);
            Vector2 center = new Vector2(layoutCircleCenterX, layoutCircleCenterZ);
            // Debug.Log($"USER: Head {userPosition} Cam {Camera.main.transform.position} 2D {center}");

            // Find radius of circle.
            // Vector2 menu2D = new Vector2(MenuManager.Instance.UIPanel.transform.position.x, MenuManager.Instance.UIPanel.transform.position.z);
            // float radius = Vector2.Distance(center, menu2D);
            // if (radius < 2.0f) radius = 2.0f;
            // Debug.Log($"Radius: {radius}");

            // Find angle to the spawn point (where the first slate will be placed).
            float startingAngle = Mathf.Atan2(start2D.y - center.y, start2D.x - center.x);
            // Debug.Log($"Starting angle: {startingAngle * Mathf.Rad2Deg}");

            // Find the angle between slates based on width of the slate and circle radius.
            float slateLayoutAngle = 15.0f * Mathf.Deg2Rad; // default 15 degrees between slates
            if (activeSlates.Count > 0) // get the width of the first slate 
            {
                slateLayoutAngle = activeSlates[0].obj.transform.localScale.x / layoutCircleRadius;
                slateLayoutAngle *= slateLayoutAngleScale;
            }
            // Debug.Log($"Layout angle {slateLayoutAngle * Mathf.Rad2Deg}");

            // Walk through slate list, positioning each one progressively in a circular pattern.
            for (int i = 0; i < activeSlates.Count; i++)
            {
                float angleInRadians = startingAngle + (i * -slateLayoutAngle);
                float newX = center.x + Mathf.Cos(angleInRadians) * layoutCircleRadius;
                float newZ = center.y + Mathf.Sin(angleInRadians) * layoutCircleRadius;
                activeSlates[i].obj.transform.position = new Vector3(newX, slateY, newZ);
                activeSlates[i].obj.transform.rotation = Quaternion.LookRotation(activeSlates[i].obj.transform.position - Camera.main.transform.position);
                // Debug.Log($"Slate {activeSlates[i].name} placed at {activeSlates[i].obj.transform.position} {angleInRadians * Mathf.Rad2Deg}");
            }
        }

        // Close (destroy) a slate 
        public void CloseSlateCallback(GameObject obj)
        {
            string slateName = obj.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>().text;
            Debug.Log($"Closing slate {slateName}");

            // Remove slate data from list of active slates.
            bool found = false;
            for (int i = 0; i < activeSlates.Count; i++)
            {
                if (activeSlates[i].obj.Equals(obj))
                {
                    activeSlates.RemoveAt(i);
                    found = true;
                    break;
                }
            }
            if (!found) Debug.LogError($"SlateManager - RemoveSlate(obj) -> No object found matching {obj.name}");

            Destroy(obj);

            // If slates are not moveable, reposition the remaining slates.
            if (!slatesMoveable) positionUnmoveableSlates();
        }

        // Return the position and orientation of every active slate as a JSON string.
        public string GetSlateTelemetryJSON()
        {
            string returnMe = "";

            if (activeSlates.Count > 0)
            {
                returnMe += $"[\"session_update\", \"objectTelemetry\"";

                foreach (SlateData slate in activeSlates)
                {            
                    string slateName = "slate:";
                    slateName += slate.obj.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>().text;
                    slateName = slateName.Replace('\n', ':');
                    returnMe += $", \"{slateName}\", ";
                    Vector3 pos = slate.obj.transform.position;
                    returnMe += $"\"{pos.x}\", \"{pos.y}\", \"{pos.z}\", ";
                    Vector3 ori = slate.obj.transform.eulerAngles;
                    returnMe += $"\"{ori.x}\", \"{ori.y}\", \"{ori.z}\"";
                }

                returnMe += "]";
                // Debug.Log("SLATE TELEMETRY: " + returnMe);
            }

            return returnMe;
        }

        // Return true if any of the slates are currently being automatically deconflicted.
        private bool CheckSimulationState()
        {
            foreach (SlateData slate in activeSlates)
            {
                if (slate.simulateMovement)
                    return true;
            }
            return false;
        }

        // Check each slate against every other slate during automatic deconfliction.
        // Called from Update
        public void SimulateSlateMovement()
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
    }


    // This class holds data for a slate, such as the name and GameObject reference, but is
    // mainly used to track and update the slate during automatic deconfliction.
    [System.Serializable]
    public class SlateData
    {
        public string name;
        public GameObject obj = null;
        public float radius = 1.0f;
        public bool simulateMovement = false; // Used to position slates around the spawn area
        int movementStallCheck = 0; // Checks how many frames the slate has been idle for
        int movementStallThreshold = 50; // Limit for the amount of frames the slate can stall before stopping movement simulation

        public SlateData(GameObject obj)
        {
            name = obj.name;
            this.obj = obj;
            SetSphereRadius();
        }

        public SlateData(string name, GameObject obj)
        {
            this.name = name;
            this.obj = obj;
            SetSphereRadius();
        }

        public Vector3 GetSphereCenter()
        {
            if (obj == null) return Vector3.zero;
            return obj.transform.position;
        }

        private void SetSphereRadius()
        {
            if(obj == null) return;
            radius = SlateManager.Instance.slatePadding * obj.transform.localScale.x;
        }

        public void SimulateCollision(SlateData other)
        {
            if (other == null) return;
            // Get initial position
            Vector3 initPos = GetSphereCenter();

            if (CheckOverlap(other))
            {
                Vector3 force = GetPushDirection(other);
                PushSlate(force / 2f);
                other.PushSlate(-force / 2f);
            }

            // Compare new position with inital and update movement state
            if (initPos == GetSphereCenter())
                movementStallCheck++;
            else
                movementStallCheck = 0;

            // Check if movement is done
            if (movementStallCheck > movementStallThreshold)
                simulateMovement = false;
        }

        public bool CheckOverlap(SlateData other)
        {
            // Check if the distance between the two spheres is less than the main radius + other radius.
            float totalRadius = radius + other.radius;
            float distance = Vector3.Distance(GetSphereCenter(), other.GetSphereCenter());

            // Debug.Log("Checking Overlap: " + totalRadius + " | " + distance);

            return distance < totalRadius;
        }

        public Vector3 GetPushDirection(SlateData other)
        {
            return GetSphereCenter() - other.GetSphereCenter();
        }

        public void PushSlate(Vector3 force)
        {
            if (obj == null) {
                Debug.LogError("SlateData - PushSlate -> Object is set to null");
                return;
            }

            obj.transform.position = obj.transform.position + (force * Time.deltaTime);
        }
    }
}