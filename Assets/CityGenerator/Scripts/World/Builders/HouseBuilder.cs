using UnityEngine;

public struct FloorProperties
{
    public WindowCount WindowCount;
    public bool HasDoor;
};

public class HouseBuilder : MonoBehaviour
{
    public static HouseBuilder Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void BuildHouse(Vector3 size, FloorProperties[] floorProperties)
    {
        House house = new House(new GameObject("House"));

        int i = 0;
        foreach (var property in floorProperties)
        {
            var floor = FloorBuilder.Instance.BuildFloor(
                size,
                property.WindowCount,
                property.HasDoor
            );

            house.AddFloor(floor, new Vector3(0, size.y * i, 0));

            ++i;
        }
    }
}
