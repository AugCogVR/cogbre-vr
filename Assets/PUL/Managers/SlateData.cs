using PUL;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class SlateData
{
    public string name;
    public GameObject obj;
    public float radius = 1.0f;
    public bool simulateMovement = false; // Used to position slates around the spawn area
    int movementStallCheck = 0; // Checks how many frames the slate has been idle for
    int movementStallThreshold = 50; // Limit for the amount of frames the slate can stall before stopping movement simulation

    public SlateData(GameObject obj)
    {
        name = obj.name;
        this.obj = obj;
        SetSphereRadius();
    }
    public SlateData(string name, GameObject obj)
    {
        this.name = name;
        this.obj = obj;
        SetSphereRadius();
    }

    public Vector3 GetSphereCenter()
    {
        if (obj == null) return Vector3.zero;
        return obj.transform.position;
    }
    private void SetSphereRadius()
    {
        if(obj == null) return;
        if(GameManager.Instance == null) return;
        radius = GameManager.Instance.slatePadding * obj.transform.localScale.x;
    }

    public void SimulateCollision(SlateData other)
    {
        if (other == null) return;
        // Get initial position
        Vector3 initPos = GetSphereCenter();

        if (CheckOverlap(other))
        {
            Vector3 force = GetPushDirection(other);
            PushSlate(force / 2f);
            other.PushSlate(-force / 2f);
        }

        // Compare new position with inital and update movement state
        if (initPos == GetSphereCenter())
            movementStallCheck++;
        else
            movementStallCheck = 0;

        // Check if movement is done
        if (movementStallCheck > movementStallThreshold)
            simulateMovement = false;
    }

    public bool CheckOverlap(SlateData other)
    {
        // Check if the distance between the two spheres is less than the main radius + other radius.
        float totalRadius = radius + other.radius;
        float distance = Vector3.Distance(GetSphereCenter(), other.GetSphereCenter());

        Debug.Log("Checking Overlap: " + totalRadius + " | " + distance);

        return distance < totalRadius;
    }

    public Vector3 GetPushDirection(SlateData other)
    {
        return GetSphereCenter() - other.GetSphereCenter();
    }

    public void PushSlate(Vector3 force)
    {
        if (obj == null) {
            Debug.LogError("SlateData - PushSlate -> Object is set to null");
            return;
        }

        obj.transform.position = obj.transform.position + (force * Time.deltaTime);
    }
}
