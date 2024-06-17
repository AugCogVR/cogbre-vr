using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Bind debug to controller manager delegate
        ControllerManager.Instance.onTouchpadChanged += TouchpadEvent;
    }
    private void OnDisable()
    {
        // Make sure to unbind all delegates
        ControllerManager.Instance.onTouchpadChanged -= TouchpadEvent;
    }

    private void TouchpadEvent(Vector2 value)
    {
        Debug.Log("Radial Menu -> Touchpad Value " + value);
    }
}
