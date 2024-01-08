using System.Collections.Generic;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

namespace PUL
{
    public class NodeInfo : MonoBehaviour
    {
        public float Mass;

        public bool IsImmobile = false;

        public Vector3 VirtualPosition = Vector3.zero;

        public readonly List<int> MyEdges = new();

        public int MyIndex;

        public GameObject nodeGameObject = null;


        public void Update()
        {
            // TEST: move nodes steadily outward as a dumb test to make sure edges follow nodes (they do)
            // this.transform.position = Vector3.Scale(this.transform.position, new Vector3(1.0005f, 1.0005f, 1.0005f));
        }
    }
}