using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using PUL;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PUL
{
    public class ControllerManager : MonoBehaviour, IMixedRealitySourceStateHandler, IMixedRealityInputHandler, IMixedRealityInputHandler<float>, IMixedRealityInputHandler<Vector2>
    {
        // ----- Variables ------

        // ====================================
        // NOTE: These values are wired up in the Unity Editor

        public GameObject RadialMenu = null;

        public GameObject MagnifyingGlass = null;

        // Values used for quick flag checks
        [Header("Quick Flags")]
        public bool runningSimulated = false;


        // END: These values are wired up in the Unity Editor
        // ====================================
        
        // Instance holder
        private static ControllerManager _instance; // this manager is a singleton

        public static ControllerManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("ControllerManager is NULL");
                return _instance;
            }
        }

        // Bind check
        private bool inputsBound = false;

        // Hold controller objects
        private Dictionary<uint, IMixedRealityController> connectedControllers; 


        // ----- Unity Events -----
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

            // Creates the controller dictionary
            connectedControllers = new Dictionary<uint, IMixedRealityController>();

            // Run on enable if enabled
            if (gameObject.activeSelf)
                OnEnable();
        }

        // Start is called before the first frame update
        void Start()
        {
            // Radial Menu enable/disable on startup
            bool radialMenuEnabledOnStartup = true;
            string value = ConfigManager.Instance.GetFeatureSetProperty("radial_menu_enabled_on_startup");
            if (value != null) radialMenuEnabledOnStartup = bool.Parse(value);
            RadialMenu.SetActive(radialMenuEnabledOnStartup);

            // Magnifying glass enable/disable on startup
            bool magnifyingGlassEnabledOnStartup = true;
            string value2 = ConfigManager.Instance.GetFeatureSetProperty("magnifying_glass_enabled_on_startup");
            if (value2 != null) magnifyingGlassEnabledOnStartup = bool.Parse(value2);
            MagnifyingGlass.SetActive(magnifyingGlassEnabledOnStartup);
        }

        // Update is called once per frame
        void Update()
        {
        }

        // Register on enable
        private void OnEnable()
        {
            if (inputsBound) return;

            Debug.Log("ControllerManager -> Binding inputs");
            CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler>(this);
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler<float>>(this);
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler<Vector2>>(this);
            inputsBound = true;
        }

        // Deregister on disable
        private void OnDisable()
        {
            if(!inputsBound) return;

            CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler>(this);
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler<float>>(this);
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler<Vector2>>(this);
            inputsBound = false;
        }


        // ----- Functions ------

        // Position tracking
        private void LogPosition() { }
        // -> Quick Raycasting
        // --> Some possible variables for this method
        // ---> Distance: Ray Distance
        // ---> LayerMask: Ray Mask
        private RaycastHit ShootRay(float distance) { return new RaycastHit(); }
        // Gets a controller based on source id
        public IMixedRealityController GetController (uint sourceID)
        {
            // Return early if the key is equal to 0
            if(sourceID == 0) return null;

            // Check if id exists (given an id is entered)
            if(!connectedControllers.ContainsKey(sourceID))
            {
                Debug.LogError($"Controller Manager (GetController) -> No controller with id [{sourceID}] found");
                return null;
            }
            return connectedControllers[sourceID];
        }


        // Keyboard tracking
        // -> Unsure how im going to approach this one right now


        // Radial Menu?
        // -> Probably should be stored somewhere else (esp with the possible delegates), gonna leave this here for now -L

        // Maybe have delegates for different inputs so events can be easily and quickly assigned?
        // -> Checks when the trigger was...
        // --> Pressed
        public delegate void OnTriggerPressed(uint sourceID);
        public OnTriggerPressed onTriggerPressed;
        // --> Changed
        public delegate void OnTriggerChanged(uint sourceID, float value);
        public OnTriggerChanged onTriggerChanged;
        // --> Released (Not yet implemented)
        public delegate void OnTriggerReleased(uint sourceID);
        public OnTriggerReleased onTriggerReleased;

        // -> Checks when the grip was...
        // --> Pressed
        public delegate void OnGripPressed(uint sourceID);
        public OnGripPressed onGripPressed;
        // --> Released (Not yet implemented)
        public delegate void OnGripReleased(uint sourceID);
        public OnGripReleased onGripReleased;

        // -> Checks when the touchpad changes
        public delegate void OnTouchpadChanged(uint sourceID, Vector2 value);
        public OnTouchpadChanged onTouchpadChanged;
        public delegate void OnTouchpadPressed(uint sourceID);
        public OnTouchpadPressed onTouchpadPressed;


        // ----- Delegate Controllers -----
        // Source detected and source lost methods pulled from https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input/input-events?view=mrtkunity-2022-05
        int sourceCount = 0;
        public void OnSourceDetected(SourceStateEventData eventData)
        {
            var hand = eventData.Controller;
            
            // Only react to Controller input sources
            if (hand != null)
            {
                // Debug.Log("Source detected: " + hand.ControllerHandedness + $"  ({hand}) [{hand.InputSource.SourceName}] || ID: {eventData.InputSource.SourceId}");
                connectedControllers.Add(eventData.InputSource.SourceId, hand);
            }
        }
        public void OnSourceLost(SourceStateEventData eventData)
        {
            var hand = eventData.Controller;

            // Only react to Controller input sources
            if (hand != null)
            {
                // Debug.Log("Source lost: " + hand.ControllerHandedness + $"  ({hand})");
                connectedControllers.Remove(eventData.InputSource.SourceId);
            }
        }



        // Controls inputs from binary sources, like buttons.
        public void OnInputUp(InputEventData eventData)
        {
            // Debug.Log($"Source Input (UP): " + eventData.MixedRealityInputAction.Description);
            
            // Bind delegates
            // --> Needs to be assigned in MRTK
            // -> Trigger Released

            // -> Grip Released

        }
        public void OnInputDown(InputEventData eventData)
        {
            // Debug.Log($"Source Input (DOWN): {eventData.MixedRealityInputAction.Description} || ID: {eventData.InputSource.SourceId}");

            // Bind delegates
            // -> Trigger Pressed 
            // --> Description value mapped and named through MRTK Toolkit
            if(eventData.MixedRealityInputAction.Description == "Select")
            {
                onTriggerPressed?.Invoke(eventData.InputSource.SourceId);
            }
            // -> Grip Pressed
            else if (eventData.MixedRealityInputAction.Description == "Grip Press")
            {
                onGripPressed?.Invoke(eventData.InputSource.SourceId);
            }
            else if (eventData.MixedRealityInputAction.Description == "Touchpad Action")
            {
                onTouchpadPressed?.Invoke(eventData.InputSource.SourceId);
            }
        }

        // Pulls input data from...
        // -> Trigger
        public void OnInputChanged(InputEventData<float> eventData)
        {
            // Debug.Log("Source Input (CHANGED): " + eventData.MixedRealityInputAction.Description + $" ({eventData.InputData})");

            // Bind delegates
            // -> Trigger Changed
            if (eventData.MixedRealityInputAction.Description == "Trigger")
            {
                onTriggerChanged?.Invoke(eventData.InputSource.SourceId, eventData.InputData);
            }
        }

        // Pulls input data from...
        // -> Touchpad
        public void OnInputChanged(InputEventData<Vector2> eventData)
        {
            // Debug.Log("Source Input (CHANGED): " + eventData.MixedRealityInputAction.Description + $" ({eventData.InputData})");

            // Bind delegates
            // -> Touchpad Changed
            if(eventData.MixedRealityInputAction.Description == "Teleport Direction")
            {
                onTouchpadChanged?.Invoke(eventData.InputSource.SourceId, eventData.InputData);
            }
        }
    }
}
