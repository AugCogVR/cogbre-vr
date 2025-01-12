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

    // Values for handling the keyboard
    [Header("Keyboard")]
    public GameObject openKeyboardButton = null;
    public NonNativeKeyboard keyboard = null;

    // Code in OpenKeyboard that uses the following values is currently disabled
    // public float keyboardDist = 1;
    // public float keyboardScale = 0.2f;
    // public float keyboardVertOffset = -1;
    // TMP_InputField kbInputField = null;

    [Header("Content")]
    public string containedText = ""; // This text gets updated everytime enter is pressed. Gets loaded into keyboard when opened.
    public string workingText = ""; // This text gets updated every key press, cleared when enter is pressed

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
        // Go ahead and call OpenKeyboard since it's always supposed to be visible with the Notepad.
        OpenKeyboard();

        // Pulls dictation handler if none is found
        if (dh == null)
            dh = gameObject.GetComponent<DictationHandler>();
    }

    public void OpenKeyboard()
    {
        // Debug.Log("OPENING KEYBOARD");
        keyboard.PresentKeyboard();

        // Temporarily disable keyboard placement code 
        // keyboard.RepositionKeyboard(Camera.main.transform.position + (Camera.main.transform.forward * keyboardDist) + (Vector3.down * keyboardVertOffset));
        // keyboard.transform.localScale = Vector3.one * keyboardScale;

        // -> Bind functions
        if (notepadInputField != null)
        {
            // Loads contained text into keyboard
            keyboard.OnPlacement += PlaceText;
            // Still updates on close, need to figure out a method to keep text
            keyboard.OnTextUpdated += _ => {
                // Band aid solution. doesn't allow for the addition of text
                // -> Make string that contains the working text and the total text, whenever the keyboard is closed, update the total text with current working text
                // --> Biggest problem here will be going back and editing the total text. Working on a solution...
                if (keyboard.InputField.text != "")
                    notepadInputField.text = workingText = keyboard.InputField.text;
            };
            // Handles event when text is submitted
            keyboard.OnTextSubmitted += SubmitText;
            // Handles event when text is canceled
            keyboard.OnClosed += CloseText;
        }

    }

    private void PlaceText(object sender, System.EventArgs e)
    {
        #if(WRITE_CONSOLE)
            Debug.Log($"Placing Text: From type {sender.GetType()} with arguments {e}");
        #endif
        keyboard.InputField.text = containedText;
        keyboard.InputField.MoveTextEnd(false);
    }
    private void SubmitText(object sender, System.EventArgs e)
    {
        #if (WRITE_CONSOLE)
            Debug.Log($"Submitting Text: From type {sender.GetType()} with arguments {e}");
        #endif
        containedText = workingText;
    }
    private void CloseText(object sender, System.EventArgs e)
    {
        #if (WRITE_CONSOLE)
            Debug.Log($"Submitting Text: From type {sender.GetType()} with arguments {e}");
#endif
        notepadInputField.text = containedText;
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
