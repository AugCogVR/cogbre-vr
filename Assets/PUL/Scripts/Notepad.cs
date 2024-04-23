using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.Input;
using PUL;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(DictationHandler))]
public class Notepad : MonoBehaviour
{
    public TextMeshProUGUI notepadText = null;
    public TMP_InputField notepadInputField = null;

    [Header("Keyboard")]
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
    }

    public void OpenKeyboard()
    {
        Debug.Log("OPENING KEYBOARD");
        GameManager.Instance.ShowKeyboard();
        if (notepadInputField != null)
        {
            // Still updates on close, need to figure out a method to keep text
            NonNativeKeyboard.Instance.OnTextUpdated += _ => { 
                // Band aid solution. doesn't allow for the addition of text
                // -> Make string that contains the working text and the total text, whenever the keyboard is closed, update the total text with current working text
                // --> Biggest problem here will be going back and editing the total text. Working on a solution...
                if(NonNativeKeyboard.Instance.InputField.text != "")
                    notepadInputField.text = NonNativeKeyboard.Instance.InputField.text; 
            };
            //NonNativeKeyboard.Instance.OnClosed += _ => ; Save text here
        }

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
