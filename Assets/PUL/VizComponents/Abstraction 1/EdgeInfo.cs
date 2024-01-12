using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PUL
{
    public class EdgeInfo : MonoBehaviour
    {
        public Transform sourceTransform;

        public Transform targetTransform;


        public void Update()
        {
            // Update transform of this edge to visually connect two nodes (source and target transforms)

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
