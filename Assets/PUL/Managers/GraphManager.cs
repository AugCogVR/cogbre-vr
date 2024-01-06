using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    [System.Serializable]
    public class GraphManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Graph Manager object

        public GameManager gameManager;

        // END: These values are wired up in the Unity Editor -> Menu Manager object
        // ====================================

        IList<GameObject> graphList;        


        void Awake()
        {
        }

        // Start is called before the first frame update
        void Start()
        {
            graphList = new List<GameObject>();
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void BuildFunctionGraph(OxideBinary binary)
        {
            RandomStartForceDirectedGraph rsfdg = gameObject.AddComponent<RandomStartForceDirectedGraph>();
        }
    }
}
