using System.Collections.Generic;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

namespace PUL
{
    public class NodeInfo : MonoBehaviour
    {
        // ============================
        // BEGIN fields used by HierarchicalGraph -- can be ignored in other cases

        public List<NodeInfo> sourceNodeInfos = new();

        public List<NodeInfo> targetNodeInfos = new();

        public bool added = false;

        // END fields used by HierarchicalGraph -- can be ignored in other cases
        // ============================

        // ============================
        // BEGIN fields used by ForceDirectedGraph -- can be ignored in other cases

        // Mass is used by Force Directed Graph and ignored otherwise
        public float Mass;

        // IsImmobile is used by Force Directed Graph and ignored otherwise
        public bool IsImmobile = false;

        // VirtualPosition is used by Force Directed Graph and ignored otherwise
        public Vector3 VirtualPosition = Vector3.zero;

        // List of indices of other graph nodes to which we're connected
        public readonly List<int> MyEdges = new();

        // Convenience field: what is my index? 
        public int MyIndex;

        // END fields used by ForceDirectedGraph -- can be ignored in other cases
        // ============================

        // Convenience field: what is my associated GameObject?
        public GameObject nodeGameObject = null;


        public void Update()
        {
            // TEST: move nodes steadily outward as a dumb test to make sure edges follow nodes (they do)
            // this.transform.position = Vector3.Scale(this.transform.position, new Vector3(1.0005f, 1.0005f, 1.0005f));
        }
    }
}