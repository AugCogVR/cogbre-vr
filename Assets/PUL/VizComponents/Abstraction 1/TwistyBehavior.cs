using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    public class TwistyBehavior : MonoBehaviour
    {
        // vars for wiggling 
        private int flipCounter = 0;
        private float targetAngle = 60.0f;

        // Start is called before the first frame update
        public void Start()
        {
            // Nothing to do
        }

        // Update is called once per frame
         public void Update()
        {
            // Wiggle the cube back and forth just to know that the cube update works
            flipCounter++;
            if (flipCounter == 100)
            {
                targetAngle *= -1.0f;
                flipCounter = 0;
            }
            // https://docs.unity3d.com/ScriptReference/Transform-rotation.html
            // Smoothly tilts a transform towards a target rotation.
            float smooth = 5.0f;
            // Rotate the cube by converting the angles into a quaternion.
            Quaternion target = Quaternion.Euler(0, targetAngle, 0);
            // Dampen towards the target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * smooth);
        }
    }
}
