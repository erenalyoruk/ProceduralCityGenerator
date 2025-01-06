using System.Collections.Generic;
using UnityEngine;

public class WallBuilder : MonoBehaviour
{
    public static WallBuilder Instance { get; private set; }

    private static readonly Vector2 _doorRatio = new Vector2(0.3f, 0.7f);
    private static readonly Vector2 _maxDoorSize = new Vector2(1f, 2.4f);

    private static readonly Vector2 _windowRatio = new Vector2(0.6f, 0.4f);
    private static readonly Vector2 _maxWindowSize = new Vector2(1f, 1f);

    [SerializeField]
    private GameObject _wallPrefab;

    [SerializeField]
    private GameObject _windowPrefab;

    [SerializeField]
    private GameObject _doorPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_wallPrefab == null)
        {
            Debug.LogError("Building wall prefab is not assigned.");
        }

        if (_windowPrefab == null)
        {
            Debug.LogError("Window prefab is not assigned.");
        }

        if (_doorPrefab == null)
        {
            Debug.LogError("Door prefab is not assigned.");
        }
    }

    /// <summary>
    /// Creates a wall with given parameters.
    /// </summary>
    /// <param name="position">Position of the wall.</param>
    /// <param name="rotation">Rotation of the wall.</param>
    /// <param name="size">Size of the wall.</param>
    /// <param name="windowCount">Number of windows on the wall.</param>
    /// <param name="door">Whether the wall has a door.</param>
    /// <param name="constrainDoorSize">Whether to constrain the door size.</param>
    /// <returns>Wall object.</returns>
    public Wall BuildWall(
        Vector3 size,
        int windowCount = 0,
        bool addDoor = false,
        bool constrainDoorSize = true
    )
    {
        if (_wallPrefab == null)
        {
            Debug.LogError("Building wall prefab is not assigned.");
            return null;
        }

        if (windowCount != 0 && _windowPrefab == null)
        {
            Debug.LogError("Window prefab is not assigned.");
            return null;
        }

        if (addDoor && _doorPrefab == null)
        {
            Debug.LogError("Door prefab is not assigned.");
            return null;
        }

        if (size.x <= 0 || size.y <= 0 || size.z <= 0)
        {
            Debug.LogError("Invalid size or window count.");
            return null;
        }

        if (addDoor && windowCount % 2 != 0)
        {
            Debug.LogWarning("Window count must be even for doors. Making it even to workaround.");
            windowCount++;
        }

        float wallWidth = size.x;
        float wallHeight = size.y;
        float wallThickness = size.z;

        float remainingWidth = wallWidth;

        var windowSize = Vector3.zero;
        if (windowCount > 0)
        {
            windowSize = new Vector3(
                size.x * _windowRatio.x / windowCount,
                size.y * _windowRatio.y,
                wallThickness / 2f
            );
        }

        windowSize = new Vector3(
            Mathf.Min(_maxWindowSize.x, windowSize.x),
            Mathf.Min(_maxWindowSize.y, windowSize.y),
            windowSize.z
        );

        var doorSize = new Vector3(
            size.x * _doorRatio.x,
            size.y * _doorRatio.y,
            wallThickness / 2f
        );

        if (constrainDoorSize)
        {
            doorSize.x = Mathf.Min(_maxDoorSize.x, doorSize.x);
            doorSize.y = Mathf.Min(Mathf.Min(_maxDoorSize.y, wallHeight), doorSize.y);
        }

        float totalWindowWidth = windowCount * windowSize.x;

        float gapWidth;
        if (addDoor)
        {
            gapWidth = (remainingWidth - totalWindowWidth - doorSize.x) / (windowCount + 2);
        }
        else
        {
            gapWidth = (remainingWidth - totalWindowWidth) / (windowCount + 1);
        }

        Wall wall = new Wall(new GameObject("Wall"));

        float currentX = -wallWidth / 2;
        float topBottomHeight = (1 - _windowRatio.y) / 2 * wallHeight;
        float topPosition = wallHeight - topBottomHeight / 2;

        int segmentCount = (windowCount * 2) + (addDoor ? 1 : 0) * 2 + 1;

        List<GameObject> segments = new List<GameObject>();

        for (int i = 0; i < segmentCount; i++)
        {
            if (i % 2 == 0) // Wall segment
            {
                segments.Add(
                    CreateWallSegment(
                        wall,
                        ref currentX,
                        new Vector3(gapWidth, wallHeight, wallThickness)
                    )
                );
            }
            else if (addDoor && i == segmentCount / 2) // Door segment
            {
                segments.Add(
                    CreateDoorSegment(
                        wall,
                        ref currentX,
                        new Vector3(doorSize.x, wallHeight - doorSize.y, wallThickness),
                        (wallHeight + doorSize.y) / 2
                    )
                );

                var doorObject = Instantiate(_doorPrefab, wall.Object.transform);
                var door = new Door(doorObject);
                door.Size = doorSize;
                wall.AddDoor(door, new Vector3(currentX - doorSize.x / 2, doorSize.y / 2, 0));

                addDoor = false;
            }
            else if (windowCount > 0 && i % 2 == 1) // Window segment (top and bottom)
            {
                segments.AddRange(
                    CreateWindowSegment(
                        wall,
                        ref currentX,
                        new Vector3(windowSize.x, topBottomHeight, wallThickness),
                        topPosition
                    )
                );

                var windowObject = Instantiate(_windowPrefab);
                var window = new Window(windowObject);
                window.Size = windowSize;
                wall.AddWindow(window, new Vector3(currentX - windowSize.x / 2, wallHeight / 2, 0));

                --windowCount;
            }
        }

        GameObject mergedWall = MeshUtils.MergeMeshes(segments.ToArray());
        mergedWall.transform.SetParent(wall.Object.transform);

        foreach (var segment in segments)
        {
            Destroy(segment);
        }

        return wall;
    }

    /// <summary>
    /// Creates a wall segment.
    /// </summary>
    /// <param name="parent">Parent object of the wall segment.</param>
    /// <param name="currentX">Current x position of the wall segment.</param>
    /// <param name="size">Size of the wall segment.</param>
    /// <returns>Wall segment object.</returns>
    private GameObject CreateWallSegment(Wall wall, ref float currentX, Vector3 size)
    {
        var segment = Instantiate(_wallPrefab, wall.Object.transform);
        segment.transform.localPosition = new Vector3(currentX + size.x / 2, size.y / 2, 0f);
        segment.transform.localScale = size;

        currentX += size.x;

        return segment;
    }

    /// <summary>
    /// Creates a window segment.
    /// </summary>
    /// <param name="parent">Parent object of the window segment.</param>
    /// <param name="currentX">Current x position of the window segment.</param>
    /// <param name="size">Size of the window segment.</param>
    /// <param name="topPosition">Top position of the window segment.</param>
    /// <returns>Window segment objects.</returns>
    private GameObject[] CreateWindowSegment(
        Wall wall,
        ref float currentX,
        Vector3 size,
        float topPosition
    )
    {
        var topSegment = Instantiate(_wallPrefab, wall.Object.transform);
        topSegment.transform.localPosition = new Vector3(currentX + size.x / 2, topPosition, 0f);
        topSegment.transform.localScale = size;

        var bottomSegment = Instantiate(_wallPrefab, wall.Object.transform);
        bottomSegment.transform.localPosition = new Vector3(currentX + size.x / 2, size.y / 2, 0f);
        bottomSegment.transform.localScale = size;

        currentX += size.x;

        return new GameObject[] { topSegment, bottomSegment };
    }

    /// <summary>
    /// Creates a door segment.
    /// </summary>
    /// <param name="parent">Parent object of the door segment.</param>
    /// <param name="currentX">Current x position of the door segment.</param>
    /// <param name="size">Size of the door segment.</param>
    /// <param name="topPosition">Top position of the door segment.</param>
    /// <returns>Door segment object.</returns>
    private GameObject CreateDoorSegment(
        Wall wall,
        ref float currentX,
        Vector3 size,
        float topPosition
    )
    {
        var doorTop = Instantiate(_wallPrefab, wall.Object.transform);
        doorTop.transform.localPosition = new Vector3(currentX + size.x / 2, topPosition, 0f);
        doorTop.transform.localScale = size;

        currentX += size.x;

        return doorTop;
    }
}
