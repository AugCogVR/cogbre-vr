using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeManager : MonoBehaviour
{
    GameManager _gameManager;

    public int uselessCounter;

    // OnStart is called by Game Manager Start
    public void OnStart(GameManager gameManager)
    {
        Debug.Log("CubeManager ONSTART");

        _gameManager = gameManager;

        uselessCounter = 0; 
    }

    // OnUpdate is called by Game Manager Update
    public void OnUpdate()
    {
        uselessCounter++;
        int uselessCounterLimit = 1000;
        if (uselessCounter > uselessCounterLimit)
        {
            Debug.Log("CubeManager ONUPDATE / " + uselessCounterLimit);
            uselessCounter = 0;
        }

        
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
