using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(DictationHandler))]
public class Notepad : MonoBehaviour
{
    public bool isRecording = false;
    public DictationHandler dh = null;
    public TextMeshProUGUI speechOut = null;
    public TextMeshPro tmproHyp = null;
    public TextMeshPro tmproRes = null;
    public TextMeshPro tmproComp = null;
    public TextMeshPro buttonText;
    public void OnEnable()
    {
        if (dh == null)
            dh = gameObject.GetComponent<DictationHandler>();
    }
    public void toggleDictation() {

        isRecording = !isRecording;

        if (isRecording)
        {
            buttonText.text = "<b>Stop Dictation</b>";
            if(dh.IsListening)
                dh.StopRecording();
            dh.StartRecording();
        }
        else
        {
            buttonText.text = "<b>Start Dictation</b>";
            dh.StopRecording();
        }
    }

    public void Update()
    {
        Debug.Log($"DH_STATUS: Listening {dh.IsListening}");
    }
    public void DictationHypothesis(string str)
    {
        Debug.Log($"DICTATION HYPOTHESIS -- Writing string: {str}");
        tmproHyp.text = str;
        speechOut.text = str;
    }
    public void DictationComplete(string str)
    {
        Debug.Log($"DICTATION COMPLETE -- Writing string: {str}");
        tmproComp.text = str;
    }
    public void DictationResult(string str)
    {
        Debug.Log($"DICTATION RESULT -- Writing string: {str}");
        tmproRes.text = str;
    }

}
