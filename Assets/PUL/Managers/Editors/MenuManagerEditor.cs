#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.UI;
using PUL;

// CONVENTION IN THIS FILE:
// CID or cid = oxide collection
// OID or oid = oxide binary
[CustomEditor(typeof(MenuManager))]
public class MenuManagerEditor : Editor
{
    int cidIndex = 0;
    int oidIndex = 0;
    List<OxideBinary> currentOIDs = null;

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
        cidIndex = EditorGUILayout.IntSlider("CID", cidIndex, 0, GameManager.Instance.nexusClient.oxideData.collectionList.Count - 1);
        
        // Build a button to test OIDs of first cid
        if (GUILayout.Button($"Spawn CID[{cidIndex}] OID buttons"))
        {
            // -> Make sure that the CID list is more than 1
            if (GameManager.Instance.nexusClient.oxideData.collectionList.Count < 0)
            {
                Debug.LogError("CID count is <= 0");
                return;
            }

            // -> Build first button
            mm.CollectionButtonCallback(GameManager.Instance.nexusClient.oxideData.collectionList[cidIndex], null/* DGB: FIX THIS LATER -- I AM SO SORRY */);

            // -> Set current OIDs
            currentOIDs = new List<OxideBinary>(GameManager.Instance.nexusClient.oxideData.collectionList[cidIndex].binaryList);
        }

        // Build information based on OIDs
        if (currentOIDs != null && currentOIDs.Count > 0)
        {
            // Space contents
            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            // Input field for OID to test
            oidIndex = EditorGUILayout.IntSlider("OID", oidIndex, 0, currentOIDs.Count - 1);

            // Build a button to select OID graph
            if (GUILayout.Button($"Spawn OID[{oidIndex}] graph"))
            {
                // -> Build Graph
                mm.BinaryButtonCallback(currentOIDs[oidIndex], null/* DGB: FIX THIS LATER -- I AM SO SORRY */);
            }
        }
    }
}
#endif