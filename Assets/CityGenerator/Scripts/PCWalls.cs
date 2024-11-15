using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

public class PCWalls : PCBuilding
{
    Vector2Int CityBoundaryMin, CityBoundaryMax;
    private float colorDarkening = 0.6f;
    private float houseWallHeight = 8f / ProceduralCityGenerator.ScaleFactor;
    private float houseWallThickness = 2f / ProceduralCityGenerator.ScaleFactor;
    private float doorWidth = 4f / ProceduralCityGenerator.ScaleFactor;
    private float windowWidth = 0.5f / ProceduralCityGenerator.ScaleFactor;

    public PCWalls(Vector2Int cmin, Vector2Int cmax, Texture t) : base("City Walls")
    {
        CityBoundaryMin = cmin;
        CityBoundaryMax = cmax;
        texture = t;
        GenerateModel();

    }

    private void GenerateModel()
    {
        ProceduralCityGenerator pcg = (ProceduralCityGenerator)GameObject.Find("CityBuilder").GetComponent(typeof(ProceduralCityGenerator));
        List<Vector3> points = new List<Vector3>();
        //check 8 way neighborhood
        for (int i = 0; i < pcg.cw; i++)
        {
            for (int j = 0; j < pcg.ch; j++)
            {
                if (pcg.CityOccupation[i, j] == ProceduralCityGenerator.Occupation.Occupied)
                {
                    //find the cost
                    Vector3 from1 = new Vector3(i + CityBoundaryMin.x - 1f, 0f, j + CityBoundaryMin.y - 1f);
                    Vector3 from2 = new Vector3(i + CityBoundaryMin.x - 1f, 0f, j + CityBoundaryMin.y + 1f);
                    Vector3 from3 = new Vector3(i + CityBoundaryMin.x + 1f, 0f, j + CityBoundaryMin.y - 1f);
                    Vector3 from4 = new Vector3(i + CityBoundaryMin.x + 1f, 0f, j + CityBoundaryMin.y + 1f);
                    from1[1] = pcg.CityTerrain.SampleHeight(from1);
                    from2[1] = pcg.CityTerrain.SampleHeight(from2);
                    from3[1] = pcg.CityTerrain.SampleHeight(from3);
                    from4[1] = pcg.CityTerrain.SampleHeight(from4);
                    points.Add(from1);
                    points.Add(from2);
                    points.Add(from3);
                    points.Add(from4);
                }
            }
        }

        if (points.Count > 2)
        {
            //sort points by z coordinates
            points.Sort((a, b) => a.z.CompareTo(b.z));

            List<Vector3> wallPoints = new List<Vector3>();

            //add first two vertices
            wallPoints.Add(points[0]);
            wallPoints.Add(points[1]);

            //scan from bottom to top (z) to build right walls
            for (int i = 2; i < points.Count; i++)
            {
                //check new point left or right of the previous
                Vector3 newSegment = points[i] - wallPoints[wallPoints.Count - 1];
                Vector3 lastSegment = wallPoints[wallPoints.Count - 1] - wallPoints[wallPoints.Count - 2];

                //roll back until a left turn is found
                while (wallPoints.Count > 2 && newSegment.x * lastSegment.z - newSegment.z * lastSegment.x >= 0)
                {
                    wallPoints.RemoveAt(wallPoints.Count - 1);
                    newSegment = points[i] - wallPoints[wallPoints.Count - 1];
                    lastSegment = wallPoints[wallPoints.Count - 1] - wallPoints[wallPoints.Count - 2];
                }
                //add new segment
                wallPoints.Add(points[i]);


            }

            //scan from top to bottom (z) to build left walls
            for (int i = points.Count - 2; i >= 0; i--)
            {
                //check new point left or right of the previous
                Vector3 newSegment = points[i] - wallPoints[wallPoints.Count - 1];
                Vector3 lastSegment = wallPoints[wallPoints.Count - 1] - wallPoints[wallPoints.Count - 2];

                //roll back until a left turn is found
                while (wallPoints.Count > 1 && newSegment.x * lastSegment.z - newSegment.z * lastSegment.x >= 0)
                {
                    wallPoints.RemoveAt(wallPoints.Count - 1);
                    newSegment = points[i] - wallPoints[wallPoints.Count - 1];
                    lastSegment = wallPoints[wallPoints.Count - 1] - wallPoints[wallPoints.Count - 2];
                }

                wallPoints.Add(points[i]);
            }

            model = new GameObject("CityWalls");
            for (int i = 0; i < wallPoints.Count - 1; i++)
            {
                HouseWall(wallPoints[i], wallPoints[i + 1], false, false);
            }
        }
    }


    private void HouseWall(Vector3 from, Vector3 to, bool door = false, bool windows = false)
    {
        Vector3 dir = to - from;

        if (Mathf.Abs(dir[1]) > houseWallHeight / 4)
        {
            //divide 2 three pieces
            HouseWall(from, from + dir / 3, false, windows);
            HouseWall(from + dir / 3, from + 2 * dir / 3, door, windows);
            HouseWall(from + 2 * dir / 3, to, false, windows);
            return;
        }

        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        if (!door && !windows)
        {
            GameObject houseWall = GameObject.CreatePrimitive(PrimitiveType.Cube);

            //adjustTexture
            Renderer houseWallRenderer = houseWall.GetComponent<Renderer>();

            houseWallRenderer.material.SetTexture("_MainTex", texture);
            houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 1));
            houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

            houseWall.transform.SetParent(model.transform, false);
            houseWall.transform.localScale = (new Vector3((to - from).magnitude, houseWallHeight, houseWallThickness));
            houseWall.transform.localPosition = (from + to + towardsIn * houseWallThickness + Vector3.up * houseWallHeight / 2f) / 2f;
            houseWall.transform.Rotate(Vector3.up, Vector3.SignedAngle(Vector3.right, dir, Vector3.up));
        }
        else if (door)
        {
            if ((to - from).magnitude <= doorWidth)
            {
                HouseWall(from, to, false, windows);
                Debug.Log("Not enough room for a door!");
            }
            else
            {
                //make a centered door
                Door((from + to - dir * doorWidth) / 2, (from + to + dir * doorWidth) / 2);
                HouseWall(from, (from + to - dir * doorWidth) / 2, false, windows);
                HouseWall((from + to + dir * doorWidth) / 2, to, false, windows);
            }
        }
        else if (windows)
        {
            float sideLength = (to - from).magnitude;
            // how many windows fits
            int windowsCount = (int)((sideLength - houseWallThickness * 2) / (windowWidth * ProceduralCityGenerator.GoldenRatio));
            //evenly distribute windows like -w-w-w-
            float intervalLength = (sideLength - windowsCount * windowWidth) / (windowsCount + 1f);
            HouseWall(from, from + dir * intervalLength);
            Vector3 windowFrom = from + dir * intervalLength;
            for (int i = 0; i < windowsCount; i++)
            {
                Window(windowFrom, windowFrom + dir * windowWidth);
                windowFrom += dir * (windowWidth + intervalLength);
                HouseWall(windowFrom - dir * intervalLength, windowFrom);
            }

        }
    }

    private void Window(Vector3 from, Vector3 to)
    {
        GameObject underWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        underWindow.transform.SetParent(model.transform, false);
        underWindow.transform.localScale = (new Vector3((to - from).magnitude, houseWallHeight / 3, houseWallThickness));

        //adjustTexture
        Renderer houseWallRenderer = underWindow.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 0.3f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);



        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);
        underWindow.transform.localPosition = (from + to + towardsIn * houseWallThickness + Vector3.up * houseWallHeight / 3f) / 2f;
        underWindow.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));


        GameObject overWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overWindow.transform.SetParent(model.transform, false);
        overWindow.transform.localScale = (new Vector3((to - from).magnitude, houseWallHeight / 6, houseWallThickness));

        //adjustTexture
        houseWallRenderer = overWindow.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 0.3f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        overWindow.transform.localPosition = (from + to + towardsIn * houseWallThickness + 22f * Vector3.up * houseWallHeight / 12f) / 2f;
        overWindow.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));

        //make inner window
        Vector3 WindowCornersUpperLeft = from;
        Vector3 WindowCornersLowerLeft = from;
        Vector3 WindowCornersUpperRight = to;
        Vector3 WindowCornersLowerRight = to;


        WindowCornersUpperLeft.y += 5f * houseWallHeight / 6;
        WindowCornersLowerLeft.y += houseWallHeight / 3;
        WindowCornersUpperRight.y += 5f * houseWallHeight / 6;
        WindowCornersLowerRight.y += houseWallHeight / 3;

        AddPrivateRegion(WindowCornersUpperLeft, WindowCornersLowerLeft, WindowCornersUpperRight, WindowCornersLowerRight);

        MakeElongatedCube(WindowCornersLowerLeft, WindowCornersLowerRight, 0.15f);
        MakeElongatedCube(WindowCornersUpperLeft, WindowCornersUpperRight, 0.15f);
        MakeElongatedCube(WindowCornersUpperLeft, WindowCornersLowerLeft, 0.15f);
        MakeElongatedCube(WindowCornersUpperRight, WindowCornersLowerRight, 0.15f);
        MakeElongatedCube((WindowCornersUpperRight + WindowCornersUpperLeft) / 2, (WindowCornersLowerRight + WindowCornersLowerLeft) / 2, 0.05f);
        MakeElongatedCube(WindowCornersLowerLeft / 3f + WindowCornersUpperLeft * 2f / 3, WindowCornersLowerRight / 3f + WindowCornersUpperRight * 2f / 3f, 0.05f);
        MakeElongatedCube(WindowCornersLowerLeft * 2f / 3f + WindowCornersUpperLeft / 3f, WindowCornersLowerRight * 2f / 3f + WindowCornersUpperRight / 3f, 0.05f);
    }


    private void MakeElongatedCube(Vector3 from, Vector3 to, float width)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //adjustTexture
        Renderer houseWallRenderer = cube.GetComponent<Renderer>();
        houseWallRenderer.material.SetColor("_Color", Color.white * 0.8f * colorDarkening);

        cube.transform.SetParent(model.transform, false);
        cube.transform.localScale = (new Vector3((to - from).magnitude + width, width, width));
        cube.transform.localPosition = (from + to) / 2f;
        Vector3 dir = to - from;
        dir.Normalize();
        cube.transform.Rotate(Vector3.Cross(Vector3.right, dir), Vector3.SignedAngle(Vector3.right, dir, Vector3.Cross(Vector3.right, dir)));

    }

    private void Door(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        GameObject overDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overDoor.transform.SetParent(model.transform, false);
        overDoor.transform.localScale = (new Vector3((to - from).magnitude, houseWallHeight / 6, houseWallThickness));

        //adjustTexture
        Renderer houseWallRenderer = overDoor.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 0.3f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        overDoor.transform.localPosition = (from + to + towardsIn * houseWallThickness + 22f * Vector3.up * houseWallHeight / 12f) / 2f;
        overDoor.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));

        //draw the door
        GameObject doorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObject.transform.SetParent(model.transform, false);
        doorObject.transform.localScale = (new Vector3((to - from).magnitude, 5f * houseWallHeight / 6, houseWallThickness * 0.25f));
        doorObject.transform.localPosition = (from + to + towardsIn * houseWallThickness * 0.5f + 10f * Vector3.up * houseWallHeight / 12f) / 2f;
        doorObject.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));
        houseWallRenderer = doorObject.GetComponent<Renderer>();
        houseWallRenderer.material.SetColor("_Color", new Color(0.3f, 0.22f, 0.05f) * UnityEngine.Random.value);
        MakeElongatedCube(from + towardsIn * houseWallThickness * 0.25f, to + towardsIn * houseWallThickness * 0.25f, houseWallThickness * 0.4f);
        MakeElongatedCube(from + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * houseWallThickness * 0.25f, to + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * houseWallThickness * 0.25f, houseWallThickness * 0.4f);
        MakeElongatedCube(from + towardsIn * houseWallThickness * 0.25f, from + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * houseWallThickness * 0.25f, houseWallThickness * 0.4f);
        MakeElongatedCube(to + towardsIn * houseWallThickness * 0.25f, to + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * houseWallThickness * 0.25f, houseWallThickness * 0.4f);
        //put a few steps in front of the door
    }
}
