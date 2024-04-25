#define WRITE_CONSOLE

using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Scrollbar))]
public class DynamicScrollbarHandler : MonoBehaviour
{
    public TMP_InputField linkedText = null; // Text linked to the dynamic scrollbar
    public bool updateHeight = false; // Flag to check if the height should be updated [SUBDUES UPDATE INTERACTABLE AREA]
    public string selectedInfo = "";

    private RectTransform ltTransform = null; // Refrence to the linked text's rect transform [REQUIRED FOR UPDATE INTERACTABLE AREA]
    private Scrollbar scrollbar = null; // Refrence to contained scrollbar (always on same component as script) [REQUIRED FOR CHECK LINK]
    private RectTransform sbTransform = null; // Refrence to the scrollbar's rect transform [REQUIRED FOR CHECK LINK]
    private bool textOverflowing = false; // Flag that checks if the text is overflowing

    private void Awake()
    {
        scrollbar = GetComponent<Scrollbar>();
        sbTransform = scrollbar.GetComponent<RectTransform>();
        ltTransform = linkedText.GetComponent<RectTransform>();
        if (scrollbar == null || sbTransform == null)
            return;
        linkedText.onValueChanged.AddListener((string value) => CheckLink(value));
        linkedText.onSelect.AddListener((string value) => SelectionListener(value));
        linkedText.onDeselect.AddListener((string value) => DeselectionListener(value));
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
        // Sets the scrollbar if the text is overflowing
        if (height > sbTransform.sizeDelta.y)
        {
            textOverflowing = true;
            BindScrollbar();
            // Updates the selection area
            UpdateInteractableArea(height);
        }
        else
            UnbindScrollbar();
    }

    // Method that handles text selection
    void SelectionListener(string value)
    {
        UnbindScrollbar();
    }
    // Method that handles text Deselection
    void DeselectionListener(string value)
    {
        BindScrollbar();
        PullSelectionData();
    }

    // Method that unbinds the scroll bar to the linked text
    void UnbindScrollbar()
    {
        linkedText.verticalScrollbar = null;
    }
    // Method that binds the scroll bar to the linked text under the condition that the text is overflowing
    void BindScrollbar()
    {
        if (!textOverflowing)
            return;

        linkedText.verticalScrollbar = scrollbar;
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
    // Updates the selectable region of the linked text
    void UpdateInteractableArea(int height)
    {
        // Check case for method
        if (!updateHeight)
            return;

        // -> Uses height to change the linked text interactable area
        ltTransform.sizeDelta = new Vector2(ltTransform.sizeDelta.x, height);
    }
}
