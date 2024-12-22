using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

public class WallBuilder : MonoBehaviour
{
    public static WallBuilder Instance { get; private set; }

    // Door will have cutout that is %20 width and %60 height
    private static readonly Vector2 _doorRatio = new Vector2(0.2f, 0.6f);

    private static readonly Vector2 _maxDoorSize = new Vector2(1f, 2f) * Units.Meters;

    // Window will have cutouts they are %60 width and %40 height
    private static readonly Vector2 _windowRatio = new Vector2(0.6f, 0.4f);

    [SerializeField]
    private GameObject _buildingWallPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
    public Wall CreateWall(
        Vector3 position,
        Quaternion rotation,
        Vector3 size,
        int windowCount = 0,
        bool door = false,
        bool constrainDoorSize = true
    )
    {
        if (_buildingWallPrefab == null)
        {
            Debug.LogError("Building wall prefab is not assigned.");
            return null;
        }

        if (size.x <= 0 || size.y <= 0 || size.z <= 0)
        {
            Debug.LogError("Invalid size or window count.");
            return null;
        }

        if (door && windowCount % 2 != 0)
        {
            Debug.LogError("Window count must be even for doors. Making it even to workaround.");
            windowCount++;
        }

        float wallWidth = size.x;
        float wallHeight = size.y;
        float wallThickness = size.z;

        float remainingWidth = wallWidth;

        var windowSize = Vector2.zero;
        if (windowCount > 0)
        {
            windowSize = new Vector2(
                size.x * _windowRatio.x / windowCount,
                size.y * _windowRatio.y
            );
        }

        var doorSize = new Vector2(size.x * _doorRatio.x, size.y * _doorRatio.y);

        if (constrainDoorSize)
        {
            doorSize.x = Mathf.Min(_maxDoorSize.x, doorSize.x);
            doorSize.y = Mathf.Min(Mathf.Min(_maxDoorSize.y, wallHeight), doorSize.y);
        }

        float totalWindowWidth = windowCount * windowSize.x;

        float gapWidth = 0f;
        if (door)
        {
            gapWidth = (remainingWidth - totalWindowWidth - doorSize.x) / (windowCount + 2);
        }
        else
        {
            gapWidth = (remainingWidth - totalWindowWidth) / (windowCount + 1);
        }

        var parentWall = new GameObject("Wall");
        parentWall.transform.position = position;
        parentWall.transform.rotation = rotation;

        float currentX = -wallWidth / 2;
        float topBottomHeight = (1 - _windowRatio.y) / 2 * wallHeight;
        float topPosition = wallHeight - topBottomHeight / 2;

        int segmentCount = (windowCount * 2) + (door ? 1 : 0) * 2 + 1;

        List<GameObject> wallObjects = new List<GameObject>();
        List<Window> windows = new List<Window>();
        List<Door> doors = new List<Door>();

        for (int i = 0; i < segmentCount; i++)
        {
            if (i % 2 == 0) // Wall segment
            {
                var createdObject = CreateWallSegment(
                    parentWall,
                    ref currentX,
                    new Vector3(gapWidth, wallHeight, wallThickness)
                );

                wallObjects.Add(createdObject);
            }
            else if (door && i == segmentCount / 2) // Door segment
            {
                var createdObject = CreateDoorSegment(
                    parentWall,
                    ref currentX,
                    new Vector3(doorSize.x, wallHeight - doorSize.y, wallThickness),
                    (wallHeight + doorSize.y) / 2
                );

                wallObjects.Add(createdObject);
                doors.Add(new Door(new Vector3(currentX - doorSize.x / 2, 0, 0), doorSize));
                door = false;
            }
            else if (windowCount > 0 && i % 2 == 1) // Window segment (top and bottom)
            {
                var createdObjects = CreateWindowSegment(
                    parentWall,
                    ref currentX,
                    new Vector3(windowSize.x, topBottomHeight, wallThickness),
                    topPosition
                );

                wallObjects.AddRange(createdObjects);

                windows.Add(
                    new Window(
                        new Vector3(currentX - windowSize.x / 2, topBottomHeight, 0),
                        windowSize
                    )
                );

                --windowCount;
            }
        }

        MergeWalls(parentWall, wallObjects.ToArray());

        return new Wall(parentWall, windows, doors);
    }

    /// <summary>
    /// Creates a wall segment.
    /// </summary>
    /// <param name="parent">Parent object of the wall segment.</param>
    /// <param name="currentX">Current x position of the wall segment.</param>
    /// <param name="size">Size of the wall segment.</param>
    /// <returns>Wall segment object.</returns>
    private GameObject CreateWallSegment(GameObject parent, ref float currentX, Vector3 size)
    {
        var segment = Instantiate(_buildingWallPrefab, parent.transform);
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
        GameObject parent,
        ref float currentX,
        Vector3 size,
        float topPosition
    )
    {
        var topSegment = Instantiate(_buildingWallPrefab, parent.transform);
        topSegment.transform.localPosition = new Vector3(currentX + size.x / 2, topPosition, 0f);
        topSegment.transform.localScale = size;

        var bottomSegment = Instantiate(_buildingWallPrefab, parent.transform);
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
        GameObject parent,
        ref float currentX,
        Vector3 size,
        float topPosition
    )
    {
        var doorTop = Instantiate(_buildingWallPrefab, parent.transform);
        doorTop.transform.localPosition = new Vector3(currentX + size.x / 2, topPosition, 0f);
        doorTop.transform.localScale = size;

        currentX += size.x;

        return doorTop;
    }

    /// <summary>
    /// Merges wall segments into a single wall.
    /// </summary>
    /// <param name="parentWall">Parent wall object.</param>
    /// <param name="walls">Wall segments to merge.</param>
    /// <returns>Merged wall object.</returns>
    private void MergeWalls(GameObject parentWall, GameObject[] walls)
    {
        MeshFilter parentMeshFilter;
        if (parentWall.GetComponent<MeshFilter>() == null)
        {
            parentMeshFilter = parentWall.AddComponent<MeshFilter>();
        }
        else
        {
            parentMeshFilter = parentWall.GetComponent<MeshFilter>();
        }

        MeshRenderer parentMeshRenderer;
        if (parentWall.GetComponent<MeshRenderer>() == null)
        {
            parentMeshRenderer = parentWall.AddComponent<MeshRenderer>();
        }
        else
        {
            parentMeshRenderer = parentWall.GetComponent<MeshRenderer>();
        }

        MeshFilter[] meshFilters = new MeshFilter[walls.Length];
        Mesh combinedMesh = new Mesh();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            meshFilters[i] = walls[i].GetComponent<MeshFilter>();
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        combinedMesh.CombineMeshes(combine, true, true);

        parentMeshFilter.sharedMesh = combinedMesh;
        parentMeshRenderer.sharedMaterial = _buildingWallPrefab
            .GetComponent<MeshRenderer>()
            .sharedMaterial;

        foreach (GameObject wall in walls)
        {
            Destroy(wall);
        }
    }
}
