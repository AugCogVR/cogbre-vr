using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;
using static Microsoft.MixedReality.Toolkit.Experimental.UI.KeyboardKeyFunc;
using LitJson;

namespace PUL
{
    public class ConfigManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values can be set in the Unity Editor 

        // auto-generated GUID; needs to be public but shouldn't be set in editor
        public string sessionId; 
        
        // optional session name
        public string sessionName = "unset";

        // a mode is (or will be) a specific collection of the settings below
        public string mode = "default";

        // AFFORDANCE: Spatial semantics
        public bool callGraphsEnabled = true;

        // AFFORDANCE: Incremental formalism
        public bool callGraphSelectButtonsEnabled = true;

        // AFFORDANCES: Persistence (spatial memory) and user organization
        public bool graphsMoveable = true;
        public bool slatesMoveable = true;

        // AFFORDANCE: Note taking
        public bool notepadEnabled = true;

        // AFFORDANCE: Signalling
        public bool graphSignalsEnabled = true;

        // END: These values can be set in the Unity Editor
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

        // public string sessionId; // auto-generated GUID

        // public Dictionary<string, string> settings;

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
            // TODO: Make this not hard-coded, I guess. Or more hard-coded. IDK.
            sessionId = Guid.NewGuid().ToString("N");
        }

        // Start is called after initialization but before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void SetConfigFromJSON(JsonData configJsonData)
        {
            foreach (KeyValuePair<string, JsonData> item in configJsonData)
            {
                switch (item.Key)
                {
                    case "sessionName": sessionName = (string)item.Value; break;
                    case "mode": mode = (string)item.Value; break;
                    case "call_graphs_enabled": callGraphsEnabled = bool.Parse((string)item.Value); break;
                    case "call_graph_select_buttons_enabled": callGraphSelectButtonsEnabled = bool.Parse((string)item.Value); break;
                    case "graphs_moveable": graphsMoveable = bool.Parse((string)item.Value); break;
                    case "slates_moveable": slatesMoveable = bool.Parse((string)item.Value); break;
                    case "notepad_enable": notepadEnabled = bool.Parse((string)item.Value); break;
                    case "graph_signals_enabled": graphSignalsEnabled = bool.Parse((string)item.Value); break;
                }
                Debug.Log("NEW CONFIG: set " + item.Key + " to " + (string)item.Value);
            }
        }

        public Dictionary<string, string> GetSettingsAsDict()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();

            settings["sessionName"] = sessionName;
            settings["mode"] = mode;
            settings["call_graphs_enabled"] = callGraphsEnabled.ToString().ToLower(); 
            settings["call_graph_select_buttons_enabled"] = callGraphSelectButtonsEnabled.ToString().ToLower();
            settings["graphs_moveable"] = graphsMoveable.ToString().ToLower();
            settings["slates_moveable"] = slatesMoveable.ToString().ToLower();
            settings["notepad_enabled"] = notepadEnabled.ToString().ToLower();
            settings["graph_signals_enabled"] = graphSignalsEnabled.ToString().ToLower();

            return settings;
        }

    }
}

