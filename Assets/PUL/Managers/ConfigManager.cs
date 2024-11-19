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

        public string sessionId; // auto-generated GUID

        public Dictionary<string, string> settings;

        // Awake is called during initialization and before Start 
        void Awake()
        {
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

