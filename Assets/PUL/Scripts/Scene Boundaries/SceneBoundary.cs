using PUL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    public class SceneBoundary : MonoBehaviour
    {
        public bool supressGizmos = false;
        public bool opaqueGizmos = false;
        [Space]
        public float correctionSpeed = 1;
        // Boxes that make up the allowed space for slates
        public Bounds[] sceneBounds = new Bounds[0];

        public void Update()
        {
            // TODO: Move to SlateManager
            foreach (SlateData slate in SlateManager.Instance.activeSlates)
            {
                CorrectSlate(slate);
            }
        }


        // Corrects slate positions
        // TODO: Move to SlateManager
        private void CorrectSlate(SlateData slate)
        {
            if (InBounds(slate)) return;

            GameObject obj = slate.obj;
            obj.transform.position = Vector3.Lerp(obj.transform.position, transform.position, Time.deltaTime * correctionSpeed);
        }
        
        // Checks if the object is in any bound listed in scene bounds
        // TODO: Move to SlateManager
        private bool InBounds(SlateData slate)
        {
            GameObject obj = slate.obj;
            foreach (Bounds bound in sceneBounds)
            {
                // Object position - Bound Parent Position (Adjusts bounds away from 0, 0) - ((Direction from object towards center, radius of padding) * Padding size)
                Vector3 cPoint = obj.transform.position - transform.position - ((transform.position - obj.transform.position).normalized * slate.radius);
                if (bound.Contains(cPoint)) return true;
            }
            return false;
        }

        // Draws bounds for the scene
        private void OnDrawGizmos()
        {
            if (supressGizmos)
                return;

            Gizmos.color = Color.green;
            // Draw a box based around each rect
            foreach(Bounds bound in sceneBounds)
            {
                if(opaqueGizmos)
                    Gizmos.DrawCube(bound.center + transform.position, bound.size);
                else
                    Gizmos.DrawWireCube(bound.center + transform.position, bound.size);
            }
        }
    }
}
