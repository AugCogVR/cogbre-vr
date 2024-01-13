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
            // themselves require calculation as well, and there is more code needed to track status. 
            // So... leaving it as-is for now. 

            // First, find center point halfway between two transforms (.lerp linearly interpolates between two points)
            transform.position = Vector3.Lerp(sourceTransform.position, targetTransform.position, .5f);

            // Second, set heading toward the target transform
            transform.LookAt(targetTransform);

            // Rotate the model to head the right way
            transform.Rotate(Vector3.up * -90);

            // Finally, scale the model to stretch between the two edge transforms
            transform.localScale = new Vector3(Vector3.Distance(sourceTransform.position, targetTransform.position), 1f, 1f);
        }
    }
}
