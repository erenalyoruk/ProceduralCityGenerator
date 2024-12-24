using System.Collections.Generic;
using UnityEngine;

public class Floor : Building
{
    public new Vector3 Position
    {
        get => Object.transform.localPosition;
        set => Object.transform.localPosition = value;
    }

    public new Vector3 Size
    {
        get => Object.transform.localScale;
        set => Object.transform.localScale = value;
    }

    public new Quaternion Rotation
    {
        get => Object.transform.localRotation;
        set => Object.transform.localRotation = value;
    }

    public List<Wall> Walls { get; private set; }
    public GameObject GroundObject { get; private set; }

    public int WallCount => Walls.Count;

    public Floor(GameObject gameObject)
        : base(gameObject)
    {
        Walls = new List<Wall>();
    }

    public void AddWall(Wall wall, Vector3 localPosition)
    {
        if (WallCount >= 4)
        {
            Debug.LogWarning("Cannot add more than 4 walls to a floor.");
            return;
        }

        wall.Object.transform.SetParent(Object.transform);
        wall.Position = localPosition;
        Walls.Add(wall);
    }

    public void SetGround(GameObject ground, Vector3 size, float groundThickness = 0.2f)
    {
        if (GroundObject != null)
        {
            Debug.LogWarning("Ground object is already set.");
            return;
        }

        GroundObject = ground;
        GroundObject.transform.SetParent(Object.transform);
        GroundObject.transform.localScale = new Vector3(size.x, size.z, groundThickness);
        GroundObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        GroundObject.transform.localPosition = new Vector3(0, -groundThickness / 2f, 0);
    }

    public new void Destroy()
    {
        base.Destroy();

        foreach (var wall in Walls)
        {
            wall.Destroy();
        }
    }
}
