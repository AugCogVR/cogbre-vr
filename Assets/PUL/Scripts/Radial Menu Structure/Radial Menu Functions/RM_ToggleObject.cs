using PUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class RM_ToggleObject : RadialMenuOption
{
    // Current function toggles the 1 active notepad, i want to expand on this in the future to allow for an array of notepads.

    public GameObject activeObject = null;
    public float scaleSpeed = 1;
    public bool isGrowing = false;    

    Vector3 originalObjectScale = Vector3.one;
    Vector3 targetObjectScale = Vector3.one;


    public override void OnBuild()
    {
        base.OnBuild();
        // Log object's original scale
        originalObjectScale = activeObject.transform.localScale;
    }

    public override void OnSelect()
    {
        Debug.Log($"RM_ToggleObject - {title} -> Selected");

        // Toggles the state of the notepad
        if (activeObject != null)
        {
            bool currentlyActive = activeObject.activeSelf;
            if (!currentlyActive)
            {
                // Activate the active object
                activeObject.transform.position = transform.position;// + (transform.up * (originalObjectScale.y + 1));
                activeObject.transform.localScale = Vector3.zero;
                activeObject.transform.LookAt(Camera.main.transform.position - (2 * (Camera.main.transform.position - transform.position)));
                activeObject.SetActive(true);
                StartAnimating(true);
            }
            else
            {
                // Deactivate the active object
                StartAnimating(false);
                // object will be SetActive(false) once done shrinking
            }
        }
        else
            Debug.LogError("RM Toggle Object (On Select) -> No active object found (NULL)");
    }

    private void StartAnimating(bool isGrowing)
    {
        this.isGrowing = isGrowing;

        // Determines the target size for the object
        if (isGrowing)
            targetObjectScale = originalObjectScale;
        else
            targetObjectScale = Vector3.zero;

        // Queue for update
        GameManager.Instance.StartPersistentCoroutine(AnimateObject());
    }

    float targetPadding = 0.05f;
    private IEnumerator AnimateObject()
    {
        while (Vector3.Distance(activeObject.transform.localScale, targetObjectScale) >= targetPadding)
        { 
            // Interpolate Scale
            activeObject.transform.localScale = Vector3.Slerp(activeObject.transform.localScale, targetObjectScale, Time.deltaTime * scaleSpeed);

            // Check if object should be deactivated
            if ((activeObject.transform.localScale.x <= targetPadding) && (!isGrowing))
                activeObject.SetActive(false);

            yield return new WaitForEndOfFrame();
        }
    }
}
