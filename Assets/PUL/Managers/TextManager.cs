using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


namespace PUL
{
    // TextManager basically manages the notepad.

    public class TextManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor 

        public Notepad notepad;

        // END: These values are wired up in the Unity Editor 
        // ====================================

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void TextCopyCallback(DynamicScrollbarHandler dynamicScrollbarHandler)
        {
            string selectedText = dynamicScrollbarHandler.selectedInfo;
            if (selectedText.Length > 0)
            {
                Debug.Log($"TO NOTEPAD: {selectedText}");
                notepad.notepadInputField.text += selectedText + "\n";
            }
        }   


    // ===========================
    // The commented-out code below was preserved from the former "InteractionManager" class -- DGB
    // ===========================

    //     //stack overflow code, could be broken. -J
    //     //update, this totally works at finding reticle position
    //     Vector3 FindCurrentReticlePos()
    //     {
    //         Vector3 reticleEndPosition = Vector3.zero; // Initialize the end position variable

    //         foreach (var source in MixedRealityToolkit.InputSystem.DetectedInputSources)
    //         {
    //             // Ignore anything that is not a hand because we want articulated hands
    //             if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand)
    //             {
    //                 foreach (var p in source.Pointers)
    //                 {
    //                     if (p is IMixedRealityNearPointer)
    //                     {
    //                         // Ignore near pointers, we only want the rays
    //                         continue;
    //                     }
    //                     if (p.Result != null)
    //                     {
    //                         var startPoint = p.Position;
    //                         var endPoint = p.Result.Details.Point;
    //                         var hitObject = p.Result.Details.Object;
    //                         if (hitObject)
    //                         {
    //                             // Store the end position of the reticle
    //                             reticleEndPosition = endPoint;
    //                         }
    //                     }
    //                 }
    //             }
    //         }

    //         return reticleEndPosition; // Return the end position of the reticle
    //     }


    //     //optimized to find the neares game object within 0.1 units of space. a little bandaid-y, so fix later 
    //     GameObject FindNearestGameObject(Vector3 targetPosition)
    //     {
    //         const float thresholdDistance = 1000f; // Define the threshold distance || Logic is off here, figure out a way to rework this

    //         GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("disassembly");
    //         GameObject nearestGameObject = null;
    //         float shortestDistance = Mathf.Infinity;

    //         foreach (GameObject gameObject in gameObjects)
    //         {
    //                 float distance = Vector3.Distance(gameObject.transform.position, targetPosition);

    //             if (distance < shortestDistance && distance < thresholdDistance)
    //             {
    //                 shortestDistance = distance;
    //                 nearestGameObject = gameObject;
    //             }
    //         }

    //         // Only return nearestGameObject if it's within the threshold distance
    //         if (nearestGameObject != null && shortestDistance <= thresholdDistance)
    //         {
    //             // Debug.Log("NearestGameObject Returning: " + nearestGameObject.name);
    //             return nearestGameObject;
    //         }
    //         else
    //         {
    //             return null; // Return null if no object is found within the threshold distance
    //         }
    //     }


    //     // Temporary solution, there has to be a more efficient manner to get line contents using TMPRO
    //     // -L
    //     string GetLineContents(string text, int line)
    //     {
    //         // Count number of \n
    //         for (int i = 0; i < line; i++)
    //         {
    //             // -> Check for more \n
    //             if(!text.Contains("\n"))
    //                 return "NONE";

    //             // -> Cut out \n
    //             int spacing = text.IndexOf("\n");
    //             text = text.Substring(spacing + 2);
    //         }

    //         if (!text.Contains("\n"))
    //             return text;
    //         else
    //             return text.Substring(0, text.IndexOf("\n"));
    //     }


    //     // Modify to replace formatting with highlight. Store new formatting
    //     // Then its a simple check and swap to revert it
    //     // -L

    //     // Function to highlight the line of text
    //     /*
    //     * Still does not work... It embedding <u>'s within <u>'s, and that's screwing the formatting up like crazy. If anyone has a better idea of how to tackle this, please feel free to take this over.
    //     public void HighlightLine(TextMeshPro text, int lineNumber)
    // {
    //     // Check if the provided line number is valid
    //     if (lineNumber < 0 || lineNumber >= text.textInfo.lineCount)
    //     {
    //         Debug.LogWarning("Invalid line number!");
    //         return;
    //     }

    //     // Get the index of the first character of the line
    //     int lineIndex = text.textInfo.lineInfo[lineNumber].firstCharacterIndex;
    //     // Get the number of characters in the line
    //     int lineLength = text.textInfo.lineInfo[lineNumber].characterCount;

    //     // Extract the line text
    //     string lineText = text.text.Substring(lineIndex, lineLength);

    //     // Check if the line is already highlighted
    //     bool isHighlighted = lineText.Contains("<u>");

    //     // Toggle highlighting
    //     if (isHighlighted)
    //     {
    //         Debug.Log("HighlightText - Removing Highlight");
    //         // Remove the highlighting tags
    //         text.text = text.text.Remove(lineIndex, lineLength);
    //         text.text = text.text.Insert(lineIndex, lineText.Replace("<u>", "").Replace("</u>", ""));
    //     }
    //     else
    //     {
    //         // Add highlighting tags
    //         text.text = text.text.Remove(lineIndex, lineLength);
    //         text.text = text.text.Insert(lineIndex, "<u>" + lineText + "</u>");
    //     }
    // }
    //     */





    //     public Camera mainCamera;

    //     // Update is called once per frame
    //     void Update()
    //     {

    //     }


        


    //     // -> Vector to hold selected position
    //     Vector3 highligtedPosition = Vector3.zero;
    //     // -> Vector to hold index range
    //     Vector2Int highlightIRange = Vector2Int.zero;

    //     // Dictates when a highlight is started
    //     public void HighlightIndividual()
    //     {
    //         highligtedPosition = FindCurrentReticlePos();

    //         GameObject nearestObject = FindNearestGameObject(highligtedPosition);
    //         //temporary fix, eventually take in controller data as well.
    //         if (nearestObject != null)
    //         {
    //             TextMeshPro tmPro = nearestObject.transform.GetChild(3).GetComponent<TextMeshPro>();
    //             // Debug.Log("Nearest Object: " + nearestObject);

    //             if (tmPro != null)
    //             {
    //                 //int nearestLine = TMP_TextUtilities.FindNearestLine(tmPro, highligtedPosition, mainCamera);
    //                 // I was testing other commands to see if there was any better accuracy :) -L
    //                 // int intersectingLine = TMP_TextUtilities.FindIntersectingLine(tmPro, FindCurrentReticlePos(), mainCamera);
    //                 //HighlightLine(tmPro, nearestLine);
    //                 //Debug.Log($"Found Line ({nearestLine}): {GetLineContents(tmPro.text, nearestLine)} (Index {nearestIndex})");

    //                 // -> Highlighting a single word
    //                 int nearestIndex = TMP_TextUtilities.GetCursorIndexFromPosition(tmPro, highligtedPosition, mainCamera);
    //                 string workingText = tmPro.text;
    //                 highlightIRange = Vector2Int.one * nearestIndex;

    //                 // -> Find start of word
    //                 int overflowCheck = 0;
    //                 while(highlightIRange.x >= 0 && IsEndText(workingText[highlightIRange.x]) && overflowCheck < 1000)
    //                 {
    //                     highlightIRange.x--;
    //                     overflowCheck++;
    //                 }
    //                 while (highlightIRange.y < workingText.Length && IsEndText(workingText[highlightIRange.y]) && overflowCheck < 1000)
    //                 {
    //                     highlightIRange.y++;
    //                     overflowCheck++;
    //                 }

    //                 // -> Debug the selected word
    //                 Debug.Log($"Found Word: {workingText.Substring(highlightIRange.x, highlightIRange.y - highlightIRange.x)} ({highlightIRange})");
    //             }
    //             else
    //                 Debug.Log("!!! NO TMPRO FOUND !!! (InteractionManager.cs)");
    //         }
    //         else
    //             Debug.Log("!!! NO NEAREST OBJECT FOUND !!! (InteractionManager.cs)");
    //     }

    //     bool IsEndText(char c)
    //     {
    //         return c != ' ' && c != '\n' && c != '>' && c != '<';
    //     }

    // ===========================
    // The commented-out code above was preserved from the former "InteractionManager" class -- DGB
    // ===========================

    }
}
