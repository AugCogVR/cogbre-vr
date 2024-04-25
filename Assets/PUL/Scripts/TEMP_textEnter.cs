using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TEMP_textEnter : MonoBehaviour
{
    public TMP_InputField textField;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(aRoutine());
    }
    IEnumerator aRoutine()
    {
        int i = 0;
        while(i < 100)
        {
            textField.text += $"[{i}] This is a silly goofy lil test :)\n";
            yield return new WaitForSeconds(0.05f);
            i++;
        }
    }
}
