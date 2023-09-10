using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem.Utilities;

namespace PUL2
{
    public class GameManager : MonoBehaviour
    {
        //#region Singleton

        public NexusClient nexusClient;
        public ActiveOxideData aod;
        //This does not exist yet, uncomment when we have made a menu manager.
        //public MenuManager menuManager;
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("GameManager is NULL");
                }

                return _instance;
            }
        }

        private void Awake()
        {
            // https://bergstrand-niklas.medium.com/setting-up-a-simple-game-manager-in-unity-24b080e9516c
            // If another game manager exists, destroy that game object. If no other game manager exists, 
            // initialize the instance to itself. As a game manager needs to exist throughout all scenes, 
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



            // https://forum.unity.com/threads/xr-device-simulator-wasd-inputs-ignored.1346477/
            // InputSystem.settings.SetInternalFeatureFlag("DISABLE_SHORTCUT_SUPPORT", true);
        }



        // Start is called before the first frame update
        void Start()
        {
            // Initialize Nexus client
            nexusClient = new NexusClient(this);
            nexusClient.NexusSessionInit();
        }
        

        // Update is called once per frame
        void Update()
        {
            // Sync with Nexus
            nexusClient.OnUpdate();
        }

        //#endregion
    }
}
