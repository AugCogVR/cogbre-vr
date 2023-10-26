using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpatialNode : MonoBehaviour
{
    // Stores graph object
    public SpatialGraph graph;
    // Holds ID so its easy to find the node on calls
    public int ID = 0;

    // Stores movement information
    public List<SpatialNode> parents = new List<SpatialNode>();
    public List<SpatialNode> children = new List<SpatialNode>();

    // Stores edge information
    // -> Indexes match up with parents
    public List<LineRenderer> lineRenderers = new List<LineRenderer>();
    float edgeWidth = 0.001f;

    // Movement information
    public float edgeLength = 2;
    float modEdgeLength = 2;
    float lengthSlack = 0.075f;
    public float moveSpeed = 3.5f;
    public bool doneMoving = false;

    // Visual components
    public TextMeshPro idOut;

    // Movement Input
    [Header("Movement Input")]
    // Refers to the movement speed of node to hand
    public float mtSpd = 3;
    // Refers to the state that the node moving is in
    public bool grabMove = false;
    // Refers to the input data
    // -> This is used to keep track of the hand position
    BaseInputEventData mData = null;



    // -> NOTE! THIS IS WHERE PACKAGED DATA WILL GO... NOT YET IMPLEMENTED



    // Updates the node. This allows for interactions from the user
    private void Update()
    {
        // Moves node towards its parents and children average position if not being interacted with
        if (!doneMoving)
        {
            // Adjusts edge length based on the number of objects
            modEdgeLength = edgeLength * Mathf.Clamp(graph.nodes.Count, 1, 300);
            lengthSlack = modEdgeLength / 10;

            // -> Get average movement vector
            Vector3 avgVector = Vector3.zero;
            foreach (SpatialNode node in parents)
            {
                avgVector += (node.transform.position - transform.position) * Vector3.Distance(node.transform.position, transform.position);
            }
            foreach (SpatialNode node in children)
            {
                avgVector += (node.transform.position - transform.position) * Vector3.Distance(node.transform.position, transform.position);
            }
            // -> Move towards average
            if (avgVector.magnitude > modEdgeLength + lengthSlack && avgVector.normalized != Vector3.zero)
                transform.position += avgVector.normalized * Time.deltaTime * moveSpeed;
            else if (avgVector.magnitude < modEdgeLength - lengthSlack && !graph.interacting && avgVector.normalized != Vector3.zero)
                transform.position -= avgVector.normalized * Time.deltaTime * moveSpeed;
            else
            {
                doneMoving = true;
                // Expand graph bounds
                graph.ModifyBounds(transform.position);
            }
        }

        // Updates edges
        for (int i = 0 ; i < parents.Count; i++)
        {
            lineRenderers[i].SetPosition(0, transform.position);
            lineRenderers[i].SetPosition(1, parents[i].transform.position);
        }

        // Make idOut look towards the main camera
        idOut.transform.LookAt(Camera.main.transform);


        // Move node towards the users hand
        if(grabMove && mData != null)
        {
            // -> Move the node towards the hand
            transform.position = Vector3.MoveTowards(transform.position, mData.currentInputModule.transform.position, Time.deltaTime * mtSpd);
        }
    }

    // Connects two nodes treat the current as the child
    public void ConnectToNode(SpatialNode node)
    {
        // Add other node to current's parent
        parents.Add(node);
        // Add current node to other's children
        node.children.Add(this);

        // Increase size and color of other
        node.transform.localScale += Vector3.one * 0.02f;
        Material nodeMat = node.GetComponent<MeshRenderer>().material;
        nodeMat.color += new Color(0.1f, 0.1f, 0.1f, 0.1f);

        // Adds an edge between nodes
        GameObject edgeObj = new GameObject("Edge");
        edgeObj.transform.parent = transform;
        edgeObj.layer = 8;

        LineRenderer newEdge = edgeObj.AddComponent<LineRenderer>();
        newEdge.positionCount = 2;

        newEdge.startWidth = newEdge.endWidth = edgeWidth;

        newEdge.material = graph.graphManager.edgeMaterial;
        lineRenderers.Add(newEdge);
    }

    // Sets visibility of node
    public void SetVisible(bool visible)
    {
        if (visible) 
        {
            gameObject.layer = 0;
            idOut.gameObject.layer = 0;
            // -> Set line renderers
            foreach (LineRenderer line in lineRenderers)
                line.gameObject.layer = 0;
        }
        else
        {
            gameObject.layer = 8;
            idOut.gameObject.layer = 8;
            // -> Set line renderers
            foreach (LineRenderer line in lineRenderers)
                line.gameObject.layer = 8;
        }
    }

    // Sets the interact state
    public void SetInteract(bool state)
    {
        graph.interacting = state;
    }

    // Highlights the active edges
    public void HighlightEdges(bool state)
    {
        // If true, highlight
        if (state)
        {
            // Highlight Parents
            foreach (LineRenderer line in lineRenderers)
            {
                line.material = graph.graphManager.highlightMaterialParent;
            }
            // Highlight Children
            foreach(SpatialNode child in children)
            {
                // Finds index of self in children
                int pIndex = child.parents.FindIndex(x => x.ID == ID);
                child.lineRenderers[pIndex].material = graph.graphManager.highlightMaterialChild;
            }
        }
        // Otherwise, set to default
        else
        {
            // Reset Parents
            foreach (LineRenderer line in lineRenderers)
            {
                line.material = graph.graphManager.edgeMaterial;
            }
            // Reset Children
            foreach (SpatialNode child in children)
            {
                // Finds index of self in children
                int pIndex = child.parents.FindIndex(x => x.ID == ID);
                child.lineRenderers[pIndex].material = graph.graphManager.edgeMaterial;
            }
        }
    }

    // Methods to control pull
    public void StartMoveTowards(BaseInputEventData data)
    {
        grabMove = true;
        mData = data;
    }
    public void EndMoveTowards(BaseInputEventData data)
    {
        grabMove = false;
    }
}
