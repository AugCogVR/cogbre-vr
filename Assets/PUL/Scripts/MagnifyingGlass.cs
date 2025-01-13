using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class MagnifyingGlass : MonoBehaviour
{
    public Camera[] attachedCameras;
    public PinchSlider pinchSlider;
    float lastPinchValue = -999;

    public float maxFOV = 70f;
    // Update is called once per frame
    void Update()
    {
        // Check for a change in the pinch slider
        if (lastPinchValue == pinchSlider.SliderValue)
            return; // If there is no change in value return
        // On a change in value, update the last frames value and update all cameras
        lastPinchValue = pinchSlider.SliderValue;

        // Update all cameras
        for(int i = 0; i < attachedCameras.Length; i++)
        {
            attachedCameras[i].fieldOfView = maxFOV * Mathf.Clamp(pinchSlider.SliderValue, 0.1f, 1);
        }
    }
}
