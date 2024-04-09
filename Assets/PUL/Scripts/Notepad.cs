using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.Input;
using PUL;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(DictationHandler))]
public class Notepad : MonoBehaviour
{
    public TextMeshProUGUI notepadText = null;


    [Header("Keyboard")]
    public MixedRealityKeyboard keyboard = null;
    public GameObject openKeyboardButton = null;

    [Header("Dictation")]
    bool isRecording = false;
    public DictationHandler dh = null;
    public TextMeshPro dictationButtonTMP;

    [Header("Dictation Debug")]
    public TextMeshPro dictationHypothesisTMP = null;
    public TextMeshPro dictationResultTMP = null;
    public TextMeshPro dictationCompleteTMP = null;
    public void Start()
    {
        // Pulls dictation handler if none is found
        if (dh == null)
            dh = gameObject.GetComponent<DictationHandler>();
        // Pulls keyboard if null
        if (keyboard == null)
            keyboard = GameManager.Instance.keyboard;
    }

    public void Update()
    {
        if(keyboard != null)
            notepadText.text = keyboard.Text;
    }

    public void OpenKeyboard()
    {
        Debug.Log("OPENING KEYBOARD");
        keyboard.ShowKeyboard();
        openKeyboardButton.SetActive(false);
    }



    // Toggles dictation on and off
    public void toggleDictation()
    {

        isRecording = !isRecording;

        if (isRecording)
        {
            dictationButtonTMP.text = "<b>Stop Dictation</b>";
            if (dh.IsListening)
                dh.StopRecording();
            dh.StartRecording();
        }
        else
        {
            dictationButtonTMP.text = "<b>Start Dictation</b>";
            dh.StopRecording();
        }
    }
    // Dictation Methods
    // -> Predicts users message by each word
    public void DictationHypothesis(string str)
    {
        Debug.Log($"DICTATION HYPOTHESIS -- Writing string: {str}");
        dictationHypothesisTMP.text = str;
        notepadText.text = str;
    }
    // -> Predicts users message by each sentence
    public void DictationResult(string str)
    {
        Debug.Log($"DICTATION RESULT -- Writing string: {str}");
        dictationResultTMP.text = str;
    }
    // -> Predicts users message by each complete dictation
    public void DictationComplete(string str)
    {
        Debug.Log($"DICTATION COMPLETE -- Writing string: {str}");
        dictationCompleteTMP.text = str;
    }

}
