using UnityEngine;
using System.Collections;

public class PCHamam : PCBuilding
{

    public PCHamam(Vector3 position, float size, float height, string buildingName, Texture t) : base(buildingName)
    {
        texture = t;
        GenerateModel(position, size, height);

    }

    private void GenerateModel(Vector3 position, float size, float height)
    {
        model = new GameObject("hamam");
        model.transform.position = position;
        model.transform.Rotate(Vector3.up, UnityEngine.Random.value * 90);
        GameObject hamamWalls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hamamWalls.transform.SetParent(model.transform, false);
        hamamWalls.transform.localScale = (new Vector3(size, height * 2 - 4, size)) / ProceduralCityGenerator.ScaleFactor;
        GameObject hamamWalls2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hamamWalls2.transform.SetParent(model.transform, false);
        hamamWalls2.transform.localScale = (new Vector3(size * 0.5f, height * 2, size * 0.5f)) / ProceduralCityGenerator.ScaleFactor;
        hamamWalls2.transform.localPosition = (new Vector3(-size * 0.25f, 0, size * 0.25f)) / ProceduralCityGenerator.ScaleFactor;

        Renderer houseWallRenderer = hamamWalls.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2(size / 4f, height / 4));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer = hamamWalls2.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2(size / 4f, height / 4));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
    }
}
