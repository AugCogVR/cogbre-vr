using PUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace PUL
{
    public class RM_ToggleObject : RadialMenuOption
    {
        // Current function toggles the 1 active notepad, i want to expand on this in the future to allow for an array of notepads.

        public GameObject activeObject = null;
        public float scaleSpeed = 1;
        public bool isGrowing = false;
        // Optional obect that follows objects as they are spawned
        public TrailRenderer spawnTrail;

        Vector3 originalObjectScale = Vector3.one;
        Vector3 targetObjectScale = Vector3.one;

        Vector3 originalObjectPosition = Vector3.zero;
        Vector3 targetObjectPosition = Vector3.zero;

        bool animating = false;

        public override void OnBuild()
        {
            base.OnBuild();
            // Log object's original position and scale
            originalObjectPosition = activeObject.transform.position;
            originalObjectScale = activeObject.transform.localScale;

            // Check if there is a spawn trail, if so clear and disable
            if(spawnTrail != null)
            {
                spawnTrail.Clear();
                spawnTrail.gameObject.SetActive(false);
            }
        }

        public override void OnSelect()
        {
            Debug.Log($"RM_ToggleObject - {title} -> Selected");

            // Check if object is locked (animating)
            if (animating)
            {
                Debug.Log($"RM_ToggleObject - {title} -> Returned early (animating)");
                return;
            }

            // Toggles the state of the active object
            if (activeObject != null)
            {
                bool currentlyActive = activeObject.activeSelf;
                if (!currentlyActive)
                {
                    // Activate the active object
                    activeObject.transform.position = transform.position; // Spawned object will slide to last known place
                    activeObject.transform.localScale = Vector3.zero;

                    // OLD ROTATION FUNCTION
                    // activeObject.transform.LookAt(Camera.main.transform.position - (2 * (Camera.main.transform.position - transform.position)));

                    activeObject.SetActive(true);
                    StartAnimating(true);
                }
                else
                {
                    // Mark down last known position of the active object
                    originalObjectPosition = activeObject.transform.position;
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

            // Determines the target size and position for the object
            if (isGrowing)
            {
                targetObjectPosition = originalObjectPosition;
                targetObjectScale = originalObjectScale;
            }
            else
            {
                targetObjectPosition = transform.position;
                targetObjectScale = Vector3.zero;
            }

            // Queue for update
            GameManager.Instance.StartPersistentCoroutine(AnimateObject());
        }

        float targetPadding = 0.05f;
        private IEnumerator AnimateObject()
        {
            // Simple lock to check if the object is animating
            animating = true;

            // If there is a spawn trail, remove parent and enable the object
            // Position on active object before enabling
            if (spawnTrail != null)
            {
                spawnTrail.transform.parent = null;
                spawnTrail.transform.position = activeObject.transform.position;
                spawnTrail.Clear();
                spawnTrail.gameObject.SetActive(true);
            }

            // Run animation
            while (Vector3.Distance(activeObject.transform.localScale, targetObjectScale) >= targetPadding || Vector3.Distance(activeObject.transform.position, targetObjectPosition) >= targetPadding)
            {
                // Interpolate position
                activeObject.transform.position = Vector3.Slerp(activeObject.transform.position, targetObjectPosition, Time.deltaTime * scaleSpeed);
                // Interpolate Scale
                activeObject.transform.localScale = Vector3.Slerp(activeObject.transform.localScale, targetObjectScale, Time.deltaTime * scaleSpeed);
                // Update rotation
                activeObject.transform.LookAt(Camera.main.transform.position - (2 * (Camera.main.transform.position - activeObject.transform.position)));

                // If there is a spawn trail, update its position
                if (spawnTrail != null)
                    spawnTrail.transform.position = activeObject.transform.position;

                // Check if object should be deactivated
                if ((activeObject.transform.localScale.x <= targetPadding) && (!isGrowing))
                    activeObject.SetActive(false);

                yield return new WaitForEndOfFrame();
            }

            // If there is a spawn trail, set parent and disable the object
            if (spawnTrail != null)
            {
                yield return new WaitForSecondsRealtime(spawnTrail.time);
                spawnTrail.transform.parent = transform;
                spawnTrail.gameObject.SetActive(false);
            }

            // Simple lock to check if the object is animating
            animating = false;
        }
    }
}
