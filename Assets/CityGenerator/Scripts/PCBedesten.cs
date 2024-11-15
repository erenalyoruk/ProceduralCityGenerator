using UnityEngine;
using System.Collections;
using System;

public class PCBedesten : PCBuilding
{
    float outerWallHeight, outerWallThickness, doorWidth, mainDoorWidth;
    Vector2 texOffset;
    GameObject anObject;
    private float windowWidth;
    Texture roofTexture;

    public PCBedesten(Vector3 position, float size, float height, string buildingName):base(buildingName)
    {
        //GenerateModel(position,size,height);

    }
    public PCBedesten(Vector3 position, float size, float height, string buildingName, Texture t, Texture roofTexture) : base(buildingName,t)
    {
        outerWallHeight = 5;
        outerWallThickness = 0.8f;
        doorWidth = 0.8f;
        mainDoorWidth = 4;
        windowWidth = 0.6f;
        texOffset = new Vector2(0,0);
        this.roofTexture = roofTexture;
        GenerateModel(position, size, height);
    }

    private void GenerateModel(Vector3 position, float size, float height)
    {
        model = new GameObject("bedesten");
        model.transform.position = position;
        model.transform.localScale = new Vector3(1, 1, 1) / ProceduralCityGenerator.ScaleFactor;

        //calculate necessary sizes and vectors
        float bedestenWidth = Mathf.Sqrt(size*size/ ProceduralCityGenerator.GoldenRatio);
        float bedestenDepth = bedestenWidth * ProceduralCityGenerator.GoldenRatio;
        Vector3 lowerLeft = -Vector3.right * bedestenWidth / 2 - Vector3.forward * bedestenDepth / 2;
        Vector3 lowerRight = Vector3.right * bedestenWidth / 2 - Vector3.forward * bedestenDepth / 2;
        Vector3 upperLeft = -Vector3.right * bedestenWidth / 2 + Vector3.forward * bedestenDepth / 2;
        Vector3 upperRight = Vector3.right * bedestenWidth / 2 + Vector3.forward * bedestenDepth / 2;

        //build base
        HorizontalBuffer(lowerLeft, lowerRight, upperRight, upperLeft, 8);
        HorizontalBuffer(   lowerLeft -Vector3.right / 2 - Vector3.forward / 2 - Vector3.up/4, 
                            lowerRight + Vector3.right / 2 - Vector3.forward / 2 - Vector3.up / 4,
                            upperRight + Vector3.right / 2 + Vector3.forward / 2 - Vector3.up / 4,
                            upperLeft - Vector3.right / 2 + Vector3.forward / 2 - Vector3.up / 4, 8);
        HorizontalBuffer(lowerLeft - Vector3.right / 1 - Vector3.forward / 1 - Vector3.up / 2,
                            lowerRight + Vector3.right / 1 - Vector3.forward / 1 - Vector3.up / 2,
                            upperRight + Vector3.right / 1 + Vector3.forward / 1 - Vector3.up / 2,
                            upperLeft - Vector3.right / 1 + Vector3.forward / 1 - Vector3.up / 2, 8);

        //build front wall including the main gate
        float spaceFront = 5;
        Wall(lowerLeft+Vector3.right*7 + Vector3.forward * spaceFront, lowerRight - Vector3.right * 7 + Vector3.forward * spaceFront, false, false, true);
        //build right part
        Wall(lowerRight - Vector3.right * 7 + Vector3.forward * spaceFront, upperRight - Vector3.right * 7 - Vector3.forward * spaceFront);
        ArcWall(lowerRight - Vector3.right * 4 + Vector3.forward * spaceFront, upperRight - Vector3.right * 4 - Vector3.forward * spaceFront, outerWallHeight,0.5f,2.5f,3f);
        //build left part
        Wall(upperLeft + Vector3.right * 7 - Vector3.forward * spaceFront, lowerLeft + Vector3.right * 7 + Vector3.forward * spaceFront);
        ArcWall(upperLeft + Vector3.right * 4 - Vector3.forward * spaceFront, lowerLeft + Vector3.right * 4 + Vector3.forward * spaceFront, outerWallHeight, 0.5f, 2.5f,3f);
        //build the opposite part
        Wall(upperRight - Vector3.right * 7 - Vector3.forward * spaceFront, upperLeft + Vector3.right * 7 - Vector3.forward * spaceFront, false, false, true);


        HorizontalBuffer(lowerLeft + Vector3.right * 4 + Vector3.forward * spaceFront + Vector3.up*(outerWallHeight+0.5f),
                         lowerLeft + Vector3.right * 10 + Vector3.forward * spaceFront + Vector3.up * (outerWallHeight + 0.5f),
                         upperLeft + Vector3.right * 10 - Vector3.forward * spaceFront + Vector3.up * (outerWallHeight + 0.5f),
                         upperLeft + Vector3.right * 4 - Vector3.forward * spaceFront + Vector3.up * (outerWallHeight + 0.5f), 0.5f);

        HorizontalBuffer(lowerRight - Vector3.right * 10 + Vector3.forward * spaceFront + Vector3.up * (outerWallHeight + 0.5f),
                         lowerRight - Vector3.right * 4 + Vector3.forward * spaceFront + Vector3.up * (outerWallHeight + 0.5f),
                         upperRight - Vector3.right * 4 - Vector3.forward * spaceFront + Vector3.up * (outerWallHeight + 0.5f),
                         upperRight - Vector3.right * 10 - Vector3.forward * spaceFront + Vector3.up * (outerWallHeight + 0.5f), 0.5f);

        float t = outerWallThickness;

        //make roof
        Roof(lowerLeft + Vector3.right * 10 + Vector3.forward * (spaceFront+3) + Vector3.up * (outerWallHeight + 0.5f),
            lowerRight - Vector3.right * 10 + Vector3.forward * (spaceFront + 3) + Vector3.up * (outerWallHeight + 0.5f),
            upperRight - Vector3.right * 10 - Vector3.forward * (spaceFront + 3) + Vector3.up * (outerWallHeight + 0.5f),
            upperLeft + Vector3.right * 10 - Vector3.forward * (spaceFront + 3) + Vector3.up * (outerWallHeight + 0.5f));
    }


    private void Roof(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
    {
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
        primitive.transform.SetParent(model.transform, true);
        primitive.SetActive(false);
        Material diffuse = primitive.GetComponent<MeshRenderer>().sharedMaterial;

        GameObject roof = new GameObject();
        roof.AddComponent<MeshFilter>();
        roof.AddComponent<MeshRenderer>();
        roof.GetComponent<Renderer>().sharedMaterial = diffuse;

        Vector3 uEdge = br - bl;
        Vector3 vEdge = tr - br;
        Vector3 uDir = uEdge;
        uDir.Normalize();
        Vector3 vDir = vEdge;
        vDir.Normalize();

        Mesh m = roof.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = new Vector3[14];
        Vector2[] texCoords = new Vector2[14];
        vertices[0] = bl - uDir * 0.5f - vDir * 0.5f;
        vertices[7] = bl - uDir * 0.5f - vDir * 0.5f;
        vertices[1] = br + uDir * 0.5f - vDir * 0.5f;
        vertices[2] = br + uDir * 0.5f - vDir * 0.5f;
        vertices[3] = tr + uDir * 0.5f + vDir * 0.5f;
        vertices[4] = tr + uDir * 0.5f + vDir * 0.5f;
        vertices[5] = tl - uDir * 0.5f + vDir * 0.5f;
        vertices[6] = tl - uDir * 0.5f + vDir * 0.5f;
        if (uEdge.magnitude > vEdge.magnitude)
        {
            float uLength = (vertices[0] - vertices[5]).magnitude / 2;
            vertices[8] = (vertices[0] + vertices[5]) / 2 + new Vector3(uLength, uLength / 2, 0);
            vertices[12] = (vertices[0] + vertices[5]) / 2 + new Vector3(uLength, uLength / 2, 0);
            vertices[13] = (vertices[0] + vertices[5]) / 2 + new Vector3(uLength, uLength / 2, 0);
            vertices[9] = (vertices[1] + vertices[3]) / 2 + new Vector3(-uLength, uLength / 2, 0);
            vertices[10] = (vertices[1] + vertices[3]) / 2 + new Vector3(-uLength, uLength / 2, 0);
            vertices[11] = (vertices[1] + vertices[3]) / 2 + new Vector3(-uLength, uLength / 2, 0);
        }
        else
        {
            float vLength = (vertices[0] - vertices[1]).magnitude / 2;
            vertices[8] = (vertices[0] + vertices[1]) / 2 + new Vector3(0, vLength / 2, vLength);
            vertices[9] = (vertices[0] + vertices[1]) / 2 + new Vector3(0, vLength / 2, vLength);
            vertices[13] = (vertices[0] + vertices[1]) / 2 + new Vector3(0, vLength / 2, vLength);
            vertices[10] = (vertices[3] + vertices[5]) / 2 + new Vector3(0, vLength / 2, -vLength);
            vertices[11] = (vertices[3] + vertices[5]) / 2 + new Vector3(0, vLength / 2, -vLength);
            vertices[12] = (vertices[3] + vertices[5]) / 2 + new Vector3(0, vLength / 2, -vLength);
        }
        m.vertices = vertices;
        int[] faces = new int[18];
        if (uEdge.magnitude > vEdge.magnitude)
        {
            faces[0] = 0; faces[2] = 1; faces[1] = 8;
            faces[3] = 1; faces[5] = 9; faces[4] = 8;
            faces[6] = 2; faces[8] = 3; faces[7] = 10;
            faces[9] = 4; faces[11] = 5; faces[10] = 11;
            faces[12] = 5; faces[14] = 12; faces[13] = 11;
            faces[15] = 6; faces[17] = 7; faces[16] = 13;
        }
        else
        {
            faces[0] = 0; faces[2] = 1; faces[1] = 8;
            faces[3] = 2; faces[5] = 3; faces[4] = 9;
            faces[6] = 3; faces[8] = 10; faces[7] = 9;
            faces[9] = 4; faces[11] = 5; faces[10] = 11;
            faces[12] = 6; faces[14] = 7; faces[13] = 12;
            faces[15] = 7; faces[17] = 13; faces[16] = 12;
        }
        m.triangles = faces;
        Vector3 n1, n2, n3, n4;
        n1 = Vector3.Cross(vertices[1] - vertices[0], vertices[8] - vertices[0]);
        n2 = Vector3.Cross(vertices[3] - vertices[2], vertices[10] - vertices[2]);
        n3 = Vector3.Cross(vertices[5] - vertices[4], vertices[11] - vertices[4]);
        n4 = Vector3.Cross(vertices[7] - vertices[6], vertices[13] - vertices[6]);

        n1.Normalize();
        n2.Normalize();
        n3.Normalize();
        n4.Normalize();
        Vector3[] normals = new Vector3[vertices.Length];
        if (uEdge.magnitude > vEdge.magnitude)
        {
            normals[0] = n1;
            normals[1] = n1;
            normals[8] = n1;
            normals[9] = n1;
            normals[2] = n2;
            normals[3] = n2;
            normals[10] = n2;
            normals[4] = n3;
            normals[5] = n3;
            normals[11] = n3;
            normals[12] = n3;
            normals[6] = n4;
            normals[7] = n4;
            normals[13] = n4;
        }
        else
        {
            normals[0] = n1;
            normals[1] = n1;
            normals[8] = n1;
            normals[9] = n2;
            normals[2] = n2;
            normals[3] = n2;
            normals[10] = n2;
            normals[4] = n3;
            normals[5] = n3;
            normals[11] = n3;
            normals[12] = n4;
            normals[6] = n4;
            normals[7] = n4;
            normals[13] = n4;
        }
        m.normals = normals;

        for (int i = 0; i < 14; i++)
        {
            if ((uEdge.magnitude > vEdge.magnitude && (i == 0 || i == 1 || i == 8 || i == 9)) || (uEdge.magnitude <= vEdge.magnitude && (i == 0 || i == 1 || i == 8)))
                texCoords[i] = (new Vector2(vertices[i].x - vertices[0].x, vertices[i].z - vertices[0].z));
            if ((uEdge.magnitude > vEdge.magnitude && (i == 4 || i == 5 || i == 11 || i == 12)) || (uEdge.magnitude <= vEdge.magnitude && (i == 4 || i == 5 || i == 11)))
                texCoords[i] = (new Vector2(vertices[5].x - vertices[i].x, vertices[5].z - vertices[i].z));
            if ((uEdge.magnitude > vEdge.magnitude && (i == 2 || i == 3 || i == 10)) || (uEdge.magnitude <= vEdge.magnitude && (i == 2 || i == 3 || i == 9 || i == 10)))
                texCoords[i] = (new Vector2(vertices[5].z - vertices[i].z, vertices[5].x - vertices[i].x));
            if ((uEdge.magnitude > vEdge.magnitude && (i == 6 || i == 7 || i == 13)) || (uEdge.magnitude <= vEdge.magnitude && (i == 6 || i == 7 || i == 12 || i == 13)))
                texCoords[i] = (new Vector2(vertices[i].z - vertices[0].z, vertices[i].x - vertices[0].x));
            texCoords[i] *= 0.5f;
        }
        m.uv = texCoords;

        //adjustTexture
        Renderer houseWallRenderer = roof.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", roofTexture);
        //houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 0.3f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer.material.SetColor("_Color", Color.white * UnityEngine.Random.Range(0.6f, 1));

        roof.transform.SetParent(model.transform, false);


        GameObject underRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //adjustTexture
        Renderer middleWallRenderer = underRoof.GetComponent<Renderer>();
        middleWallRenderer.material.SetTexture("_MainTex", texture);
        middleWallRenderer.material.SetTextureScale("_MainTex", new Vector2(0.1f, 0.1f));
        middleWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        middleWallRenderer.material.SetColor("_Color", Color.black);

        underRoof.transform.SetParent(model.transform, false);
        underRoof.transform.localScale = new Vector3(0.05f, (vertices[1] - vertices[0]).magnitude, (vertices[3] - vertices[1]).magnitude);
        underRoof.transform.localPosition = (bl + br + tr + tl) / 4f;
        underRoof.transform.Rotate(Vector3.forward, 90);
    }


    private void InnerWall(Vector3 from, Vector3 to)
    {
        //change necessary parameters and make inner walls
        outerWallHeight = 3;
        outerWallThickness = 0.5f;

        int numRooms = Mathf.RoundToInt((to - from).magnitude / 4);

        for(int i = 0; i < numRooms; i++)
        {
            Wall(from + i * (to - from) / numRooms, from + (i + 0.5f) * (to - from) / numRooms, false, true);
            Wall(from + (i + 0.5f) * (to - from) / numRooms, from + (i + 1f) * (to - from) / numRooms, true);
        }
    }
    
    private void HorizontalBuffer(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, float thickness)
    {
        GameObject bottomWall = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //adjustTexture
        Renderer bottomWallRenderer = bottomWall.GetComponent<Renderer>();
        //bottomWallRenderer.material.SetTexture("_MainTex", texture);
        //bottomWallRenderer.material.SetTextureScale("_MainTex", new Vector2((br - bl).magnitude / 5, thickness/2));
        //bottomWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        bottomWallRenderer.material.SetColor("_Color", Color.white * 0.6f);

        bottomWall.transform.SetParent(model.transform, false);
        bottomWall.transform.localScale = new Vector3((br - bl).magnitude, thickness, (tr - br).magnitude);
        bottomWall.transform.localPosition = (bl + br + tr + tl) / 4f + new Vector3(0, -thickness/2, 0);
    }


    private void Wall(Vector3 from, Vector3 to, bool door = false, bool windows = false, bool mainDoor = false)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        if (!door && !windows && !mainDoor)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);

            //adjustTexture
            Renderer houseWallRenderer = wall.GetComponent<Renderer>();
            houseWallRenderer.material.SetTexture("_MainTex", texture);
            houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, outerWallHeight));
            houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
            //houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

            wall.transform.SetParent(model.transform, false);
            wall.transform.localScale = (new Vector3((to - from).magnitude, outerWallHeight, outerWallThickness));
            wall.transform.localPosition = (from + to + towardsIn * outerWallThickness + Vector3.up * outerWallHeight) / 2f;
            wall.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));
        }
        else if(mainDoor)
        {
            if ((to - from).magnitude <= mainDoorWidth)
            {
                Wall(from, to, true, windows, false);
                Debug.Log("Not enough room for the main door, making small door!");
            }
            else
            {
                //make a centered door
                MainDoor((from + to - dir * mainDoorWidth) / 2, (from + to + dir * mainDoorWidth) / 2);
                Wall(from, (from + to - dir * mainDoorWidth) / 2, false, windows);
                Wall((from + to + dir * mainDoorWidth) / 2, to, false, windows);
            }
        }
        else if (door)
        {
            if ((to - from).magnitude <= doorWidth)
            {
                Wall(from, to, false, windows);
                Debug.Log("Not enough room for a door!");
            }
            else
            {
                //make a centered door
                Door((from + to - dir * doorWidth) / 2, (from + to + dir * doorWidth) / 2);
                Wall(from, (from + to - dir * doorWidth) / 2, false, windows);
                Wall((from + to + dir * doorWidth) / 2, to, false, windows);
            }
        }
        else if (windows)
        {
            float sideLength = (to - from).magnitude;
            // how many windows fits
            int windowsCount = Mathf.RoundToInt((sideLength - outerWallThickness * 2) / (2 * windowWidth * ProceduralCityGenerator.GoldenRatio));
            //evenly distribute windows like -w-w-w-
            float intervalLength = (sideLength - windowsCount * windowWidth) / (windowsCount + 1f);
            Wall(from, from + dir * intervalLength);
            Vector3 windowFrom = from + dir * intervalLength;
            for (int i = 0; i < windowsCount; i++)
            {
                Window(windowFrom, windowFrom + dir * windowWidth);
                windowFrom += dir * (windowWidth + intervalLength);
                Wall(windowFrom - dir * intervalLength, windowFrom);
            }

        }
    }

    private void Window(Vector3 from, Vector3 to)
    {
        GameObject underWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        underWindow.transform.SetParent(model.transform, false);
        underWindow.transform.localScale = (new Vector3((to - from).magnitude, outerWallHeight / 3, outerWallThickness));

        //adjustTexture
        Renderer houseWallRenderer = underWindow.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 0.6f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        //houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);
        underWindow.transform.localPosition = (from + to + towardsIn * outerWallThickness + Vector3.up * outerWallHeight / 3f) / 2f;
        underWindow.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));


        GameObject overWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overWindow.transform.SetParent(model.transform, false);
        overWindow.transform.localScale = (new Vector3((to - from).magnitude, outerWallHeight / 3, outerWallThickness));

        //adjustTexture
        houseWallRenderer = overWindow.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 0.6f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        //houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        overWindow.transform.localPosition = (from + to + towardsIn * outerWallThickness + 10f * Vector3.up * outerWallHeight / 6f) / 2f;
        overWindow.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));

        //make inner window
        Vector3 WindowCornersUpperLeft = from;
        Vector3 WindowCornersLowerLeft = from;
        Vector3 WindowCornersUpperRight = to;
        Vector3 WindowCornersLowerRight = to;
        WindowCornersUpperLeft.y += 2f * outerWallHeight / 3;
        WindowCornersLowerLeft.y += outerWallHeight / 3;
        WindowCornersUpperRight.y += 2f * outerWallHeight / 3;
        WindowCornersLowerRight.y += outerWallHeight / 3;
        /*MakeElongatedCube(WindowCornersLowerLeft, WindowCornersLowerRight, 0.15f);
        MakeElongatedCube(WindowCornersUpperLeft, WindowCornersUpperRight, 0.15f);
        MakeElongatedCube(WindowCornersUpperLeft, WindowCornersLowerLeft, 0.15f);
        MakeElongatedCube(WindowCornersUpperRight, WindowCornersLowerRight, 0.15f);*/
        MakeElongatedCube((2*WindowCornersUpperRight + WindowCornersUpperLeft) / 3, (2*WindowCornersLowerRight + WindowCornersLowerLeft) / 3, 0.05f);
        MakeElongatedCube((WindowCornersUpperRight + 2*WindowCornersUpperLeft) / 3, (WindowCornersLowerRight + 2* WindowCornersLowerLeft) / 3, 0.05f);
        MakeElongatedCube(WindowCornersLowerLeft / 3f + WindowCornersUpperLeft * 2f / 3, WindowCornersLowerRight / 3f + WindowCornersUpperRight * 2f / 3f, 0.05f);
        MakeElongatedCube(WindowCornersLowerLeft * 2f / 3f + WindowCornersUpperLeft / 3f, WindowCornersLowerRight * 2f / 3f + WindowCornersUpperRight / 3f, 0.05f);
    }

    private void MakeElongatedCube(Vector3 from, Vector3 to, float width)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //adjustTexture
        Renderer houseWallRenderer = cube.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 1));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        houseWallRenderer.material.SetColor("_Color", Color.white * 0.5f);

        cube.transform.SetParent(model.transform, false);
        cube.transform.localScale = (new Vector3((to - from).magnitude + width, width, width));
        cube.transform.localPosition = (from + to) / 2f;
        Vector3 dir = to - from;
        dir.Normalize();
        cube.transform.Rotate(Vector3.Cross(Vector3.right, dir), Vector3.SignedAngle(Vector3.right, dir, Vector3.Cross(Vector3.right, dir)));

    }

    private void ArcWall(Vector3 from, Vector3 to2, float height, float thickness, float columnHeight, float depth)
    {
        Vector3 dir = to2 - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        Vector3 to = to2 - dir * thickness;

        float length = (to - from).magnitude;
        float insideLength = columnHeight * Mathf.Tan(Mathf.PI / 4);
        int numUnits = Mathf.RoundToInt(length / (insideLength + thickness));
        float unitLength = length / numUnits;
        float wallt = outerWallThickness;
        outerWallThickness = 0.5f;
        for (int i = 0; i <= numUnits; i++)
        {
            if(i < numUnits)
                ArcWallUnit(from + i * (to - from) / numUnits, from + (i + 1) * (to - from) / numUnits, height, thickness, columnHeight);
            Wall(from + i * (to - from) / numUnits, from + i * (to - from) / numUnits + towardsIn * 3);
        }
        outerWallThickness = wallt;
    }



    private void ArcWallUnit(Vector3 from, Vector3 to, float height, float thickness, float columnHeight)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);
        float arcRadius = Mathf.Sqrt(Mathf.Pow(((to - from).magnitude - thickness) / 2, 2) + Mathf.Pow(columnHeight / 2, 2));

        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
        primitive.transform.SetParent(model.transform, true);
        primitive.SetActive(false);
        Material diffuse = primitive.GetComponent<MeshRenderer>().sharedMaterial;

        GameObject arcWall = new GameObject();
        arcWall.AddComponent<MeshFilter>();
        arcWall.AddComponent<MeshRenderer>();
        arcWall.GetComponent<Renderer>().sharedMaterial = diffuse;


        //make the front face
        Mesh m = arcWall.GetComponent<MeshFilter>().mesh;

        int arcSamples = 7; 
        Vector3[] vertices = new Vector3[4*(8+arcSamples)+16];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] texCoords = new Vector2[vertices.Length];

        vertices[0] = from + Vector3.up * height;
        vertices[1] = from;
        vertices[2] = from + dir * thickness / 2;
        vertices[3] = vertices[2] + Vector3.up * columnHeight;
        vertices[7 + arcSamples] = to + Vector3.up * height;
        vertices[6 + arcSamples] = to;
        vertices[5 + arcSamples] = to - dir * thickness / 2;
        vertices[4 + arcSamples] = vertices[5+arcSamples] + Vector3.up * columnHeight;

        Vector3 arcCenter = (to + from) / 2 + Vector3.up * columnHeight / 2;
        float endAngle = Mathf.Atan(columnHeight/((to-from).magnitude-thickness));
        float startAngle = Mathf.PI - endAngle;
        float angleStep = (endAngle - startAngle) / (arcSamples + 1);

        for (int i = 1; i <= arcSamples; i++)
        {
            vertices[3 + i] = arcCenter + dir * Mathf.Cos(startAngle + i * angleStep) * arcRadius + Vector3.up * Mathf.Sin(startAngle + i * angleStep) * arcRadius;
        }

        for (int i = 0; i < 8 + arcSamples; i++)
        {
            normals[i] = towardsIn * -1;
            Vector3 dif = vertices[i] - from;
            float ydif = dif.y;
            float xdif = Mathf.Sqrt(dif.magnitude * dif.magnitude - ydif * ydif);
            texCoords[i] = new Vector2(xdif / (to - from).magnitude, ydif / height);
        }

        int[] faces = new int[2*3*(6+arcSamples)+(8+arcSamples)*2*3];
        for (int i = 2; i <= 3 + (arcSamples+1)/2; i++)
        {
            faces[(i - 2) * 3] = 0;
            faces[(i - 2) * 3 + 1] = i;
            faces[(i - 2) * 3 + 2] = i-1;
        }
        for (int i = 3 + (arcSamples + 1) / 2; i <= 5 + arcSamples; i++)
        {
            faces[(i - 1) * 3] = arcSamples + 7;
            faces[(i - 1) * 3 + 1] = i+1;
            faces[(i - 1) * 3 + 2] = i;
        }
        faces[(arcSamples + 5) * 3] = 0;
        faces[(arcSamples + 5) * 3 + 1] = arcSamples + 7;
        faces[(arcSamples + 5) * 3 + 2] = 3 + (arcSamples + 1) / 2;

        //make the inner face
        int voffset = 8 + arcSamples;
        for(int i = 0; i < voffset; i++)
        {
            vertices[i + voffset] = vertices[i] + towardsIn * thickness;
            normals[i + voffset] = normals[i] * -1;
            texCoords[i + voffset] = texCoords[i];
        }
        
        int foffset = 6 + arcSamples;
        for (int i = 0; i < foffset; i++)
        {
            faces[3*(i+foffset)] = faces[3*i] + voffset;
            faces[3 * (i + foffset) + 1] = faces[3 * i + 2] + voffset;
            faces[3 * (i + foffset) + 2] = faces[3 * i + 1] + voffset;
        }

        //make the 3rd dim
        int additional = 0;
        for(int i = 0; i <= 1; i++)
        {
            vertices[2 * voffset + i] = vertices[i];
            vertices[3 * voffset + i + 8] = vertices[voffset + i];
            normals[2 * voffset + i] = -dir;
            normals[3 * voffset + i + 8] = -dir;
            texCoords[2 * voffset + i] = texCoords[i];
            texCoords[3 * voffset + i + 8] = texCoords[i];
        }
        additional++;
        for (int i = 1; i <= 2; i++)
        {
            vertices[2 * voffset + i + additional] = vertices[i];
            vertices[3 * voffset + i + 8 + additional] = vertices[voffset + i];
            normals[2 * voffset + i + additional] = Vector3.down;
            normals[3 * voffset + i + 8 + additional] = Vector3.down;
            texCoords[2 * voffset + i + additional] = texCoords[i];
            texCoords[3 * voffset + i + 8 + additional] = texCoords[i];
        }
        additional++;
        for (int i = 2; i <= 3; i++)
        {
            vertices[2 * voffset + i + additional] = vertices[i];
            vertices[3 * voffset + i + 8 + additional] = vertices[voffset + i];
            normals[2 * voffset + i + additional] = dir;
            normals[3 * voffset + i + 8 + additional] = dir;
            texCoords[2 * voffset + i + additional] = texCoords[i];
            texCoords[3 * voffset + i + 8 + additional] = texCoords[i];
        }
        additional++;
        for (int i = 3; i <= 4+arcSamples; i++)
        {
            vertices[2 * voffset + i + additional] = vertices[i];
            vertices[3 * voffset + i + 8 + additional] = vertices[voffset + i];
            Vector3 n = arcCenter - vertices[i];
            n.Normalize();
            normals[2 * voffset + i + additional] = n;
            normals[3 * voffset + i + 8 + additional] = n;
            texCoords[2 * voffset + i + additional] = texCoords[i];
            texCoords[3 * voffset + i + 8 + additional] = texCoords[i];
        }
        additional++;
        for (int i = 4+arcSamples; i <= 5 + arcSamples; i++)
        {
            vertices[2 * voffset + i + additional] = vertices[i];
            vertices[3 * voffset + i + 8 + additional] = vertices[voffset + i];
            normals[2 * voffset + i + additional] = -dir;
            normals[3 * voffset + i + 8 + additional] = -dir;
            texCoords[2 * voffset + i + additional] = texCoords[i];
            texCoords[3 * voffset + i + 8 + additional] = texCoords[i];
        }
        additional++;
        for (int i = 5 + arcSamples; i <= 6 + arcSamples; i++)
        {
            vertices[2 * voffset + i + additional] = vertices[i];
            vertices[3 * voffset + i + 8 + additional] = vertices[voffset + i];
            normals[2 * voffset + i + additional] = Vector3.down;
            normals[3 * voffset + i + 8 + additional] = Vector3.down;
            texCoords[2 * voffset + i + additional] = texCoords[i];
            texCoords[3 * voffset + i + 8 + additional] = texCoords[i];
        }
        additional++;
        for (int i = 6 + arcSamples; i <= 7 + arcSamples; i++)
        {
            vertices[2 * voffset + i + additional] = vertices[i];
            vertices[3 * voffset + i + 8 + additional] = vertices[voffset + i];
            normals[2 * voffset + i + additional] = dir;
            normals[3 * voffset + i + 8 + additional] = dir;
            texCoords[2 * voffset + i + additional] = texCoords[i];
            texCoords[3 * voffset + i + 8 + additional] = texCoords[i];
        }
        additional++;
        for (int i = 7 + arcSamples; i <= 8 + arcSamples; i++)
        {
            vertices[2 * voffset + i + additional] = vertices[(i% voffset)];
            vertices[3 * voffset + i + 8 + additional] = vertices[voffset + (i % voffset)];
            normals[2 * voffset + i + additional] = Vector3.up;
            normals[3 * voffset + i + 8 + additional] = Vector3.up;
            texCoords[2 * voffset + i + additional] = texCoords[(i % voffset)];
            texCoords[3 * voffset + i + 8 + additional] = texCoords[(i % voffset)];
        }

        int faceIndex = foffset * 2;
        additional = 0;
        for(int i = 0; i < 15 + arcSamples; i++)
        {
            if (i == 1 || i == 3 || i == 5 || i == arcSamples + 7 || i == arcSamples + 7 || i == arcSamples + 9 || i == arcSamples + 11 || i == arcSamples + 13)
            {
                additional++;
                continue;
            }
            faces[3 * faceIndex] = 2 * voffset + i;
            faces[3 * faceIndex + 1] = 2 * voffset + i + 1;
            faces[3 * faceIndex + 2] = 3 * voffset + i + 1 + 8;
            faces[3 * faceIndex + 3] = 2 * voffset + i;
            faces[3 * faceIndex + 4] = 3 * voffset + i + 1 + 8;
            faces[3 * faceIndex + 5] = 3 * voffset + i + 8;
            faceIndex += 2;
        }
        m.vertices = vertices;
        m.normals = normals;
        m.uv = texCoords;
        m.triangles = faces;

        //adjustTexture
        Renderer houseWallRenderer = arcWall.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 3f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        //houseWallRenderer.material.SetColor("_Color", Color.white * UnityEngine.Random.Range(0.6f, 1));

        arcWall.transform.SetParent(model.transform, false);
        
    }

    private void MainDoor(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        Wall(from + towardsIn*3, from - towardsIn * 1);
        Wall(to - towardsIn, to + towardsIn * 3);
        HorizontalBuffer(from + towardsIn * 3.2f + Vector3.up * (outerWallHeight+0.5f) - dir*0.2f, 
                        from - towardsIn * 1.2f + Vector3.up * (outerWallHeight + 0.5f) - dir * 0.2f, 
                        to - towardsIn * 1.2f + Vector3.up * (outerWallHeight + 0.5f) + dir * 0.2f, 
                        to + towardsIn * 3.2f + Vector3.up * (outerWallHeight + 0.5f) + dir * 0.2f, 1);
        ArcWallUnit(from + dir * outerWallThickness, to - dir * outerWallThickness, 3, 0.5f, 2);
        OverTheDoor(from + dir * outerWallThickness + Vector3.up*3, 
            to - dir * outerWallThickness + Vector3.up * 3, 2, outerWallHeight-3);

    }

    private void OverTheDoor(Vector3 from, Vector3 to, int depth, float height)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
        primitive.transform.SetParent(model.transform, true);
        primitive.SetActive(false);
        Material diffuse = primitive.GetComponent<MeshRenderer>().sharedMaterial;

        GameObject overTheDoor = new GameObject();
        overTheDoor.AddComponent<MeshFilter>();
        overTheDoor.AddComponent<MeshRenderer>();
        overTheDoor.GetComponent<Renderer>().sharedMaterial = diffuse;
        
        //make the front face
        Mesh m = overTheDoor.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = new Vector3[30];
        Vector3[] corners = new Vector3[9];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] texCoords = new Vector2[vertices.Length];

        corners[0] = from;
        corners[1] = from + towardsIn * depth;
        corners[2] = to;
        corners[3] = to + towardsIn * depth;
        corners[4] = from + Vector3.up*height;
        corners[5] = from + towardsIn * depth + Vector3.up * height;
        corners[6] = to + Vector3.up * height;
        corners[7] = to + towardsIn * depth + Vector3.up * height;
        corners[8] = corners[4] * 0.5f + corners[6] * 0.5f - Vector3.up * height * 1.0f;

        int[] faces = new int[14*3];

        int fc = 0;
        int voffset = 0;

        //left face
        vertices[voffset + 0] = corners[0];
        vertices[voffset + 1] = corners[4];
        vertices[voffset + 2] = corners[1];
        vertices[voffset + 3] = corners[5];
        normals[voffset + 0] = -dir;
        normals[voffset + 1] = -dir;
        normals[voffset + 2] = -dir;
        normals[voffset + 3] = -dir;
        texCoords[voffset + 0] = new Vector2(0, 0);
        texCoords[voffset + 1] = new Vector2(depth / height, 0);
        texCoords[voffset + 2] = new Vector2(0, 1);
        texCoords[voffset + 3] = new Vector2(depth / height, 1);
        faces[fc++] = voffset + 0; faces[fc++] = voffset + 2; faces[fc++] = voffset + 1;
        faces[fc++] = voffset + 1; faces[fc++] = voffset + 2; faces[fc++] = voffset + 3;

        voffset += 4;

        //top face
        vertices[voffset + 0] = corners[4];
        vertices[voffset + 1] = corners[6];
        vertices[voffset + 2] = corners[5];
        vertices[voffset + 3] = corners[7];
        normals[voffset + 0] = Vector3.up;
        normals[voffset + 1] = Vector3.up;
        normals[voffset + 2] = Vector3.up;
        normals[voffset + 3] = Vector3.up;
        texCoords[voffset + 0] = new Vector2(0, 0);
        texCoords[voffset + 1] = new Vector2(depth / height, 0);
        texCoords[voffset + 2] = new Vector2(0, (to-from).magnitude/height);
        texCoords[voffset + 3] = new Vector2(depth / height, (to - from).magnitude / height);
        faces[fc++] = voffset + 0; faces[fc++] = voffset + 2; faces[fc++] = voffset + 1;
        faces[fc++] = voffset + 1; faces[fc++] = voffset + 2; faces[fc++] = voffset + 3;

        voffset += 4;

        //right face
        vertices[voffset + 0] = corners[3];
        vertices[voffset + 1] = corners[7];
        vertices[voffset + 2] = corners[2];
        vertices[voffset + 3] = corners[6];
        normals[voffset + 0] = dir;
        normals[voffset + 1] = dir;
        normals[voffset + 2] = dir;
        normals[voffset + 3] = dir;
        texCoords[voffset + 0] = new Vector2(0, 0);
        texCoords[voffset + 1] = new Vector2(depth / height, 0);
        texCoords[voffset + 2] = new Vector2(0, 1);
        texCoords[voffset + 3] = new Vector2(depth / height, 1);
        faces[fc++] = voffset + 0; faces[fc++] = voffset + 2; faces[fc++] = voffset + 1;
        faces[fc++] = voffset + 1; faces[fc++] = voffset + 2; faces[fc++] = voffset + 3;

        voffset += 4;

        //backface
        vertices[voffset + 0] = corners[1];
        vertices[voffset + 1] = corners[3];
        vertices[voffset + 2] = corners[5];
        vertices[voffset + 3] = corners[7];
        normals[voffset + 0] = towardsIn;
        normals[voffset + 1] = towardsIn;
        normals[voffset + 2] = towardsIn;
        normals[voffset + 3] = towardsIn;
        texCoords[voffset + 0] = new Vector2(0, 0);
        texCoords[voffset + 1] = new Vector2(1, 0);
        texCoords[voffset + 2] = new Vector2(0, 1);
        texCoords[voffset + 3] = new Vector2(1, 1);
        faces[fc++] = voffset + 0; faces[fc++] = voffset + 1; faces[fc++] = voffset + 2;
        faces[fc++] = voffset + 1; faces[fc++] = voffset + 3; faces[fc++] = voffset + 2;

        voffset += 4;

        //front face
        vertices[voffset + 0] = corners[0];
        vertices[voffset + 1] = corners[4];
        vertices[voffset + 2] = corners[6];
        vertices[voffset + 3] = corners[2];
        vertices[voffset + 4] = corners[8];
        normals[voffset + 0] = -towardsIn;
        normals[voffset + 1] = -towardsIn;
        normals[voffset + 2] = -towardsIn;
        normals[voffset + 3] = -towardsIn;
        normals[voffset + 4] = -towardsIn;
        texCoords[voffset + 0] = new Vector2(0, 0);
        texCoords[voffset + 1] = new Vector2(0, 1);
        texCoords[voffset + 2] = new Vector2(1, 1);
        texCoords[voffset + 3] = new Vector2(1, 0);
        texCoords[voffset + 4] = new Vector2(0.5f, 0.0f);
        faces[fc++] = voffset + 0; faces[fc++] = voffset + 1; faces[fc++] = voffset + 4;
        faces[fc++] = voffset + 1; faces[fc++] = voffset + 2; faces[fc++] = voffset + 4;
        faces[fc++] = voffset + 2; faces[fc++] = voffset + 3; faces[fc++] = voffset + 4;

        voffset += 5;

        //mukarnas left
        vertices[voffset + 0] = corners[0];
        vertices[voffset + 1] = corners[8];
        vertices[voffset + 2] = corners[1];
        Vector3 n = Vector3.Cross(towardsIn, corners[8] - corners[0]);
        n.Normalize();
        normals[voffset + 0] = n;
        normals[voffset + 1] = n;
        normals[voffset + 2] = n;
        texCoords[voffset + 0] = new Vector2(0, 0);
        texCoords[voffset + 1] = new Vector2(0, 0.8f);
        texCoords[voffset + 2] = new Vector2(depth/height, 0);
        faces[fc++] = voffset + 0; faces[fc++] = voffset + 1; faces[fc++] = voffset + 2;

        voffset += 3;

        //mukarnas right
        vertices[voffset + 0] = corners[2];
        vertices[voffset + 1] = corners[3];
        vertices[voffset + 2] = corners[8];
        n = Vector3.Cross(towardsIn, corners[2] - corners[8]);
        n.Normalize();
        normals[voffset + 0] = n;
        normals[voffset + 1] = n;
        normals[voffset + 2] = n;
        texCoords[voffset + 0] = new Vector2(0, 0);
        texCoords[voffset + 1] = new Vector2(depth/height, 0);
        texCoords[voffset + 2] = new Vector2(0, 0.8f);
        faces[fc++] = voffset + 0; faces[fc++] = voffset + 1; faces[fc++] = voffset + 2;

        voffset += 3;

        //mukarnas back
        vertices[voffset + 0] = corners[1];
        vertices[voffset + 1] = corners[8];
        vertices[voffset + 2] = corners[3];
        n = Vector3.Cross(dir, corners[8] - corners[1]);
        n.Normalize();
        normals[voffset + 0] = n;
        normals[voffset + 1] = n;
        normals[voffset + 2] = n;
        texCoords[voffset + 0] = new Vector2(0, 0);
        texCoords[voffset + 1] = new Vector2(0.5f, 0.8f);
        texCoords[voffset + 2] = new Vector2(1, 0);
        faces[fc++] = voffset + 0; faces[fc++] = voffset + 1; faces[fc++] = voffset + 2;


        m.vertices = vertices;
        m.normals = normals;    
        m.uv = texCoords;
        m.triangles = faces;

        //adjustTexture
        Renderer houseWallRenderer = overTheDoor.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 3f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        //houseWallRenderer.material.SetColor("_Color", Color.white * UnityEngine.Random.Range(0.6f, 1));

        overTheDoor.transform.SetParent(model.transform, false);

        if (anObject == null)
            anObject = overTheDoor;
    }

    public void updateTexture()
    {
        Renderer houseWallRenderer = anObject.GetComponent<Renderer>();
        houseWallRenderer.material.SetTextureOffset("_MainTex", texOffset);
        texOffset += new Vector2(0.001f, 0.01f);

    }

    private void Door(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        GameObject overDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overDoor.transform.SetParent(model.transform, false);
        overDoor.transform.localScale = (new Vector3((to - from).magnitude, outerWallHeight / 3, outerWallThickness));

        //adjustTexture
        Renderer houseWallRenderer = overDoor.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2((to - from).magnitude / 4f, 0.6f));
        houseWallRenderer.material.SetTextureOffset("_MainTex", new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        //houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        overDoor.transform.localPosition = (from + to + towardsIn * outerWallThickness + 10f * Vector3.up * outerWallHeight / 6f) / 2f;
        overDoor.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));

    }

}
