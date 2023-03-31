using System.Collections.Generic;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

namespace PUL
{
    public class SimpleCubeNode : MonoBehaviour
    {
        public readonly List<BasicEdge> MyEdges = new();
        public static SimpleCubeNode New(string nodeName, string nodeText)
        {
            // Isntantiate the cube
            GameObject cubePrefab = Resources.Load("Prefabs/GrabbableCube 1 1") as GameObject;
            var position = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(2.0f, 5.0f), Random.Range(-5.0f, 5.0f));
            GameObject newCube = Instantiate(cubePrefab, position, Quaternion.identity);
            SimpleCubeNode scn = newCube.AddComponent<SimpleCubeNode>();
            

            // Add a behavior
           // newCube.AddComponent<TwistyBehavior>();
            //newCube.GetComponent<TwistyBehavior>().OnStart();
            

            // Set random color
            var cubeRenderer = newCube.GetComponent<Renderer>();
            Color newColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1.0f);
            cubeRenderer.material.SetColor("_Color", newColor);

            // Title Text
            // Add object to hold text 
            GameObject textHolder = new GameObject();
            textHolder.transform.parent = newCube.transform;

            // Create text mesh and attach to text holder object; position above cube
            TextMeshPro textObject = textHolder.AddComponent<TextMeshPro>();
            RectTransform rectTransform = textHolder.GetComponent<RectTransform>();
            rectTransform.localPosition = new Vector3(0, 1.0f, 0);
            //rectTransform.sizeDelta = new Vector2(400, 200);

            // Set text contents and style
            textObject.font = Resources.Load("Fonts/LiberationSans", typeof(TMP_FontAsset)) as TMP_FontAsset;
            textObject.color = new Color(0,0,0,1.0f);
            textObject.text = nodeName;
            textObject.fontSize = 1;  
            //textObject.autoSizeTextContainer = true;
            textObject.alignment = TextAlignmentOptions.Center;

            // Content Text
            // Add object to hold text 
            GameObject textHolder2 = new GameObject();
            textHolder2.transform.parent = newCube.transform;

            // Create text mesh and attach to text holder object; position on interior plane
            TextMeshPro textObject2 = textHolder2.AddComponent<TextMeshPro>();
            RectTransform rectTransform2 = textHolder2.GetComponent<RectTransform>();
            rectTransform2.localPosition = new Vector3(0, 0, -0.05f);

            // Set text contents and style
            textObject2.font = Resources.Load("Fonts/LiberationSans", typeof(TMP_FontAsset)) as TMP_FontAsset;
            textObject2.color = new Color(0,0,0,1.0f);
            textObject2.text = nodeText;
            textObject2.fontSize = 0.125f;  
            textObject2.alignment = TextAlignmentOptions.Center;

            newCube.name = nodeName;

            return scn;
        }

        public void OnUpdate()
        {
            // Disable twisting for now. I can't think of a good reason to have it anyway, but it's fun.
            // this.GetComponent<TwistyBehavior>().OnUpdate();
            
            // move nodes steadily outward as a dumb test to make sure edges follow nodes (they do)
            //this.transform.position = Vector3.Scale(this.transform.position, new Vector3(1.0005f, 1.0005f, 1.0005f));
            
            UpdateMyEdges();
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