using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RM_ToggleObject : RadialMenuOption
{
    // Current function toggles the 1 active notepad, i want to expand on this in the future to allow for an array of notepads.

    public GameObject activeObject = null;

    public override void OnSelect()
    {
        // Toggles the state of the notepad
        if (activeObject != null)
            activeObject.SetActive(!activeObject.activeSelf);
        else
            Debug.LogError("RM Toggle Object (On Select) -> No active object found (NULL)");
    }
}
