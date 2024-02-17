using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PUL
{
    public class EdgeInfo : MonoBehaviour
    {
        public NodeInfo sourceNodeInfo;

        public NodeInfo targetNodeInfo;

        // The control points defining the line or curve between source and target nodes
        public IList<GameObject> controlPoints;


        public void Update()
        {
            // TODO: I *feel* like we should check if the source or target node positions have changed before
            // recalculating the transforms for every edge model on every single frame. However, the checks 
            // themselves require calculation on every frame as well, and there would be more code needed 
            // to track status, increasing maintenance footprint. So... leaving it as-is for now. 

            // Update the line renderer
            LineRenderer lineRenderer = this.gameObject.GetComponent<LineRenderer>();

            // If there are more than 2 control points, draw bezier curves
            if (controlPoints.Count > 2)
            {
                UpdateLineRendererBezier(lineRenderer);
            }
            // Otherwise just draw a single straight line
            else 
            {
                lineRenderer.SetPosition(0, controlPoints[0].transform.position);
                lineRenderer.SetPosition(1, controlPoints[1].transform.position);
            }

            // Update the arrow:
            // Position the arrow closer to the target (.lerp linearly interpolates between two points)
            // Ideally, we want the arrow to be adjacent to the target but not inside the target. 
            // TODO: Don't hardcode the distance adjustment
            Transform lastSectionStart = (controlPoints[controlPoints.Count - 2]).transform;
            Transform lastSectionEnd = (controlPoints[controlPoints.Count - 1]).transform;
            float distance = Vector3.Distance(lastSectionStart.position, lastSectionEnd.position);
            transform.position = Vector3.Lerp(lastSectionStart.position, lastSectionEnd.position, (distance - 0.12f) / distance);
            // Set the arrow's heading toward the target transform
            transform.LookAt(lastSectionEnd);
            // Rotate the model to head the right way
            transform.Rotate(Vector3.up * -90);
        }

        // Thanks to https://www.gamedeveloper.com/business/how-to-work-with-bezier-curve-in-games-with-unity
        void UpdateLineRendererBezier(LineRenderer lineRenderer)
        {
            int curveCount = (int)(controlPoints.Count / 3);
            int segmentCount = 25;

            for (int curveIdx = 0; curveIdx < curveCount; curveIdx++)
            {
                for (int segmentIdx = 1; segmentIdx <= segmentCount; segmentIdx++)
                {
                    float t = segmentIdx / (float)segmentCount;
                    int nodeIndex = curveIdx * 3;
                    Vector3 point = CalculateCubicBezierPoint(t, controlPoints[nodeIndex].transform.position, controlPoints[nodeIndex + 1].transform.position, controlPoints[nodeIndex + 2].transform.position, controlPoints[nodeIndex + 3].transform.position);
                    lineRenderer.positionCount = (curveIdx * segmentCount) + segmentIdx;
                    lineRenderer.SetPosition((curveIdx * segmentCount) + (segmentIdx - 1), point);
                }                
            }
        }
        Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            
            Vector3 p = uuu * p0; 
            p += 3 * uu * t * p1; 
            p += 3 * u * tt * p2; 
            p += ttt * p3; 
            
            return p;
        }
    }
}
