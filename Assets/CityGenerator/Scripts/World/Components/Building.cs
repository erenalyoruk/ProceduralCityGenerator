using UnityEngine;

public class Building : IBuilding
{
    public GameObject Object { get; set; }

    public Vector3 Position { get; set; }
    public Vector3 Size { get; set; }

    public Quaternion Rotation { get; set; }

    public Building(GameObject gameObject)
    {
        Object = gameObject;
    }

    public void Destroy()
    {
        GameObject.Destroy(Object);
    }
}
