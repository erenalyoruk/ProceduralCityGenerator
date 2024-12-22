using UnityEngine;

public class Door : Building
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

    public Door(GameObject gameObject)
        : base(gameObject) { }
}
