#define WRITE_CONSOLE

using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PUL
{
    [RequireComponent(typeof(Scrollbar))]
    public class DynamicScrollbarHandler : MonoBehaviour
    {
        public TMP_InputField linkedText = null; // Text linked to the dynamic scrollbar
        public bool updateHeight = false; // Flag to check if the height should be updated [SUBDUES UPDATE INTERACTABLE AREA]
        public bool ignoreLineLimit = true;
        public string selectedInfo = "";

        private RectTransform ltTransform = null; // Refrence to the linked text's rect transform [REQUIRED FOR UPDATE INTERACTABLE AREA]
        private Scrollbar scrollbar = null; // Refrence to contained scrollbar (always on same component as script) [REQUIRED FOR CHECK LINK]
        private RectTransform sbTransform = null; // Refrence to the scrollbar's rect transform [REQUIRED FOR CHECK LINK]
        private bool textOverflowing = false; // Flag that checks if the text is overflowing

        private Image barBGImage = null; // Holds refrence to image stored on same component as scrollbar

        private void Awake()
        {
            scrollbar = GetComponent<Scrollbar>();
            barBGImage = GetComponent<Image>();
            sbTransform = scrollbar.GetComponent<RectTransform>();
            ltTransform = linkedText.GetComponent<RectTransform>();
            if (scrollbar == null || sbTransform == null)
                return;
            linkedText.onValueChanged.AddListener((string value) => CheckLink(value));
            linkedText.onValueChanged.AddListener((string value) => CheckLines(value));
            linkedText.onSelect.AddListener((string value) => SelectionListener(value));
            linkedText.onDeselect.AddListener((string value) => DeselectionListener(value));

            // Set caret to always visible
            // -> Code snippet pulled from https://discussions.unity.com/t/howto-inputfield-always-show-caret/635634
            linkedText.GetType().GetField("m_AllowInput", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(linkedText, true);
            linkedText.GetType().InvokeMember("SetCaretVisible", BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance, null, linkedText, null);
        }

        private void OnEnable()
        {
            UpdateLineLimit();
        }

        // Checks if the linked text should be bound to the scrollbar
        // -> When text overflows the selection area starts to get updated
        void CheckLink(string value)
        {
#if (WRITE_CONSOLE)
            Debug.Log("Checking linked text");
#endif

            // -> Check to see if the value has gone out of bounds
            int lineCount = linkedText.textComponent.textInfo.lineCount + 2; // Added 2 to create a slight buffer
            int height = lineCount * Mathf.CeilToInt(linkedText.textComponent.fontSize);
#if (WRITE_CONSOLE)
            Debug.Log($"Dynamic Scrollbar Handler -> Text with {lineCount - 2} lines modified. \nHeight is {height} with a size of {sbTransform.sizeDelta.y}");
#endif
            // Sets the scrollbar if the text is overflowing
            if (height > sbTransform.sizeDelta.y)
                BindScrollbar();
            else
                UnbindScrollbar();
        }

        // Checks Linked text to see if the line total has changed
        int totalLines = 0;
        void CheckLines(string value)
        {
            // Get total lines of linked text
            int cLines = linkedText.textComponent.textInfo.lineCount;

            // Compare total lines
            if (totalLines == cLines)
                return;

            // Do actions based on if the last frames lines are greater than or less than the current lines
            // -> If we are removing lines and our cursor is in the last 200 characters, scroll to bottom
            if (cLines < totalLines && value.Length - linkedText.caretPosition <= 200)
                SetScrollbarValue(1);

            // Set last lines to current
            totalLines = cLines;
        }

        // Method that handles text selection
        void SelectionListener(string value)
        {
            UnbindScrollbar(false); // Discretely unbind the scrollbar, keeps the slate stable while selecting
        }
        // Method that handles text Deselection
        void DeselectionListener(string value)
        {
            BindScrollbar(false); // Rebind the scrollbar
            PullSelectionData();
        }

        // Method that unbinds the scroll bar to the linked text
        void UnbindScrollbar(bool modifyVisibility = true)
        {
            textOverflowing = false;
            linkedText.verticalScrollbar = null;

            if (modifyVisibility)
            {             
                scrollbar.image.enabled = false;
                barBGImage.enabled = false;
            }
        }
        // Method that binds the scroll bar to the linked text under the condition that the text is overflowing
        void BindScrollbar(bool modifyVisibility = true)
        {
            textOverflowing = true;
            linkedText.verticalScrollbar = scrollbar;

            if (modifyVisibility)
            {
                scrollbar.image.enabled = true;
                barBGImage.enabled = true;
            }
        }
        // Method that pulls the information selected
        void PullSelectionData()
        {
            int start = linkedText.selectionAnchorPosition;
            int end = linkedText.selectionFocusPosition;
            if(start < end)
                selectedInfo = linkedText.textComponent.GetParsedText().Substring(start, end - start);
            else
                selectedInfo = linkedText.textComponent.GetParsedText().Substring(end, start - end);
        }
        
        // Updates the line limit for the contained notepad
        void UpdateLineLimit()
        {
            // Check if the region should ignore the line limit
            if (ignoreLineLimit) return;

            // Grab the height of the allocated space
            float storedHeight = ltTransform.sizeDelta.y;

            // Divide height by the font size
            int totalLines = Mathf.FloorToInt(storedHeight / linkedText.textComponent.fontSize);
            linkedText.lineLimit = totalLines;
        }

        // Clears the notepad
        public void ClearText()
        {
            // Clear the linked text, update the scrollbar
            linkedText.text = "";
            CheckLink("");
        }

        void SetScrollbarValue(int value)
        {
            StartCoroutine(SetScrollbarValue_Coroutine(value));
        }
        IEnumerator SetScrollbarValue_Coroutine(int value) 
        {
            yield return null;// new WaitForEndOfFrame();
#if (WRITE_CONSOLE)
            Debug.Log($"DynamicScrollbarHandler : SetScrollbarValue_Coroutine -> Setting value to {value}");
#endif
            scrollbar.value = value;
        }
    }
}
