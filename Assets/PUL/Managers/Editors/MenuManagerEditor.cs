using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PUL2;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.UI;

[CustomEditor(typeof(MenuManager))]
public class MenuManagerEditor : Editor
{
    int cidIndex = 0;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Space(EditorGUIUtility.singleLineHeight);
        // -> Make sure editor is playing
        if (!Application.isPlaying)
        {
            EditorGUILayout.LabelField("!! Editor Paused in Edit !!", EditorStyles.boldLabel);
            return;
        }

        MenuManager mm = (MenuManager)target;

        // Make sure that the field has been initialized
        if (!mm.initialized) return;

        EditorGUILayout.LabelField("===== Editor =====", EditorStyles.boldLabel);


        // Input field for CID to test
        cidIndex = EditorGUILayout.IntSlider("CID", cidIndex, 0, GameManager.Instance.nexusClient.aod.CIDs.Count - 1);
        
        // Build a button to test OIDs of first cid
        if (GUILayout.Button($"Spawn CID[{cidIndex}] OID buttons"))
        {
            // -> Make sure that the CID list is more than 1
            if (GameManager.Instance.nexusClient.aod.CIDs.Count < 0)
            {
                Debug.LogError("CID count is <= 0");
                return;
            }

            // -> Build first button
            mm.BuildButton(GameManager.Instance.nexusClient.aod.CIDs[cidIndex]);
        }
    }
}
