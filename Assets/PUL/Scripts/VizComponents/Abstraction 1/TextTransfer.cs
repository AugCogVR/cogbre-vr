using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextTransfer : MonoBehaviour
{
    //this text transfers the text of the cube node onto the preview panel in scene. 
    //NOTE: PREVIEW PANEL MUST BE NAMED: "PanelView" *WITHOUT QUOTATIONS
    public Transform Cube;
    public TextTransferActivationController textTransferActivationController;
    // Update is called once per frame
    void Update()
    {
       //finds the preview panel game object
        GameObject Panel = GameObject.Find("PanelView");
        //gets the node title and text
       GameObject CubeNode1 = Cube.transform.GetChild(4).gameObject;
        GameObject CubeNode2 = Cube.transform.GetChild(3).gameObject;
        //gets the Panel's Text Mesh Pro
        TextMeshPro TMP1 = Panel.transform.GetChild(1).gameObject.GetComponent<TextMeshPro>();
        TextMeshPro TMP2 = Panel.transform.GetChild(2).gameObject.GetComponent<TextMeshPro>();
        //sets Panel's text to node info
        TMP1.text = CubeNode1.GetComponent<TextMeshPro>().text;
        TMP2.text = CubeNode2.GetComponent<TextMeshPro>().text;
        //resets TextTransfer's enabling
        textTransferActivationController.isFinished = true;
    }
}
