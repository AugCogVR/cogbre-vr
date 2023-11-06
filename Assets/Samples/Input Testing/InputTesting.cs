using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class InputTesting : MonoBehaviour, IMixedRealitySourceStateHandler
{
    // Register on enable
    private void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
    }
    // Deregister on disable
    private void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
    }

    // Source detected and source lost methods pulled from https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input/input-events?view=mrtkunity-2022-05
    public void OnSourceDetected(SourceStateEventData eventData)
    {
        var hand = eventData.Controller;

        // Only react to Controller input sources
        if (hand != null)
        {
            Debug.Log("Source detected: " + hand.ControllerHandedness + $"  ({hand})");
        }
    }

    public void OnSourceLost(SourceStateEventData eventData)
    {
        var hand = eventData.Controller;

        // Only react to Controller input sources
        if (hand != null)
        {
            Debug.Log("Source lost: " + hand.ControllerHandedness + $"  ({hand})");
        }
    }
}
