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
        // NOTE: These values are wired up in the Unity Editor 

        // END: These values are wired up in the Unity Editor
        // ====================================

        // Instance holder
        private static ConfigManager _instance; // this manager is a singleton

        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("ConfigManager is NULL");
                return _instance;
            }
        }

        public string sessionId; // auto-generated GUID

        public Dictionary<string, string> settings;

        // Awake is called during initialization and before Start 
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

            // Create and initialize the configuration options
            // TODO: Make this not hard-coded, I guess
            sessionId = Guid.NewGuid().ToString("N");
            settings = new Dictionary<string, string>();
            settings["sessionName"] = "unset";
            settings["mode"] = "default";

            settings["call_graphs"] = "enabled"; // AFFORDANCE: Spatial semantics

            settings["call_graph_select_buttons"] = "enabled"; // AFFORDANCE: Incremental formalism

            settings["graphs_move"] = "enabled"; // AFFORDANCES: Persistence (spatial memory) and user organization
            settings["slates_move"] = "enabled"; // AFFORDANCES: Persistence (spatial memory) and user organization

            settings["notepad"] = "enabled"; // AFFORDANCE: Note taking

            settings["graph_signals"] = "enabled"; // AFFORDANCE: Signalling
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

