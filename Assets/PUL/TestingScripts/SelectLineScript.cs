using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectLineScript : MonoBehaviour
{
    public void OnHover()
    {
        Debug.Log("Hovering " + transform.name);
    }
    public void OffHover()
    {
        Debug.Log("Not Hovering " + transform.name);
    }
}
