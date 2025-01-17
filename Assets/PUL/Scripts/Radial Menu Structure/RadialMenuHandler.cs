using Microsoft.MixedReality.Toolkit.Input;
using PUL;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PUL
{
    public class RadialMenuHandler : MonoBehaviour
    {
        // Ideally I want this script to be able to handle 2 different input devices
        // -> VR Controller (For obvious reasons)
        // -> Mouse & Keyboard (For debugging purposes)
        // I plan on adding a function that checks for "play mode" in the game manager, this mode will swap between a simulated space (debug using inputs from k/m) and a virtual reality space (vr headset connected) - This corresponds with a note in the Game Manager [~ln66]
        // Through simple checking of this state, the setup and function of this script will change
        // - L


        // The radial menu will store a list of (at most) 8 objects
        // -> These objects are called Radial Menu Options. They act as tabs that appear in the radial menu that can be selected.
        // A few objects will extend off of these options.
        // -> The first of these objects is the Radial Menu SubMenu (Extends Radial Menu Option). When selected it will load up a sub menu, with new options. It will also allow the user to navigate backwards through the menu hierarchy.
        // - L

        // Defines the options contained in the menu
        public RadialMenuOption[] menuOptions = new RadialMenuOption[8];
        public bool allowDebugInputs = false;
        public KeyCode debugOpen = KeyCode.Z;
        public KeyCode debugSelect = KeyCode.X;
        [Space]
        // Stores the users input point
        public Vector2 inputPosition = Vector2.zero; 
        // Defines the area in the radial menu where inputs are ignored. Used for the back button in sub menus
        public float deadRadius = 0.2f;
        // Stores the hovered option index
        int hoveredOption = 0;
        // Stores the cursor rotation
        float cursorRotation = 0f;
        // Flags if the cursor is in the dead zone
        bool cursorDead = false;
        // Checks if the player opens the radial menu
        bool menuOpened = false;

        // Holds information about the input source
        private uint controllerSourceId = 0;

        // Models used in the menu
        // Holds the graphic parent
        public GameObject radialMenuModelParent = null;
        // Holds refrence for the cursor in menu
        public GameObject cursorObject = null;
        // Holds refrence to the placement distance of pie infographics
        public float pGraphicDistance = 0.6f;


        // Start is called before the first frame update
        void Start()
        {
            // Bind debug to controller manager delegate
            ControllerManager.Instance.onTouchpadChanged += TouchpadEvent;
            ControllerManager.Instance.onTouchpadPressed += SelectEvent;

            // Build the menu
            BuildMenu();
        }
        // Sets the properties in Radial Menu Options used for the menu
        void BuildMenu()
        {

            // Get options
            int oCount = OptionCount();
            if (oCount <= 0)
            {
                Debug.LogError("Radial Menu Handler (Build Menu) -> No options found...");
                return;
            }
            // Get the size of each pie
            float pieSize = (float)360 / oCount;
            // Get the origin and working rotation
            float cRot = 180;
            // Set the rotation of pies
            for (int i = 0; i < menuOptions.Length; i++)
                if (menuOptions[i] != null)
                {
                    // Get a normalized rotation
                    float rad = Mathf.Deg2Rad * cRot;
                    menuOptions[i].rotation = Mathf.Atan2(Mathf.Sin(rad), Mathf.Cos(rad));
                    menuOptions[i].size = pieSize * Mathf.Deg2Rad;
                    menuOptions[i].graphicDist = pGraphicDistance;
                    cRot -= pieSize;

                    menuOptions[i].OnBuild();
                }

            // Set model to inactive
            radialMenuModelParent.SetActive(false);
        }

        // Makes sure values are set properly based on scale
        float storedScale = 1;

        private void OnDisable()
        {
            // Make sure to unbind all delegates if ControllerManager isn't null.
            // When shutting down VR session, ControllerManager may already be null
            // when this method is called. If so, just do nothing, since it doesn't matter at that point.
            if (ControllerManager.Instance != null)
            {
                ControllerManager.Instance.onTouchpadChanged -= TouchpadEvent;
                ControllerManager.Instance.onTouchpadPressed -= SelectEvent;
            }
        }

        private void TouchpadEvent(uint sourceID, Vector2 value)
        {
            // If a source id is set then only allow inputs from said source
            if (CheckInputSource(sourceID))
                return;

            Debug.Log("Radial Menu Handler -> Touchpad Event run");

            // Update input and log source
            inputPosition = value * storedScale;
            controllerSourceId = sourceID;

            UpdateHover();
        }
        private void SelectEvent(uint sourceID)
        {
            // Check if the menu should be opened
            if (!menuOpened)
            {
                menuOpened = true;
                return;
            }

            // If a source id is set then only allow inputs from said source
            if (CheckInputSource(sourceID))
                return;
            // Do nothing if the cursor is dead
            if (cursorDead) return;

            // Get the hovered option and run the associated select method
            if (hoveredOption < menuOptions.Length)
            {
                menuOptions[hoveredOption].OnSelect();
            }
            else
            {
                Debug.LogError($"Radial Menu Handler (Select Event) -> Hovered option {hoveredOption} is out of range. Menu option length is {menuOptions.Length}");
            }
        }

        // Checks if the input source is 0 or if the source id is our current source
        private bool CheckInputSource(uint sourceID)
        {
            return controllerSourceId != 0 && sourceID != controllerSourceId;
        }


        Vector2 mousePosition = Vector2.zero; // Current mouse position
        Vector2 lMousePosition = Vector2.zero; // Mouse position last frame
        Vector2 mouseTimeout = Vector2.one * 3f;
        float mouseSpeed = 0.005f;
        private void KB_TrackMouse()
        {
            // Fall out early if mouse isn't allowed
            // -> This will later be controlled by tracking the bootup state (simulation vs headset)
            if (!allowDebugInputs)
                return;

            // Check for debug key press
            if (!Input.GetKey(debugOpen))
            {
                // Check for timeout
                if(mouseTimeout.x > 0)
                    mouseTimeout.x -= Time.deltaTime;
                else
                    ResetInput();

                // Disable menu
                radialMenuModelParent.SetActive(false);

                return;
            }
            // Enable menu
            radialMenuModelParent.SetActive(true);
            // Position in front of the main camera
            Transform camTransform = Camera.main.transform;
            radialMenuModelParent.transform.position = camTransform.position + (camTransform.forward * 4);
            radialMenuModelParent.transform.LookAt(camTransform.position);
            radialMenuModelParent.transform.localEulerAngles = radialMenuModelParent.transform.localEulerAngles + new Vector3(-90, 0, 180);

            // Reset timeout
            mouseTimeout.x = mouseTimeout.y;

            // Get the current mouse position
            mousePosition = Input.mousePosition;
            // Get the mouse difference
            Vector2 mDif = (mousePosition - lMousePosition) * mouseSpeed;

            // Set input position
            inputPosition = new Vector2(Mathf.Clamp(inputPosition.x + mDif.x, -1, 1), Mathf.Clamp(inputPosition.y + mDif.y, -1, 1));
            // Set last mouse position
            lMousePosition = mousePosition;

            UpdateHover();

            // Check for a selection
            if (Input.GetKeyDown(debugSelect))
            {
                SelectEvent(0);
                return;
            }
        }

        // Run ONLY if the simulation mode is running
        // -> Not yet implemented. Falls back to tracking debug key
        private void Update()
        {
            KB_TrackMouse();

            // Tracks the controller's radial menu
            VR_PositionMenu();
            VR_TimeoutMenu();

            // Check if the game is running in simulation
            allowDebugInputs = ControllerManager.Instance.runningSimulated;
        }

        // Checks for the currently selected menu option
        void UpdateHover()
        {
            // Update the cursor
            cursorObject.transform.localPosition = new Vector3(inputPosition.x, 0.01f, inputPosition.y);

            // Check if the cursor is in the dead zone. If so throw early
            if (Vector3.Distance(transform.position, (Vector3)inputPosition + transform.position) < deadRadius || inputPosition == Vector2.zero)
            {
                cursorDead = true;
                cursorRotation = 0;
                return;
            }
            else
                cursorDead = false;

            // Get the rotation of the cursor
            cursorRotation = Mathf.Atan2(inputPosition.y, inputPosition.x);

            // Check what slice the cursor is hovered over
            // -> Find what option the value is smaller than + closest to
            float minDistance = 8; // Will never be greater than 8 since the menu is measured in radians
            int sIndex = 0;
            for(int i = 0; i < menuOptions.Length;i++)
            {
                // Check if the option is valid
                if (menuOptions[i] == null)
                    continue;
                // Check if the cursor rotation is less than this option
                if (menuOptions[i].rotation < cursorRotation)
                    continue;
                // Check distance between current rotation and the options rotation
                float wDistance = Mathf.Abs(menuOptions[i].rotation - cursorRotation);
                if(wDistance < minDistance)
                {
                    sIndex = i;
                    minDistance = wDistance;
                }
            }
            // Select sIndex
            hoveredOption = sIndex;
        }

        // Positions the menu on the hand if debug is not allowed
        // -> Has an overflow that surpresses the allignment when disabled. This allows for smoother positioning when first enabling
        void VR_PositionMenu(bool allowWhenDisabled = false)
        {
            // Check if we are in debug mode
            if (allowDebugInputs)
                return;
            // Makes sure the model is active before moving it
            if (!radialMenuModelParent.activeSelf && !allowWhenDisabled)
                return;

            // Pull the position of the hand
            IMixedRealityController tController = ControllerManager.Instance.GetController(controllerSourceId);
            // Check if the controller is null
            if (tController == null)
                return;
            Vector3 controllerPosition = tController.Visualizer.GameObjectProxy.transform.position;
            // Move, rotate, and scale model
            radialMenuModelParent.transform.position = controllerPosition + (Vector3.up * 0.1f);
            radialMenuModelParent.transform.LookAt(Camera.main.transform.position);
            radialMenuModelParent.transform.localEulerAngles = radialMenuModelParent.transform.localEulerAngles + new Vector3(-90, 0, 180);
            radialMenuModelParent.transform.localScale = Vector3.one * 0.15f;
        }

        // Handles the active state of the radial menu model
        Vector2 vrTimeout = Vector2.one * 0.35f;
        void VR_TimeoutMenu()
        {
            // Check if we are in debug mode
            if (allowDebugInputs)
                return;

            // Check if the trackpad is being touched
            if (inputPosition != Vector2.zero)
            {
                // Wait for player input to open the menu
                if (!menuOpened)
                    return;

                // Open the menu
                VR_PositionMenu(true);
                radialMenuModelParent.SetActive(true);
                vrTimeout.x = vrTimeout.y;
                return;
            }
            // Check if the menu has timed out
            if(vrTimeout.x < 0)
            {
                menuOpened = false;
                radialMenuModelParent.SetActive(false);
                vrTimeout.x = 0;
                controllerSourceId = 0;
                return;
            }
            else if(vrTimeout.x == 0)
            {
                return;
            }

            // Reduce the vrTimeout
            vrTimeout.x -= Time.deltaTime;
        }

        // Resets input variables for the menu
        private void ResetInput()
        {
            inputPosition = Vector2.zero;
            mousePosition = Vector2.zero;
            lMousePosition = Vector2.zero;
            hoveredOption = 0;
            cursorDead = true;
            cursorRotation = 0;
            cursorObject.SetActive(false);
        }

        // Count the options in the Menu Option list that aren't null
        int OptionCount()
        {
            int count = 0;
            for(int i = 0;i < menuOptions.Length;i++)
            {
                if (menuOptions[i] != null) count++;
            }

            return count;
        }
        // This is used to build the test version of the menu. For debugging purposes
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 menuPosition = new Vector3(transform.position.x, 10, transform.position.z);
            // Place menu center
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(menuPosition, storedScale);
            // -> Draws deadzone
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(menuPosition, deadRadius);
            // -> Draws input
            Gizmos.color = cursorDead ? Color.gray : Color.magenta;
            Gizmos.DrawWireSphere((Vector3)inputPosition + menuPosition, 0.05f);


            // Set text style
            GUIStyle tStyle = new GUIStyle();
            tStyle.fontSize = 12;
            tStyle.alignment = TextAnchor.UpperCenter;
            tStyle.normal.textColor = Color.white;

            int oCount = OptionCount();
            if (oCount <= 0)
            {
                Handles.Label(menuPosition, "No Options Found", tStyle);
                return;
            }

            for (int i = 0; i < oCount; i++)
            {
                float cRot = menuOptions[i].rotation;
                Gizmos.color = Color.Lerp(Color.red, Color.blue, (float)i / oCount);
                Vector3 endLn = new Vector3(Mathf.Cos(cRot), Mathf.Sin(cRot));
                Gizmos.DrawLine(menuPosition, menuPosition + endLn);
                // Place titles
                Handles.Label(menuPosition + endLn, menuOptions[i].ToString(), tStyle);
            }

            Handles.Label(menuPosition, $"Rotation: {cursorRotation}\nHovered: {menuOptions[hoveredOption].title}", tStyle);
        }
        #endif
    }
}
