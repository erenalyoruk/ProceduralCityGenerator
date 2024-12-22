using System.Collections.Generic;
using UnityEngine;

public class Wall
{
    public GameObject WallObject { get; private set; }

    public Vector3 Position => WallObject.transform.position;

    public Vector3 Size => WallObject.transform.localScale;

    public List<Window> Windows { get; private set; }
    public List<Door> Doors { get; private set; }

    public bool HasDoor => Doors.Count > 0;
    public int WindowCount => Windows.Count;

    public Wall(GameObject wallObject, List<Window> windows, List<Door> doors)
    {
        WallObject = wallObject;
        Windows = windows;
        Doors = doors;
    }

    public void Destroy()
    {
        if (WallObject == null)
        {
            return;
        }

        Object.Destroy(WallObject);
    }

    public void SetMaterial(Material material)
    {
        if (WallObject.TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer.material = material;
        }
    }

    public void SetPosition(Vector3 position)
    {
        WallObject.transform.position = position;
    }

    public void Rotate(Quaternion rotation)
    {
        WallObject.transform.rotation = rotation;
    }

    public void SetSize(Vector3 size)
    {
        WallObject.transform.localScale = size;
    }
}
