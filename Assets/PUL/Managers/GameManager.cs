using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
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


            // ====================================================
            // JUST SOME JUNK I'M PLAYING AROUND WITH -- DGB
            // string codesample = "endbr64\n" + "sub  rsp,0x8\n" + "mov  rax,qword ptr [0x00103fe8]\n" + "test  rax,rax\n";
            // codesample += codesample + codesample + codesample + codesample + codesample + codesample + codesample + codesample + codesample + codesample;

            // GameObject tokenButtonSlatePrefab = Resources.Load("Prefabs/TokenButtonSlate") as GameObject;
            // GameObject tokenButtonSlate = Instantiate(tokenButtonSlatePrefab);
            // tokenButtonSlate.transform.position = new Vector3(0, 0, 1.5f);

            // ScrollingObjectCollection tokenButtonScrollingObjectCollection = tokenButtonSlate.GetComponentInChildren<ScrollingObjectCollection>();
            // // Debug.Log($"**** EXTENTS {tokenButtonScrollingObjectCollection.GetComponent<Collider>().bounds.extents}");
            // // Vector3 contentExtents = tokenButtonScrollingObjectCollection.GetComponent<Collider>().bounds.extents;
            // // Debug.Log($"**** SCALE {tokenButtonScrollingObjectCollection.GetComponent<Collider>().transform.localScale}");

            // GameObject tokenButtonsContainer = tokenButtonScrollingObjectCollection.transform.Find("Container").gameObject;
            
            // GameObject tokenButtonPrefab = Resources.Load("Prefabs/TokenButton") as GameObject;

            // string[] lines = codesample.Split('\n');
            // float yOffset = 0.0f; 
            // float zOffset = -0.01f;
            // foreach (string line in lines)
            // {
            //     float xOffset = 0.0f; 
            //     float maxYSize = 0f;
                
            //     string[] tokens = line.Split(new char[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);
            //     foreach (string token in tokens)
            //     {
            //         // Make a new token button
            //         GameObject newButton = Instantiate(tokenButtonPrefab);
            //         newButton.transform.SetParent(tokenButtonsContainer.transform);

            //         Debug.Log($"BUTTON COLLIDER BEFORE: {newButton.GetComponent<Collider>().bounds.size}");

            //         // Find preferred TMP size for this token
            //         TMP_Text tmp = newButton.GetComponentInChildren<TMP_Text>(); 
            //         Vector2 prefTextSize = tmp.GetPreferredValues(token);
            //         Debug.Log($"PREFERRED SIZE FOR {token}: {prefTextSize.x} {prefTextSize.y}");

            //         // Debug.Log($"BUTTON SCALE {newButton.transform.localScale.x} {newButton.transform.localScale.y}");

            //         // Resize a bunch of stuff in the button. This sucks but I'm too dumb to find a more elegant way.
            //         tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(prefTextSize.x, prefTextSize.y);
            //         GameObject buttonVis = newButton.transform.Find("CompressableButtonVisuals").gameObject;
            //         buttonVis.transform.localScale = new Vector3(prefTextSize.x, buttonVis.transform.localScale.y, buttonVis.transform.localScale.z);
            //         GameObject backplate = newButton.transform.Find("BackPlate").gameObject;
            //         backplate.transform.localScale = new Vector3(prefTextSize.x, backplate.transform.localScale.y, backplate.transform.localScale.z);
            //         BoxCollider buttonCollider = newButton.GetComponent<BoxCollider>();
            //         buttonCollider.size = new Vector3(prefTextSize.x, buttonCollider.size.y, buttonCollider.size.z);

            //         Debug.Log($"BUTTON COLLIDER AFTER: {newButton.GetComponent<Collider>().bounds.size}");

            //         // Set the text and update mesh
            //         tmp.text = token;
            //         tmp.ForceMeshUpdate();
            //         // Debug.Log($"TMP BOUNDS AFTER: {tmp.bounds.size.x} {tmp.bounds.size.y}");

            //         // Place the new button relative to the container
            //         newButton.transform.localPosition = new Vector3(xOffset + (prefTextSize.x / 2.0f), (yOffset - buttonCollider.size.y), zOffset);
            //         Debug.Log($"BUTTON GOES {newButton.transform.localPosition}");
            //         newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * tokenButtonSlate.transform.localScale.x, newButton.transform.localScale.y * tokenButtonSlate.transform.localScale.y, newButton.transform.localScale.z * tokenButtonSlate.transform.localScale.z);
            //         newButton.transform.localEulerAngles = Vector3.zero;
            //         newButton.transform.name = $"{xOffset} {yOffset} {token}";

            //         // Update the coordinates for the next token
            //         xOffset += prefTextSize.x + 0.02f;
            //         if (buttonCollider.size.y > maxYSize) maxYSize = buttonCollider.size.y;


            //     }
            //     yOffset -= (maxYSize * 1.2f);
            // }
            // tokenButtonScrollingObjectCollection.UpdateContent();
            // ====================================================

        }

        // Update is called once per frame
        void Update()
        {
            // Not the most elegant solution but it gets the job done... -L
            spawnPoint.transform.LookAt(Camera.main.transform.position);
            spawnPoint.transform.Rotate(Vector3.up * 180);
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
