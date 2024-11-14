using PUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class RM_ToggleObject : RadialMenuOption
{
    // Current function toggles the 1 active notepad, i want to expand on this in the future to allow for an array of notepads.

    public GameObject activeObject = null;
    public bool visibleOnStart = false;
    public float scaleSpeed = 1;
    

    Vector3 originalObjectScale = Vector3.one;
    Vector3 targetObjectScale = Vector3.one;


    public override void OnBuild()
    {
        base.OnBuild();
        // Log object's original scale
        targetObjectScale = originalObjectScale = activeObject.transform.localScale;

        if(!visibleOnStart)
        {
            activeObject.transform.localScale = Vector3.zero;
            activeObject.SetActive(false);
        }
    }

    public override void OnSelect()
    {
        Debug.Log($"RM_ToggleObject - {title} -> Selected");

        // Toggles the state of the notepad
        if (activeObject != null)
        {
            if (!activeObject.activeSelf)
                activeObject.transform.position = transform.position;// + (transform.up * (originalObjectScale.y + 1));

            activeObject.transform.LookAt(Camera.main.transform.position - (2 * (Camera.main.transform.position - transform.position)));
            SetActiveState(!activeObject.activeSelf);
        }
        else
            Debug.LogError("RM Toggle Object (On Select) -> No active object found (NULL)");
    }

    private void SetActiveState(bool state)
    {
        // Determines the target size for the object
        if (state)
            targetObjectScale = originalObjectScale;
        else
            targetObjectScale = Vector3.zero;

        // Queue for update
        GameManager.Instance.StartPersistentCoroutine(UpdateSmoothing());
    }

    float targetPadding = 0.05f;
    private IEnumerator UpdateSmoothing()
    {
        while (Vector3.Distance(activeObject.transform.localScale, targetObjectScale) >= targetPadding)
        { 
            // Interpolate Scale
            activeObject.transform.localScale = Vector3.Slerp(activeObject.transform.localScale, targetObjectScale, Time.deltaTime * scaleSpeed);

            // Check if object should be active or not
            if (activeObject.transform.localScale.x <= targetPadding)
                activeObject.SetActive(false);
            else
                activeObject.SetActive(true);

            yield return new WaitForEndOfFrame();
        }
    }
}
