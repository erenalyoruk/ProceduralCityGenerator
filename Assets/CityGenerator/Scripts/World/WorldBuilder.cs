using UnityEngine;
using UnityEngine.Assertions.Must;

public class WorldBuilder : MonoBehaviour
{
    public static WorldBuilder Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        var wall = WallBuilder.Instance.CreateWall(new Vector3(10f, 3f, 0.3f), 4, true);
        wall.Position = new Vector3(0f, 0f, 0f);

        var wall2 = WallBuilder.Instance.CreateWall(new Vector3(10f, 3f, 0.3f), 2, true);
        wall2.Position = new Vector3(0f, 0f, 10f);

        var wall3 = WallBuilder.Instance.CreateWall(new Vector3(10f, 3f, 0.3f), 6, true);
        wall3.Position = new Vector3(0f, 0f, 20f);

        var wall4 = WallBuilder.Instance.CreateWall(new Vector3(10f, 3f, 0.3f), 1, false);
        wall4.Position = new Vector3(0f, 0f, 30f);

        var wall5 = WallBuilder.Instance.CreateWall(new Vector3(10f, 3f, 0.3f), 3, true);
        wall5.Position = new Vector3(0f, 0f, 40f);
    }
}
