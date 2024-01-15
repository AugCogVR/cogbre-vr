using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PUL
{
    public class EdgeInfo : MonoBehaviour
    {
        // Source node for this edge
        public Transform sourceTransform;

        // Target node for this edge
        public Transform targetTransform;


        public void Update()
        {
            // TODO: I *feel* like we should check if the source or target node positions have changed before
            // recalculating the transforms for every edge model on every single frame. However, the checks 
            // themselves require calculation on every frame as well, and there would be more code needed 
            // to track status, increasing maintenance footprint. So... leaving it as-is for now. 

            // Update the line renderer
            LineRenderer lineRenderer = this.gameObject.GetComponent<LineRenderer>();
            lineRenderer.SetPosition(0, sourceTransform.position);
            lineRenderer.SetPosition(1, targetTransform.position);
            // This is apparently the new location for the default-line material per some rando on the internet
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

            // Update the arrow:
            // Position the arrow closer to the target (.lerp linearly interpolates between two points)
            // Ideally, we want the arrow to be adjacent to the target but not inside the target. 
            // TODO: Don't hardcode the distance adjustment
            float distance = Vector3.Distance(sourceTransform.position, targetTransform.position);
            transform.position = Vector3.Lerp(sourceTransform.position, targetTransform.position, (distance - 0.15f) / distance);
            // Set the arrow's heading toward the target transform
            transform.LookAt(targetTransform);
            // Rotate the model to head the right way
            transform.Rotate(Vector3.up * -90);
        }
    }
}
