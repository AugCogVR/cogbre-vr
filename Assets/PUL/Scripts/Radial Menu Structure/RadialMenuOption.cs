using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class RadialMenuOption: MonoBehaviour
{
    public string title = "Default";
    public Sprite sprite = null;
    public float rotation = 0; // Stores in radians
    public float size = 0; // Stores in radians
    public float graphicDist = 0;
    private GameObject radialNeedle = null;
    private GameObject radialInfographic = null;

    private void Awake()
    {
        // Pull the radial needle from the editor
        GameObject radialNeedlePrefab = Resources.Load("Prefabs/RadialNeedle") as GameObject;
        radialNeedle = Instantiate(radialNeedlePrefab, Vector3.zero, Quaternion.identity);
        radialNeedle.transform.SetParent(transform);

        // Pull the infographic from the editor
        GameObject radialInfographicPrefab = Resources.Load("Prefabs/RadialInfographic") as GameObject;
        radialInfographic = Instantiate(radialInfographicPrefab, Vector3.zero, Quaternion.identity);
        radialInfographic.transform.SetParent(transform);
    }

    public override string ToString()
    {
        string rStr = "";

        rStr += $"Title: {title} \n";
        rStr += $"Rotation: {rotation} \n";

        return rStr;
    }

    public virtual void OnBuild()
    {
        // Set the radial needle to the right position. Given it is not null
        if(radialNeedle != null) 
        {
            radialNeedle.transform.localPosition = Vector3.zero;
            radialNeedle.transform.localEulerAngles = new Vector3(0, 0, rotation * Mathf.Rad2Deg);
            radialNeedle.transform.localScale = Vector3.one;
            Debug.Log($"Building needle with rotation {rotation}");
        }

        // Set graphic & text to the center of the selection area
        if(radialInfographic != null)
        {
            float midRot = rotation - (size / 2f);
            Vector3 infographicPosition = new Vector3(Mathf.Cos(midRot), Mathf.Sin(midRot), 0) * graphicDist;
            radialInfographic.transform.localPosition = infographicPosition;
            radialInfographic.transform.localScale = Vector3.one;

            // Set label information
            TextMeshPro tmp = radialInfographic.transform.Find("Label").gameObject.GetComponent<TextMeshPro>();
            tmp.text = title;
            if(sprite != null)
            {
                SpriteRenderer sr = radialInfographic.transform.Find("Graphic").gameObject.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
            }
        }
    }

    public virtual void OnSelect() { Debug.Log($"Selected {title} at {rotation}"); return; }
}
