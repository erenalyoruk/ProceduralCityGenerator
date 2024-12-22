using System.Collections.Generic;
using UnityEngine;

public class Wall : Building
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

    public List<Window> Windows { get; private set; }
    public List<Door> Doors { get; private set; }

    public bool HasDoor => Doors.Count > 0;

    public int WindowCount => Windows.Count;

    public Wall(GameObject gameObject)
        : base(gameObject)
    {
        Windows = new List<Window>();
        Doors = new List<Door>();
    }

    public void AddWindow(Window window, Vector3 localPosition)
    {
        window.Object.transform.SetParent(Object.transform);
        window.Position = localPosition;

        Windows.Add(window);
    }

    public void AddDoor(Door door, Vector3 localPosition)
    {
        door.Object.transform.SetParent(Object.transform);
        door.Position = localPosition;

        Doors.Add(door);
    }

    public new void Destroy()
    {
        base.Destroy();

        foreach (var window in Windows)
        {
            window.Destroy();
        }

        foreach (var door in Doors)
        {
            door.Destroy();
        }
    }
}
