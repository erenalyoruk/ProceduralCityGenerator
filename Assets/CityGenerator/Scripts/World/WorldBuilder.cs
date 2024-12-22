using UnityEngine;
using UnityEngine.Assertions.Must;

public class WorldBuilder : MonoBehaviour
{
    public WallBuilder WallBuilder { get; private set; }

    public static WorldBuilder Instance { get; private set; }

    private Wall wall;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        WallBuilder = transform.Find("WallBuilder").GetComponent<WallBuilder>();
        if (WallBuilder == null)
        {
            Debug.LogError("WallBuilder component is not found.");
        }
    }

    private void Start()
    {
        wall = WallBuilder.CreateWall(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(10, 3, 0.3f),
            0,
            true
        );

        foreach (var window in wall.Windows)
        {
            // draw debug thing to visualize in pos
            Debug.DrawLine(
                window.Position,
                window.Position + new Vector3(0, window.Size.y, 0),
                Color.red,
                100000
            );

            Debug.DrawLine(
                window.Position - new Vector3(window.Size.x / 2, -window.Size.y / 2, 0),
                window.Position + new Vector3(window.Size.x / 2, window.Size.y / 2, 0),
                Color.red,
                100000
            );
        }

        foreach (var door in wall.Doors)
        {
            Debug.Log($"Door position: {door.Position}");

            Debug.DrawLine(
                door.Position,
                door.Position + new Vector3(0, door.Size.y, 0),
                Color.red,
                100000
            );

            Debug.DrawLine(
                door.Position - new Vector3(door.Size.x / 2, -door.Size.y / 2, 0),
                door.Position + new Vector3(door.Size.x / 2, door.Size.y / 2, 0),
                Color.red,
                100000
            );
        }
    }
}
