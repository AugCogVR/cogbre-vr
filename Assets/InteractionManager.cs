using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    //stack overflow code, could be broken. -J
    //update, this totally works at finding reticle position
    Vector3 FindCurrentReticlePos()
    {
        Vector3 reticleEndPosition = Vector3.zero; // Initialize the end position variable

        foreach (var source in MixedRealityToolkit.InputSystem.DetectedInputSources)
        {
            // Ignore anything that is not a hand because we want articulated hands
            if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand)
            {
                foreach (var p in source.Pointers)
                {
                    if (p is IMixedRealityNearPointer)
                    {
                        // Ignore near pointers, we only want the rays
                        continue;
                    }
                    if (p.Result != null)
                    {
                        var startPoint = p.Position;
                        var endPoint = p.Result.Details.Point;
                        var hitObject = p.Result.Details.Object;
                        if (hitObject)
                        {
                            // Store the end position of the reticle
                            reticleEndPosition = endPoint;
                        }
                    }
                }
            }
        }

        return reticleEndPosition; // Return the end position of the reticle
    }

    // Update is called once per frame
    void Update()
    {
        Debug.LogWarning(FindCurrentReticlePos());
    }
}
