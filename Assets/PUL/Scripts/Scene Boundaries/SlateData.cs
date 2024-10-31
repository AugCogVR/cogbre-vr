using PUL;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class SlateData
{
    public string name;
    public GameObject obj;
    public float radius = 1.0f;

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
}
