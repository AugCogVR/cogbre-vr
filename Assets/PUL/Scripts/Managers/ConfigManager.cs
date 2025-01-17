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
using IniParser;
using IniParser.Model;
using System.IO;

namespace PUL
{
    public class ConfigManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values can be set in the Unity Editor 

        // END: These values can be set in the Unity Editor
        // ====================================

        // auto-generated GUID; needs to be public but shouldn't be set in editor
        public string sessionId; 

        public IniData configData = null;

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

            // Use a GUID for session ID
            sessionId = Guid.NewGuid().ToString("N");

            // Initialize the configuration options file ini file
            var configParser = new IniParser.IniDataParser();
            configData = configParser.Parse(File.ReadAllText("config.ini"));
        }

        // Start is called after initialization but before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        // Get the value specified by the section and property. 
        // Return null if section or property doesn't exist.
        public string GetSectionProperty(string section, string property)
        {
            string result = null;
            if (configData.Sections.Contains(section))
            {
                if (configData.Sections.FindByName(section).Properties.Contains(property))
                {
                    result = configData[section][property];
                }
            }
            return result;
        }

        // Get the value specified by the property within the "general" section.
        // Return null if section or property doesn't exist.
        public string GetGeneralProperty(string property)
        {
            string result = null;
            if (configData.Sections.Contains("general"))
            {
                if (configData.Sections.FindByName("general").Properties.Contains(property))
                {
                    result = configData["general"][property];
                }
            }
            return result;
        }

        // Get the value specified by the property within the section for the active feature set.
        // Return null if section or property doesn't exist.
        public string GetFeatureSetProperty(string property)
        {
            return GetSectionProperty(GetGeneralProperty("feature_set"), property);
        }

        public void SetConfigFromJSON(JsonData configJsonData)
        {
            foreach (KeyValuePair<string, JsonData> item in configJsonData)
            {
                string[] parts = item.Key.Split('|');
                configData[parts[0]][parts[1]] = (string)item.Value;
                Debug.Log($"NEW CONFIG: set [{parts[0]}][{parts[1]}] to {(string)item.Value}");
            }
        }

        public Dictionary<string, string> GetSettingsAsDict()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();

            foreach (Section section in configData.Sections)
            {
                // Debug.Log("[" + section.Name + "]");
                foreach(Property property in section.Properties)
                {
                    settings[section.Name + "|" + property.Key] = property.Value;
                }
            }

            return settings;
        }
    }
}

