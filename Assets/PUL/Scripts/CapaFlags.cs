using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PUL
{
    public class CapaFlags : MonoBehaviour
    {
        public IList<string> flags;
        public GameObject functionCube;
        public List<GameObject> flagObjects;


        public void AssignFlagGameObject()
        {
            int count = flags.Count;
            if (flagObjects != null)
            {
                if (count > 0 && count < 6) {
                    flagObjects[0].SetActive(true);
                    return;
                }
                else if (count > 5 && count < 11)
                {
                    flagObjects[1].SetActive(true);
                    return;
                }
                else if (count > 10 && count < 16)
                {
                    flagObjects[2].SetActive(true);
                    return;
                }
                else if (count > 15)
                {
                    flagObjects[3].SetActive(true);
                    return;
                }
                else
                {
                    return;
                }
            }
        }
    }
}
