using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

namespace PUL2
{
    public class MenuManager : MonoBehaviour
    {
        //refers to the storage of the data actively being returned from Oxide.
        public ActiveOxideData aod = null;
        private bool isInitialized = false;
        //refers to the CID Grid Object Collection, stored inside the Scrolling Object Collection
        public GridObjectCollection CIDGridObjectCollection;
        //refers to the same thing, but for OIDs
        public GridObjectCollection OIDGridObjectCollection;
        //refers to the menu button prefabs that will be instantiated on the menu.
        public GameObject MenuButtonPrefab;
        public GameObject ObjectButtonPrefab;
        public GameManager GameManager;
        //refers to the transform of the UI panel
        public Transform UIPanel;
        //refers to the graph manager
        public SpatialGraphManager graphManager;

        public TextMeshPro disasmContainer;

        //store all active buttons in a list
        private List<GameObject> activeOIDButtons = new List<GameObject>();

        public bool initialized = false;

        // DEBUG GRAPH
       public void MenuInit()
        {
            foreach (Collection CID in aod.CIDs)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(MenuButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = CIDGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = CID.Name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = CID.Name;

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchBegin.AddListener(() => BuildButton(CID));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => BuildButton(CID));
            }

            CIDGridObjectCollection.UpdateCollection();
            initialized = true;
        }

        public void BuildButton(Collection CID)
        {
            // Resets oid information
            ResetOIDInformation();

            // Builds Button
            StartCoroutine(BuildButtonEnum(CID));
        }

        // Function that creates the objects that are associated with given collection
        IEnumerator BuildButtonEnum(Collection CID)
        {   
            //destroy all active buttons
            foreach (GameObject button in activeOIDButtons)
            {
                Object.Destroy(button);
            }
            //clear list space
            activeOIDButtons = new List<GameObject>();

            

            foreach (NexusObject OID in CID.OIDs)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(ObjectButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = OIDGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.localScale = new Vector3(newButton.transform.localScale.x * UIPanel.localScale.x, newButton.transform.localScale.y * UIPanel.localScale.y, newButton.transform.localScale.z * UIPanel.localScale.z);
                newButton.transform.name = OID.Name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = $"<size=145%><line-height=55%><b>{OID.Name}</b>\n<size=60%>{OID.OID}\n<b>Size:</b> {OID.Size}";

                activeOIDButtons.Add(newButton);

                // Set button functions
                // -> Physical Press
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(() => SetOIDInformation(OID));
                // -> Ray Press
                Interactable distanceInteract = newButton.GetComponent<Interactable>();
                distanceInteract.OnClick.AddListener(() => SetOIDInformation(OID));

            }

            yield return new WaitForEndOfFrame();

            OIDGridObjectCollection.UpdateCollection();
        }

        // Sets information about an oid and builds a graph
        public void SetOIDInformation(NexusObject OID)
        {
            // Reset OID information
            ResetOIDInformation();

            // Builds a graph based on information contained
            // -> NOTE! CURRENTLY GENERATES A RANDOM GRAPH
            graphManager.CreateGraph(OID);
            disasmContainer.text = OID.disassembly;
        }

        void ResetOIDInformation()
        {
            // Resets current graph
            graphManager.DisableGraphs();
        }
    }
}

