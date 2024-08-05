using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialMenuOption
{
    public string title = "Default";
    public float rotation = 0;

    public override string ToString()
    {
        string rStr = "";

        rStr += $"Title: {title} \n";
        rStr += $"Rotation: {rotation} \n";

        return rStr;
    }
}
