using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    // Code from https://github.com/atonalfreerider/Unity-FDG 

    class BasicNode : MonoBehaviour
    {
        public readonly List<BasicEdge> MyEdges = new();

        public static BasicNode New(string nodeName)
        {
            BasicNode newSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<BasicNode>();
            newSphere.name = nodeName;
            return newSphere;
        }

        public void UpdateMyEdges()
        {
            foreach (BasicEdge myEdge in MyEdges)
            {
                myEdge.UpdateEdge();
            }
        }
    }
}