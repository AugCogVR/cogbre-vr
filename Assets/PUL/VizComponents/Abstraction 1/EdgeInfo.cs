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
            transform.position = Vector3.Lerp(sourceTransform.position, targetTransform.position, .5f);
            transform.LookAt(sourceTransform);
            transform.Rotate(Vector3.right * 90);
            transform.localScale = new Vector3(.025f, Vector3.Distance(sourceTransform.position, targetTransform.position) * 0.5f, .025f);
        }
    }
}
