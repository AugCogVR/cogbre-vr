using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    GameManager _gameManager;

    private int flipCounter = 0;
    private float targetAngle = 60.0f;

    // OnStart is called by Game Manager Start
    public void OnStart(GameManager gameManager)
    {
        // Debug.Log("CubeManager ONSTART");

        _gameManager = gameManager;
    }

    // OnUpdate is called by Game Manager Update
    public void OnUpdate()
    {
        flipCounter++;
        if (flipCounter == 100)
        {
            targetAngle *= -1.0f;
            flipCounter = 0;
        }

        // https://docs.unity3d.com/ScriptReference/Transform-rotation.html
        // Smoothly tilts a transform towards a target rotation.
        float smooth = 5.0f;
        // Rotate the cube by converting the angles into a quaternion.
        Quaternion target = Quaternion.Euler(0, targetAngle, 0);
        // Dampen towards the target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * smooth);
    }


    // Start is called before the first frame update
    void Start()
    {
        // Empty on purpose -- GameManager will call OnStart() as needed
    }

    // Update is called once per frame
    void Update()
    {
        // Empty on purpose -- GameManager will call OnUpdate() as needed
    }
}
