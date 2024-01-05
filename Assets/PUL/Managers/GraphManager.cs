using System;
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


        void Awake()
        {
        }

        // Start is called before the first frame update
        void Start()
        {
            RandomStartForceDirectedGraph rsfdg = gameObject.AddComponent<RandomStartForceDirectedGraph>();

            Debug.Log("**********************************************************");
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}
