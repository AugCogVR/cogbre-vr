using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;
using static Microsoft.MixedReality.Toolkit.Experimental.UI.KeyboardKeyFunc;

namespace PUL
{
    public class ConfigManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Menu Manager object

        public GameManager GameManager;

        // END: These values are wired up in the Unity Editor -> Menu Manager object
        // ====================================

        public string userId; // auto-generated GUID

        public string userName; // human-readable, can be artbitrarily changed

        public Dictionary<string, string> settings;

        // Awake is called during initialization and before Start 
        void Awake()
        {
            userId = Guid.NewGuid().ToString("N");
            userName = "default";
            settings = new Dictionary<string, string>();
            settings["mode"] = "default";
        }

        // Start is called after initialization but before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

    }
}

