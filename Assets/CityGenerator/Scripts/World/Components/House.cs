using System.Collections.Generic;
using UnityEngine;

public class House : Building
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

    List<Floor> Floors { get; set; }

    public int FloorCount => Floors.Count;

    public House(GameObject gameObject)
        : base(gameObject) { }

    public void AddFloor(Floor floor, Vector3 localPosition)
    {
        floor.Object.transform.SetParent(Object.transform);
        floor.Position = localPosition;
    }

    public new void Destroy()
    {
        base.Destroy();

        foreach (var floor in Floors)
        {
            floor.Destroy();
        }
    }
}
