using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempGraphCreator : MonoBehaviour
{
    public GraphGenerator graphGenerator;
    void Start() 
    {


        graphGenerator.AddNodeToGraph("0", "Stuff!");
        graphGenerator.AddNodeToGraph("1", "Stuff!");
        graphGenerator.AddNodeToGraph("2", "Stuff!");
        graphGenerator.AddNodeToGraph("3", "Stuff!");

        GameObject g1 = graphGenerator.totalNodes[0];
        GameObject g2 = graphGenerator.totalNodes[1]; 
        GameObject g3 = graphGenerator.totalNodes[2];
        GameObject g4 = graphGenerator.totalNodes[3];



        List <GameObject> g1Children = new List<GameObject>();
        List<GameObject> g4Children = new List<GameObject>();
        List<GameObject> g2and3Children = new List<GameObject>();
        g1Children.Add(g2);
        g1Children.Add(g3);
        g2and3Children.Add(g4);
        g4Children.Add(g1);
        graphGenerator.AssignChildrenToNode(g1, g1Children);
        graphGenerator.AssignChildrenToNode(g2, g2and3Children);
        graphGenerator.AssignChildrenToNode(g2, g4Children);
        graphGenerator.AssignChildrenToNode(g3, g2and3Children);
        graphGenerator.AssignChildrenToNode(g4, g4Children);
        graphGenerator.ArrangeGraph(g1, g4);
        //graphGenerator.DestroyGraph();

    }
   
}
