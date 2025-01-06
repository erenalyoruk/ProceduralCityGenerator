using UnityEngine;

public interface IBuilding
{
    GameObject Object { get; set; }

    Vector3 Position { get; set; }
    Vector3 Size { get; set; }

    Quaternion Rotation { get; set; }

    void Destroy();
}
