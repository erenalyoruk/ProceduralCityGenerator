using UnityEngine;

public class PCBuilding
{
    public Vector2Int entranceGrid;
    public GameObject model;
    protected string buildingName;
    protected Texture texture;
    public GameObject tree1,
        tree2,
        tree3;
    protected GameObject privateRegions;
    protected float viewPrivacyRadius = 50;
    protected ProceduralCityGenerator pcg;

    public PCBuilding(string bname)
    {
        buildingName = bname;
        pcg = (ProceduralCityGenerator)
            GameObject.Find("CityBuilder").GetComponent(typeof(ProceduralCityGenerator));
        Debug.Log(pcg.Security);
    }

    public PCBuilding(string bname, Texture t)
    {
        buildingName = bname;
        texture = t;
        pcg = (ProceduralCityGenerator)
            GameObject.Find("CityBuilder").GetComponent(typeof(ProceduralCityGenerator));
        Debug.Log(pcg.Security);
    }

    public PCBuilding(Vector3 position, float size, float height, string buildingName)
    {
        GenerateModel(position, size, height);
        entranceGrid = new Vector2Int();
    }

    private void GenerateModel(Vector3 position, float size, float height)
    {
        model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.position = position;
        model.transform.localScale =
            new Vector3(size, height * 2, size) / ProceduralCityGenerator.ScaleFactor;
        model.transform.Rotate(Vector3.up, Random.value * 90);
    }

    //assumes looking from outside
    protected void AddPrivateRegion(Vector3 ul, Vector3 ll, Vector3 ur, Vector3 lr)
    {
        GameObject pr = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pr.tag = "PrivateRegion";

        pr.transform.SetParent(model.transform);
        pr.transform.localScale = new Vector3(
            (ur + lr - ul - ll).magnitude / 2,
            (ur - lr + ul - ll).magnitude / 2,
            1
        );
        pr.transform.localPosition = (ur + lr + ul + ll) / 4;

        Vector3 upVector = ur - lr + ul - ll;
        upVector.Normalize();
        Vector3 horVector = ur + lr - ul - ll;
        horVector.Normalize();
        Vector3 normal = Vector3.Cross(horVector, upVector);
        Quaternion q = Quaternion.LookRotation(normal, upVector);
        pr.transform.localRotation = q;
        pr.GetComponent<MeshRenderer>().enabled = false;
    }
}
