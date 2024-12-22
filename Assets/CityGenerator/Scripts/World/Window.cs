using UnityEngine;

public class Window
{
    public Vector3 Position { get; private set; }
    public Vector2 Size { get; private set; }

    public Window(Vector3 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }
}
