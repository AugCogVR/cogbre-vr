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

        GameObject g1 = graphGenerator.totalNodes[0];
        GameObject g2 = graphGenerator.totalNodes[1]; 
        GameObject g3 = graphGenerator.totalNodes[2]; 

        

        List <GameObject> g1Children = new List<GameObject>();
        g1Children.Add(g2);
        g1Children.Add(g3);
       graphGenerator.AssignChildrenToNode(g1, g1Children);

    }
   
}
