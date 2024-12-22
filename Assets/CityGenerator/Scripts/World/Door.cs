using UnityEngine;

public class Door
{
    public Vector3 Position { get; private set; }
    public Vector3 WallRelativePosition { get; private set; }

    public Vector2 Size { get; private set; }

    public Quaternion Rotation { get; private set; }

    public Door(Vector3 wallRelativePosiiton, Vector2 size)
    {
        Position = wallRelativePosiiton;
        WallRelativePosition = wallRelativePosiiton;
        Size = size;
    }

    public Door(Vector3 wallRelativePosiiton, Vector2 size, Quaternion rotation)
    {
        Position = wallRelativePosiiton;
        WallRelativePosition = wallRelativePosiiton;
        Size = size;
        Rotation = rotation;
    }

    public void SetPosition(Vector3 position)
    {
        Position = position + WallRelativePosition;
    }

    public void SetRotation(Quaternion rotation)
    {
        Rotation = rotation;
        Position = rotation * Position;
    }

    public void SetSize(Vector2 size)
    {
        Size = size;
    }
}
