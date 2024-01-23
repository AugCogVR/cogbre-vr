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

namespace PUL
{
    // GameManager keeps track of relevant objects.
    // It is intended to be a singleton per the pattern described at:
    // https://bergstrand-niklas.medium.com/setting-up-a-simple-game-manager-in-unity-24b080e9516c
    public class GameManager : MonoBehaviour
    {
        // ====================================
        // NOTE: These values are wired up in the Unity Editor -> Game Manager object

        public NexusClient nexusClient;

        public MenuManager menuManager;

        public GraphManager graphManager;

        // END: These values are wired up in the Unity Editor -> MenuManager object
        // ====================================

        
        // Spawn point for new slates, graphs, etc. 
        private GameObject spawnPoint;

        private static GameManager _instance; // GameManager is a singleton

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
        }

        // Start is called before the first frame update
        void Start()
        {
            // Create a spawn point relative to the UI Panel
            spawnPoint = new GameObject();
            spawnPoint.transform.parent = menuManager.UIPanel.transform;
            spawnPoint.transform.localPosition = new Vector3(0.9f, 0.25f, 0);
        }

        // Update is called once per frame
        void Update()
        {
        }

        // Return the position of the spawn point.
        public Vector3 getSpawnPosition()
        {
            return spawnPoint.transform.position;
        }

        // Return the rotation of the spawn point.
        public Quaternion getSpawnRotation()
        {
            return spawnPoint.transform.rotation;
        }
    }
}
