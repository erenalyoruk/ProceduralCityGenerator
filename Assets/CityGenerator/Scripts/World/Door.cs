using UnityEngine;

public class Door
{
    /// <summary>
    /// Position of the door that relative to the wall.
    /// </summary>
    public Vector3 Position { get; private set; }
    public Vector2 Size { get; private set; }

    public Door(Vector3 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }
}
