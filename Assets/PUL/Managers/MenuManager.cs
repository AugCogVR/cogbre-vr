using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;

namespace PUL2
{
    public class MenuManager : MonoBehaviour
    {
        //refers to the storage of the data actively being returned from Oxide.
        public ActiveOxideData aod = null;
        //refers to the CID Grid Object Collection, stored inside the Scrolling Object Collection
        public GridObjectCollection CIDGridObjectCollection;
        //refers to the same thing, but for OIDs
        public GridObjectCollection OIDGridObjectCollection;
        //refers to the menu button prefabs that will be instantiated on the menu.
        public GameObject MenuButtonPrefab;

        
        // Start is called before the first frame update
        void Start()
        {

        }

        void MenuInit()
        {
            foreach (string CID in aod.CIDs)
            {
                // Instantiate the button prefab.
                GameObject newButton = Instantiate(MenuButtonPrefab);

                // Set the parent to the GridObjectCollection.
                newButton.transform.parent = CIDGridObjectCollection.transform;
            }

            CIDGridObjectCollection.UpdateCollection();
        }

        // Update is called once per frame
        void Update()
        {
            if(aod != null)
            {
                MenuInit();
            }
        }
    }
}
