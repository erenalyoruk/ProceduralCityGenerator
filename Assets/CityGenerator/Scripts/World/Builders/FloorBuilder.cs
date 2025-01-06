using UnityEngine;

public struct WindowCount
{
    public int Front;
    public int Back;
    public int Left;
    public int Right;

    public WindowCount(int front, int back, int left, int right)
    {
        Front = front;
        Back = back;
        Left = left;
        Right = right;
    }
}

public class FloorBuilder : MonoBehaviour
{
    public static FloorBuilder Instance { get; private set; }

    [SerializeField]
    private GameObject _floorPrefab;

    [SerializeField]
    private float _wallThickness = 0.3f;

    [SerializeField]
    private float _groundThickness = 0.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_floorPrefab == null)
        {
            Debug.LogError("Floor prefab is not set in the FloorBuilder!");
        }
    }

    public Floor BuildFloor(Vector3 size, WindowCount windowCount, bool hasDoor = false)
    {
        var floor = new Floor(new GameObject("Floor"));

        floor.SetGround(Instantiate(_floorPrefab), size, _groundThickness);

        Vector3 wallSize = new Vector3(size.x, size.y, _wallThickness);

        // Front wall
        Wall frontWall = WallBuilder.Instance.BuildWall(wallSize, windowCount.Front, hasDoor);
        floor.AddWall(frontWall, new Vector3(0, 0, size.z / 2 - _wallThickness / 2));

        // Back wall
        var backWallSize = new Vector3(size.x, size.y, _wallThickness);
        Wall backWall = WallBuilder.Instance.BuildWall(wallSize, windowCount.Back);
        floor.AddWall(backWall, new Vector3(0, 0, -size.z / 2 + _wallThickness / 2));

        // Left wall
        var leftWallSize = new Vector3(size.z, size.y, _wallThickness);
        Wall leftWall = WallBuilder.Instance.BuildWall(wallSize, windowCount.Left);
        leftWall.Object.transform.rotation = Quaternion.Euler(0, 90, 0);
        floor.AddWall(leftWall, new Vector3(-size.x / 2 + _wallThickness / 2, 0, 0));

        // Right wall
        var rightWallSize = new Vector3(size.z, size.y, _wallThickness);
        Wall rightWall = WallBuilder.Instance.BuildWall(wallSize, windowCount.Right);
        rightWall.Object.transform.rotation = Quaternion.Euler(0, 90, 0);
        floor.AddWall(rightWall, new Vector3(size.x / 2 - _wallThickness / 2, 0, 0));

        return floor;
    }
}
