using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class InputTesting : MonoBehaviour, IMixedRealitySourceStateHandler, IMixedRealityInputHandler, IMixedRealityInputHandler<float>, IMixedRealityInputHandler<Vector2>
{
    // Register on enable
    private void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler>(this);
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler<float>>(this);
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler<Vector2>>(this);
    }
    // Deregister on disable
    private void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler>(this);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler<float>>(this);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler<Vector2>>(this);
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
    


    // Controls inputs from binary sources, like buttons.
    public void OnInputUp(InputEventData eventData)
    {
        Debug.Log("Source Input (UP): " + eventData.MixedRealityInputAction.Description);
    }
    public void OnInputDown(InputEventData eventData)
    {
        Debug.Log("Source Input (DOWN): " + eventData.MixedRealityInputAction.Description);
    }



    // Pulls input data from...
    // -> Trigger
    public void OnInputChanged(InputEventData<float> eventData)
    {
        Debug.Log("Source Input (CHANGED): " + eventData.MixedRealityInputAction.Description + $" ({eventData.InputData})");
    }



    // Pulls input data from...
    // -> Touchpad
    public void OnInputChanged(InputEventData<Vector2> eventData)
    {
        Debug.Log("Source Input (CHANGED): " + eventData.MixedRealityInputAction.Description + $" ({eventData.InputData})");
    }
}
  