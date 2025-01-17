using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PUL;

public class SpatialGraphManager : MonoBehaviour
{
    // Holds refrece to Spatial Graph Prefab
    public GameObject spatialGraph;

    // Holds refrences to materials
    public Material edgeMaterial;
    public Material highlightMaterialParent;
    public Material highlightMaterialChild;

    // Dictionary that links graphs to binaries
    Dictionary<OxideBinary, SpatialGraph> spatialGraphs;


    // Creates dicitonary
    private void Awake()
    {
        spatialGraphs = new Dictionary<OxideBinary, SpatialGraph>();
    }

    // -> Creates a new graph and indexes it for later use
    public void CreateGraph(OxideBinary binary)
    {
        // Disables graphs
        DisableGraphs();

        // Check for prexisting graph
        if(spatialGraphs.ContainsKey(binary))
        {
            spatialGraphs[binary].gameObject.SetActive(true);
            return;
        }

        // Create new Spatial graph
        GameObject newGraph = Instantiate(spatialGraph, transform);
        // Log in dictionary
        SpatialGraph sGraph = newGraph.GetComponent<SpatialGraph>();
        spatialGraphs[binary] = sGraph;

        // Set graph manager
        sGraph.graphManager = this;

        // Call generate graph
        sGraph.GenerateGraph(binary);
    }

    // Disables all graphs
    public void DisableGraphs()
    {
        // Set all contents in dictionary to inactive
        foreach (SpatialGraph cSGraph in spatialGraphs.Values)
        {
            cSGraph.gameObject.SetActive(false);
        }
    }
}
