using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


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


    //optimize this to take in several tags.
    GameObject FindNearestGameObject(Vector3 targetPosition)
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("disassembly");
        GameObject nearestGameObject = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject gameObject in gameObjects)
        {
            float distance = Vector3.Distance(gameObject.transform.position, targetPosition);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestGameObject = gameObject;
            }
        }
        return nearestGameObject;
    }

    // Function to highlight the line of text

    void HighlightLine(TextMeshPro text, int lineNumber)
    {
        //so there's no way to draw a rect naturally using tmpro, which i refuse to believe. regardless, this still needs work
    }




    public Camera mainCamera;

    // Update is called once per frame
    void Update()
    {
        GameObject nearestObject = FindNearestGameObject(FindCurrentReticlePos());
        //temporary fix, eventually take in controller data as well.
        if (nearestObject != null)
        {
            TextMeshPro tmPro = nearestObject.transform.GetChild(3).GetComponent<TextMeshPro>();

            if (tmPro != null)
            {
                int nearestLine = TMP_TextUtilities.FindNearestLine(tmPro, FindCurrentReticlePos(), mainCamera);
               // HighlightLine(tmPro, nearestLine);
            }
        }
    }
}

