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
        public GameManager GameManager;

        //store all active buttons in a list
        private List<GameObject> activeOIDButtons = new List<GameObject>();

        public bool initialized = false;

       public void MenuInit()
        {
            foreach (Collection CID in aod.CIDs)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(MenuButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = CIDGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.name = CID.Name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = CID.Name;

                // Set button functions - Doesn't work yet
                PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchBegin.AddListener(() => BuildButton(CID));

            }

            CIDGridObjectCollection.UpdateCollection();
            initialized = true;
        }

        public void BuildButton(Collection CID)
        {
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
                GameObject newButton = Instantiate(MenuButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = OIDGridObjectCollection.transform;
                newButton.transform.localEulerAngles = Vector3.zero;
                newButton.transform.name = OID.Name + ": Menu Button";
                newButton.GetComponentInChildren<TextMeshPro>().text = OID.Name;

                activeOIDButtons.Add(newButton);
                // Set button functions
                /*PressableButtonHoloLens2 buttonFunction = newButton.GetComponent<PressableButtonHoloLens2>();
                buttonFunction.TouchEnd.AddListener(delegate { BuildButton(CID); });*/

            }

            yield return new WaitForEndOfFrame();

            OIDGridObjectCollection.UpdateCollection();
        }
    }
}
