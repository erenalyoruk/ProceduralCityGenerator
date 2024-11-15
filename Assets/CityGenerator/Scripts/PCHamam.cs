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
        /*GameObject hamamWalls2b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hamamWalls2b.transform.SetParent(model.transform, false);
        hamamWalls2b.transform.localScale = (new Vector3(size * 0.45f, height * 2+1, size * 0.45f)) / ProceduralCityGenerator.ScaleFactor;
        hamamWalls2b.transform.localPosition = (new Vector3(-size * 0.25f, 0, size * 0.25f)) / ProceduralCityGenerator.ScaleFactor;
        GameObject hamamWalls3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hamamWalls3.transform.SetParent(model.transform, false);
        hamamWalls3.transform.localScale = (new Vector3(size * 0.45f, height * 2 - 3, size * 0.45f)) / ProceduralCityGenerator.ScaleFactor;
        hamamWalls3.transform.localPosition = (new Vector3(size * 0.25f, 0, size * 0.25f)) / ProceduralCityGenerator.ScaleFactor;
        GameObject hamamKubbesi = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hamamKubbesi.transform.SetParent(model.transform, false);
        hamamKubbesi.transform.localScale = (new Vector3(size, size, size)) * 0.5f / ProceduralCityGenerator.ScaleFactor;
        hamamKubbesi.transform.localPosition = new Vector3(-size * 0.25f, height-2, size * 0.25f) / ProceduralCityGenerator.ScaleFactor;
        GameObject hamamKubbesi2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hamamKubbesi2.transform.SetParent(model.transform, false);
        hamamKubbesi2.transform.localScale = (new Vector3(size, size, size)) * 0.5f / ProceduralCityGenerator.ScaleFactor;
        hamamKubbesi2.transform.localPosition = new Vector3(size * 0.25f, height - 4, size * 0.25f) / ProceduralCityGenerator.ScaleFactor;*/

        Renderer houseWallRenderer = hamamWalls.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2(size / 4f, height / 4));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer = hamamWalls2.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2(size / 4f, height / 4));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        /*houseWallRenderer = hamamWalls2b.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2(size / 4f, height / 4));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer = hamamWalls3.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2(size / 4f, height / 4));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer = hamamKubbesi.GetComponent<Renderer>();
        houseWallRenderer.material.SetColor("_Color", Color.gray);
        houseWallRenderer = hamamKubbesi2.GetComponent<Renderer>();
        houseWallRenderer.material.SetColor("_Color", Color.gray);*/
    }
}
