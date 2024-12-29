using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


namespace PUL
{
    // TextManager basically manages the notepad.

    public class TextManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor 

        public Notepad notepad;

        // END: These values are wired up in the Unity Editor 
        // ====================================

        // Instance holder
        private static TextManager _instance; // this manager is a singleton

        public static TextManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("TextManager is NULL");
                return _instance;
            }
        }

        void Awake()
        {
            // If another instance exists, destroy that game object. If no other game manager exists, 
            // initialize the instance to itself. As this manager needs to exist throughout all scenes, 
            // call the function DontDestroyOnLoad.
            if (_instance)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            DontDestroyOnLoad(this);
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void TextCopyCallback(DynamicScrollbarHandler dynamicScrollbarHandler)
        {
            string selectedText = dynamicScrollbarHandler.selectedInfo;
            if (selectedText.Length > 0)
            {
                Debug.Log($"TO NOTEPAD: {selectedText}");
                notepad.notepadInputField.text += selectedText + "\n";
            }
        }   
    }
}
