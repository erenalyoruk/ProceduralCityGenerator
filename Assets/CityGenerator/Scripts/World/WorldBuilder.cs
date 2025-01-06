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
        var house = HouseBuilder.Instance.BuildHouse(
            new Vector3(4, 2.5f, 4),
            new FloorProperties[]
            {
                new FloorProperties { WindowCount = new WindowCount(2, 3, 2, 2), HasDoor = true },
                new FloorProperties { WindowCount = new WindowCount(1, 1, 1, 1), HasDoor = false },
                new FloorProperties { WindowCount = new WindowCount(3, 3, 2, 2), HasDoor = false },
            }
        );
    }
}
