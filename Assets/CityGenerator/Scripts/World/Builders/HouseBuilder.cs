using UnityEngine;

public struct FloorProperties
{
    public WindowCount WindowCount;
    public bool HasDoor;
};

public class HouseBuilder : MonoBehaviour
{
    public static HouseBuilder Instance { get; private set; }

    [SerializeField]
    private Material _roofMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public House BuildHouse(Vector3 size, FloorProperties[] floorProperties)
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

        BuildRoof(house, size, floorProperties.Length);

        return house;
    }

    private void BuildRoof(House house, Vector3 size, int floorCount)
    {
        GameObject roof = new GameObject("Roof");
        roof.transform.parent = house.Object.transform;

        var roofMesh = roof.AddComponent<MeshFilter>();
        roof.AddComponent<MeshRenderer>();

        roofMesh.mesh = CreateSlantedRoof(size);

        var roofRenderer = roof.GetComponent<MeshRenderer>();
        roofRenderer.sharedMaterial = _roofMaterial;

        roof.transform.position = new Vector3(0, floorCount * size.y, 0);
    }

    private Mesh CreateSlantedRoof(Vector3 size)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-size.x / 2, 0, -size.z / 2),
            new Vector3(size.x / 2, 0, -size.z / 2),
            new Vector3(-size.x / 2, 0, size.z / 2),
            new Vector3(size.x / 2, 0, size.z / 2),
            new Vector3(0, size.y / 2, 0),
        };

        int[] triangles = new int[] { 0, 2, 4, 2, 3, 4, 3, 1, 4, 1, 0, 4 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        return mesh;
    }
}
