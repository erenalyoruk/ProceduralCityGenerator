using System.Collections.Generic;
using UnityEngine;

public class Wall
{
    public GameObject WallObject { get; private set; }

    public Vector3 Position => WallObject.transform.position;

    public Vector3 Size => WallObject.transform.localScale;

    public Quaternion Rotation => WallObject.transform.rotation;

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

    /// <summary>
    /// Destroy the wall object.
    /// </summary>
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

        foreach (var window in Windows)
        {
            window.SetPosition(position);
        }

        foreach (var door in Doors)
        {
            door.SetPosition(position);
        }
    }

    public void SetRotation(Quaternion rotation)
    {
        WallObject.transform.rotation = rotation;

        foreach (var window in Windows)
        {
            window.SetRotation(rotation);
        }

        foreach (var door in Doors)
        {
            door.SetRotation(rotation);
        }
    }

    public void SetSize(Vector3 size)
    {
        WallObject.transform.localScale = size;
    }
}
