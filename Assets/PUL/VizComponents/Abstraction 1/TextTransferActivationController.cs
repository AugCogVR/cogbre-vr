using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TextTransferActivationController : MonoBehaviour
{
    //this script resets the enable on text transfer, such that when another object is selected and then the original object is selected,
    //the object can properly display the text of the box onto the preview panel

    //refers to the currently selected cube node
    public GameObject currentObject;
    // checks to see if text transfer is finished
    public bool isFinished = false;

    // Update is called once per frame
    void Update()
    {
        //if text transfer is finished, disable TextTransfer and reset isFinished
        if (isFinished)
        {
            currentObject.GetComponent<TextTransfer>().enabled = false;
            isFinished = false;
        }
        
    }
}
