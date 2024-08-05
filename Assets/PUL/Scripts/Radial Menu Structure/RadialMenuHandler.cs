using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

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
    // -> The next of these objects is the Radial Menu Function. This one will perform a basic function when selected
    // - L

    // Defines the options contained in the menu
    public RadialMenuOption[] menuOptions = new RadialMenuOption[8];
    public KeyCode debugOpen = KeyCode.Z;

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

    // Models used in the menu
    

    // Start is called before the first frame update
    void Start()
    {
        // Bind debug to controller manager delegate
        ControllerManager.Instance.onTouchpadChanged += TouchpadEvent;

        // Build the menu
        BuildMenu();
    }
    // Sets the properties in Radial Menu Options used for the menu
    void BuildMenu()
    {
        // Debug list of items used for testing the menu
        for (int i = 0; i < menuOptions.Length; i++)
        {
            RadialMenuOption option = new RadialMenuOption();
            option.title += " " + i;
            menuOptions[i] = option;
        }

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
        float cRot = 90;
        // Set the rotation
        for (int i = 0; i < menuOptions.Length; i++)
            if (menuOptions[i] != null)
            {
                // Get a normalized rotation
                float rad = Mathf.Deg2Rad * cRot;
                menuOptions[i].rotation = Mathf.Atan2(Mathf.Sin(rad), Mathf.Cos(rad));
                cRot -= pieSize;
            }
    }

    private void OnDisable()
    {
        // Make sure to unbind all delegates
        ControllerManager.Instance.onTouchpadChanged -= TouchpadEvent;
    }

    private void TouchpadEvent(Vector2 value)
    {
        Debug.Log("Radial Menu -> Touchpad Value " + value);
        inputPosition = value;
        UpdateHover();
    }
    Vector2 mousePosition = Vector2.zero; // Current mouse position
    Vector2 lMousePosition = Vector2.zero; // Mouse position last frame
    Vector2 mouseTimeout = Vector2.one * 7.5f;
    float mouseSpeed = 0.005f;
    private void TrackMouse()
    {
        // Check for debug key press
        if (!Input.GetKey(debugOpen))
            return;

        // Get the current mouse position
        mousePosition = Input.mousePosition;
        // Get the mouse difference
        Vector2 mDif = (mousePosition - lMousePosition) * mouseSpeed;
        
        // Check for timeout
        if(mDif == Vector2.zero)
            mouseTimeout.x -= Time.deltaTime;
        if(mouseTimeout.x < 0)
        {
            ResetInput();
            mouseTimeout.x = mouseTimeout.y;
            return;
        }

        
        // Set input position
        inputPosition = new Vector2(Mathf.Clamp(inputPosition.x + mDif.x, -1, 1), Mathf.Clamp(inputPosition.y + mDif.y, -1, 1));
        // Set last mouse position
        lMousePosition = mousePosition;

        UpdateHover();
    }

    // Run ONLY if the simulation mode is running
    // -> Not yet implemented. Falls back to tracking debug key
    private void Update()
    {
        TrackMouse();
    }

    // Checks for the currently selected menu option
    void UpdateHover()
    {
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

    // Resets input variables for the menu
    private void ResetInput()
    {
        inputPosition = Vector2.zero;
        mousePosition = Vector2.zero;
        lMousePosition = Vector2.zero;
        hoveredOption = 0;
        cursorDead = true;
        cursorRotation = 0;
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
    private void OnDrawGizmos()
    {
        Vector3 menuPosition = transform.position;
        // Place menu center
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(menuPosition, 1f);
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
}
