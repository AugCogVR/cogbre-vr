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


    //optimized to find the neares game object within 0.1 units of space. a little bandaid-y, so fix later 
    GameObject FindNearestGameObject(Vector3 targetPosition)
    {
        const float thresholdDistance = 0.1f; // Define the threshold distance

        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("disassembly");
        GameObject nearestGameObject = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject gameObject in gameObjects)
        {
            float distance = Vector3.Distance(gameObject.transform.position, targetPosition);

            if (distance < shortestDistance && distance < thresholdDistance)
            {
                shortestDistance = distance;
                nearestGameObject = gameObject;
            }
        }

        // Only return nearestGameObject if it's within the threshold distance
        if (nearestGameObject != null && shortestDistance <= thresholdDistance)
        {
            Debug.Log("NearestGameObject - Works!");
            return nearestGameObject;
        }
        else
        {
            return null; // Return null if no object is found within the threshold distance
        }
    }


    // Function to highlight the line of text
    /*
     * Still does not work... It embedding <u>'s within <u>'s, and that's screwing the formatting up like crazy. If anyone has a better idea of how to tackle this, please feel free to take this over.
    public void HighlightLine(TextMeshPro text, int lineNumber)
{
    // Check if the provided line number is valid
    if (lineNumber < 0 || lineNumber >= text.textInfo.lineCount)
    {
        Debug.LogWarning("Invalid line number!");
        return;
    }

    // Get the index of the first character of the line
    int lineIndex = text.textInfo.lineInfo[lineNumber].firstCharacterIndex;
    // Get the number of characters in the line
    int lineLength = text.textInfo.lineInfo[lineNumber].characterCount;

    // Extract the line text
    string lineText = text.text.Substring(lineIndex, lineLength);

    // Check if the line is already highlighted
    bool isHighlighted = lineText.Contains("<u>");

    // Toggle highlighting
    if (isHighlighted)
    {
        Debug.Log("HighlightText - Removing Highlight");
        // Remove the highlighting tags
        text.text = text.text.Remove(lineIndex, lineLength);
        text.text = text.text.Insert(lineIndex, lineText.Replace("<u>", "").Replace("</u>", ""));
    }
    else
    {
        // Add highlighting tags
        text.text = text.text.Remove(lineIndex, lineLength);
        text.text = text.text.Insert(lineIndex, "<u>" + lineText + "</u>");
    }
}
    */





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
               //HighlightLine(tmPro, nearestLine);
            }
        }
    }
}

