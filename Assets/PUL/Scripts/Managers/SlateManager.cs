using PUL;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;
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

        [Header("Slate Logging")]
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

            // Slate enable/disable movement at startup based on config
            bool slatesMoveable = true;
            string value = ConfigManager.Instance.GetFeatureSetProperty("slates_moveable");
            if (value != null) slatesMoveable = bool.Parse(value);
            ObjectManipulator slateOM = slate.GetComponent<ObjectManipulator>();
            slateOM.enabled = slatesMoveable;
            ObjectManipulator titleBarOM = slate.transform.Find("TitleBar").gameObject.GetComponent<ObjectManipulator>();
            titleBarOM.enabled = slatesMoveable;

            // Log slate  
            // TODO: Streamline MakeASlate -- AddSlate -- how and where data is held about a slate -- etc.
            SlateData slateData = new SlateData(slate);
            AddSlate(slateData);

            return slate;
        }

        // Add a slate to the log
        public void AddSlate(SlateData slateData)
        {
            // Create a new slate
            activeSlates.Add(slateData);

            if (slateDeconflictionEnabled)
            {
                // Flag slates that need to be moved for spawning
                Vector3 center = slateData.GetSphereCenter();
                slateData.simulateMovement = true;

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
        }

        // Close (destroy) a slate 
        public void CloseSlateCallback(GameObject obj)
        {
            string slateName = obj.transform.Find("TitleBar/TitleBarTMP").gameObject.GetComponent<TextMeshPro>().text;
            Debug.Log($"Closing slate {slateName}");

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
        }

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

        private bool CheckSimulationState()
        {
            foreach (SlateData slate in activeSlates)
            {
                if (slate.simulateMovement)
                    return true;
            }
            return false;
        }

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