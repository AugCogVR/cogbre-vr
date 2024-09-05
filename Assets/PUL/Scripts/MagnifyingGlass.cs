using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class MagnifyingGlass : MonoBehaviour
{
    public Camera magCamera;
    public PinchSlider pinchSlider;

    public float maxFOV = 70f;
    // Update is called once per frame
    void Update()
    {
        magCamera.fieldOfView = maxFOV * pinchSlider.SliderValue;
    }
}
