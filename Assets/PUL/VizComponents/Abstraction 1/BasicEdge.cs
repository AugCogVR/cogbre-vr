using UnityEngine;
using System.Collections.Generic;
using System.Collections;
namespace PUL
{
    // Code from https://github.com/atonalfreerider/Unity-FDG 

    public class BasicEdge : MonoBehaviour
    {
        public Transform NodeA;
        public Transform NodeB;

        private static LineRenderer lr;

        public static BasicEdge New(string edgeName)
        {
            BasicEdge newCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<BasicEdge>();
            newCylinder.name = edgeName;
            Destroy(newCylinder.GetComponent<CapsuleCollider>());
           newCylinder.transform.localScale = new Vector3(.1f, 1f, .1f);
           return newCylinder;
        }

        public void UpdateEdge()
        {
           transform.position = Vector3.Lerp(NodeA.position, NodeB.position, .5f);
           transform.LookAt(NodeA);
           transform.Rotate(Vector3.right * 90);
            transform.localScale = new Vector3(
               .025f,
               Vector3.Distance(NodeA.position, NodeB.position) * 0.5f,
               .025f);
        }

      
    }
}