using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PUL
{
    public class SimpleCubeNode : MonoBehaviour
    {
        public readonly List<BasicEdge> MyEdges = new();

        public static SimpleCubeNode New(string nodeName)
        {
            GameObject cubePrefab = Resources.Load("Prefabs/BACube") as GameObject;
            var position = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(2.0f, 5.0f), Random.Range(-5.0f, 5.0f));
            GameObject newCube = Instantiate(cubePrefab, position, Quaternion.identity);
            SimpleCubeNode scn = newCube.AddComponent<SimpleCubeNode>();

            newCube.AddComponent<TwistyBehavior>();
            newCube.GetComponent<TwistyBehavior>().OnStart();            

            // Set random color
            var cubeRenderer = newCube.GetComponent<Renderer>();
            Color newColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 1.0f);
            cubeRenderer.material.SetColor("_Color", newColor);

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

            newCube.name = nodeName;

            return scn;
        }

        public void OnUpdate()
        {
            this.GetComponent<TwistyBehavior>().OnUpdate();     
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