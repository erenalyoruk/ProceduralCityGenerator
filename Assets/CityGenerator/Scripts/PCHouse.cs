using System;
using System.Collections;
using UnityEngine;

public class PCHouse : PCBuilding
{
    public int accommodatesCurrent;
    int numFloors;
    float courtyardArea;
    float edgeSize;
    float coveredEdgeSize;
    float courtyardEdgeSize;
    float outerWallThickness,
        outerWallHeight;
    float houseWallThickness,
        houseWallHeight;
    float doorWidth = 1f;
    float windowWidth = 0.8f;
    float colorDarkening;
    int colorStyle = 1;
    Texture houseWallTexture,
        stoneWallTexture,
        roofTexture;
    public Vector2Int secondaryEntranceGrid; //if there is a second door

    public PCHouse(
        Vector3 position,
        int numFloors,
        float coveredArea,
        float courtyardArea,
        int accommodates,
        string buildingName,
        Texture houseWallTexture,
        Texture stoneWallTexture,
        Texture roofTexture,
        GameObject tree1,
        GameObject tree2
    )
        : base(buildingName)
    {
        this.numFloors = numFloors;
        this.courtyardArea = courtyardArea;
        this.houseWallTexture = houseWallTexture;
        this.stoneWallTexture = stoneWallTexture;
        this.roofTexture = roofTexture;
        this.tree1 = tree1;
        this.tree2 = tree2;
        edgeSize = Mathf.Sqrt(coveredArea + courtyardArea);
        if (courtyardArea / coveredArea > 2)
        {
            edgeSize = Mathf.Sqrt(
                (coveredArea + courtyardArea) / ProceduralCityGenerator.GoldenRatio
            );
        }
        coveredEdgeSize = coveredArea / edgeSize;
        courtyardEdgeSize = courtyardArea / edgeSize;

        houseWallThickness = 0.5f;
        houseWallHeight = 2.7f;
        outerWallThickness = 0.6f;
        outerWallHeight = 2f;
        colorDarkening = 0.7f + UnityEngine.Random.value * 0.3f;
        accommodatesCurrent = accommodates;
        colorStyle = UnityEngine.Random.value > 0.7 ? 2 : 1;
        GenerateModel(position);
    }

    private void GenerateModel(Vector3 position)
    {
        model = new GameObject(buildingName);
        model.transform.position = position;

        float[] privacyCosts = pcg.CheckPrivacyCost(
            position
                + (numFloors - 0.5f)
                    * (houseWallHeight / ProceduralCityGenerator.ScaleFactor)
                    * Vector3.up
        );
        float[] sceneryScores = pcg.CheckSceneryScoreOld(
            position
                + (numFloors - 0.5f)
                    * (houseWallHeight / ProceduralCityGenerator.ScaleFactor)
                    * Vector3.up
        );
        float rotAngle = FindBestOrientation(privacyCosts, sceneryScores);

        //model.transform.Rotate(Vector3.up, UnityEngine.Random.value * 90);
        model.transform.Rotate(Vector3.up, rotAngle - 90);
        model.transform.localScale = new Vector3(1, 1, 1) / ProceduralCityGenerator.ScaleFactor;

        privateRegions = new GameObject();
        privateRegions.transform.SetParent(model.transform);

        //generate courtyard walls
        Vector3 lowerLeft = new Vector3(
            -(coveredEdgeSize + courtyardEdgeSize) / 2,
            0,
            -edgeSize / 2
        );
        Vector3 upperLeft = new Vector3(
            -(coveredEdgeSize + courtyardEdgeSize) / 2,
            0,
            edgeSize / 2
        );
        Vector3 courtYardEdgeVector = new Vector3(courtyardEdgeSize, 0, 0);
        Vector3 coveredAreaVector = new Vector3(coveredEdgeSize, 0, 0);

        if (courtyardArea > 25)
        {
            OuterWall(lowerLeft, lowerLeft + courtYardEdgeVector);
            OuterWall(upperLeft, lowerLeft, true);
            OuterWall(upperLeft + courtYardEdgeVector, upperLeft);

            AddPrivateRegion(
                upperLeft + courtYardEdgeVector + Vector3.up * outerWallHeight,
                upperLeft + Vector3.up * outerWallHeight,
                lowerLeft + courtYardEdgeVector + Vector3.up * outerWallHeight,
                lowerLeft + Vector3.up * outerWallHeight
            );

            int numTreesI = (int)(courtyardEdgeSize / 3);
            int numTreesJ = (int)(edgeSize / 3);
            for (int i = 0; i < numTreesI; i++)
            {
                for (int j = 0; j < numTreesJ; j++)
                {
                    if (UnityEngine.Random.value > 0.6f)
                    {
                        Vector3 TreePos =
                            lowerLeft
                            + (upperLeft - lowerLeft) / edgeSize
                            + coveredAreaVector / coveredEdgeSize
                            + (upperLeft - lowerLeft - 2 * (upperLeft - lowerLeft) / edgeSize)
                                / numTreesI
                                * (i + UnityEngine.Random.value)
                            + (courtYardEdgeVector - 2 * courtYardEdgeVector / courtyardEdgeSize)
                                / numTreesJ
                                * (j + UnityEngine.Random.value);
                        TreePos = model.transform.TransformPoint(TreePos);
                        pcg.MakeTree(new Vector2(TreePos.x, TreePos.z));
                    }
                }
            }
        }

        BottomBuffer(
            lowerLeft,
            lowerLeft + courtYardEdgeVector + coveredAreaVector,
            upperLeft + courtYardEdgeVector + coveredAreaVector,
            upperLeft
        );

        Vector3 heightVector = new Vector3(0, 0, 0);
        bool CumbaExists = (numFloors > 1) && (UnityEngine.Random.value > 0.3f);
        for (int i = 0; i < numFloors; i++)
        {
            //bottom wall of the house
            HouseWall(
                heightVector + lowerLeft + courtYardEdgeVector,
                heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector
            ); //right side
            //left(inside/courtyard) wall of the house
            HouseWall(
                heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector,
                heightVector + upperLeft + courtYardEdgeVector
            ); //left side
            //top wall of the house
            HouseWall(
                heightVector + upperLeft + courtYardEdgeVector,
                heightVector + lowerLeft + courtYardEdgeVector,
                i == 0,
                true
            );
            if (CumbaExists && i > 0)
            {
                if (edgeSize > windowWidth * 2 * 6) //cumba takes all side or part of it
                {
                    HouseWall(
                        heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector,
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + (upperLeft - lowerLeft) / 3,
                        false,
                        true
                    );
                    //bottom extrusion
                    HouseWall(
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + (upperLeft - lowerLeft) / 3,
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude
                            + (upperLeft - lowerLeft) / 3
                    ); //right side
                    HouseWall(
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude
                            + (upperLeft - lowerLeft) / 3,
                        heightVector
                            + upperLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude
                            - (upperLeft - lowerLeft) / 3,
                        false,
                        true
                    );
                    //top extrusion
                    HouseWall(
                        heightVector
                            + upperLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude
                            - (upperLeft - lowerLeft) / 3,
                        heightVector
                            + upperLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            - (upperLeft - lowerLeft) / 3
                    ); //left side
                    HouseWall(
                        heightVector
                            + upperLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            - (upperLeft - lowerLeft) / 3,
                        heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector,
                        false,
                        true
                    );

                    MiddleBuffer(
                        heightVector + lowerLeft + courtYardEdgeVector,
                        heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector,
                        heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector,
                        heightVector + upperLeft + courtYardEdgeVector
                    );

                    MiddleBuffer(
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + (upperLeft - lowerLeft) / 3,
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + (upperLeft - lowerLeft) / 3
                            + coveredAreaVector / coveredAreaVector.magnitude,
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + 2 * (upperLeft - lowerLeft) / 3
                            + coveredAreaVector / coveredAreaVector.magnitude,
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + 2 * (upperLeft - lowerLeft) / 3
                    );

                    if (i == 1)
                    {
                        Vector3 floorHeight = heightVector;
                        floorHeight.y -= 3;
                        MiddleBuffer(
                            floorHeight
                                + lowerLeft
                                + courtYardEdgeVector
                                + coveredAreaVector
                                + (upperLeft - lowerLeft) / 3,
                            floorHeight
                                + lowerLeft
                                + courtYardEdgeVector
                                + coveredAreaVector
                                + (upperLeft - lowerLeft) / 3
                                + coveredAreaVector / coveredAreaVector.magnitude,
                            floorHeight
                                + lowerLeft
                                + courtYardEdgeVector
                                + coveredAreaVector
                                + 2 * (upperLeft - lowerLeft) / 3
                                + coveredAreaVector / coveredAreaVector.magnitude,
                            floorHeight
                                + lowerLeft
                                + courtYardEdgeVector
                                + coveredAreaVector
                                + 2 * (upperLeft - lowerLeft) / 3
                        );

                        Vector3 supportStartPoint =
                            heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + (upperLeft - lowerLeft) / 3
                            + 0.9f * coveredAreaVector / coveredAreaVector.magnitude
                            - Vector3.up * 0.2f
                            + Vector3.forward * 0.15f
                            - Vector3.right * 0.1f;
                        Vector3 supportEndPoint =
                            heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + (upperLeft - lowerLeft) / 3
                            - Vector3.up * 1.4f
                            + Vector3.forward * 0.15f
                            + Vector3.right * 0.25f;
                        MakeElongatedCube(supportStartPoint, supportEndPoint, 0.2f, true);

                        supportStartPoint =
                            heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + 2 * (upperLeft - lowerLeft) / 3
                            + 0.9f * coveredAreaVector / coveredAreaVector.magnitude
                            - Vector3.up * 0.2f
                            - Vector3.forward * 0.15f
                            - Vector3.right * 0.1f;
                        supportEndPoint =
                            heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + 2 * (upperLeft - lowerLeft) / 3
                            - Vector3.up * 1.4f
                            - Vector3.forward * 0.15f
                            + Vector3.right * 0.25f;
                        MakeElongatedCube(supportStartPoint, supportEndPoint, 0.2f, true);
                    }
                }
                else
                {
                    //bottom extrusion
                    HouseWall(
                        heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector,
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude
                    ); //right side
                    HouseWall(
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude,
                        heightVector
                            + upperLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude,
                        false,
                        true
                    );
                    //top extrusion
                    HouseWall(
                        heightVector
                            + upperLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude,
                        heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector
                    ); //left side
                    MiddleBuffer(
                        heightVector + lowerLeft + courtYardEdgeVector,
                        heightVector
                            + lowerLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude,
                        heightVector
                            + upperLeft
                            + courtYardEdgeVector
                            + coveredAreaVector
                            + coveredAreaVector / coveredAreaVector.magnitude,
                        heightVector + upperLeft + courtYardEdgeVector
                    );
                    if (i == 1)
                    {
                        Vector3 floorHeight = heightVector;
                        floorHeight.y -= 3;
                        MiddleBuffer(
                            floorHeight + lowerLeft + courtYardEdgeVector,
                            floorHeight
                                + lowerLeft
                                + courtYardEdgeVector
                                + coveredAreaVector
                                + coveredAreaVector / coveredAreaVector.magnitude,
                            floorHeight
                                + upperLeft
                                + courtYardEdgeVector
                                + coveredAreaVector
                                + coveredAreaVector / coveredAreaVector.magnitude,
                            floorHeight + upperLeft + courtYardEdgeVector
                        );
                    }
                }
            }
            else
            {
                //outside wall of the house
                HouseWall(
                    heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector,
                    i == 0,
                    i > 0 || numFloors == 1
                );
                MiddleBuffer(
                    heightVector + lowerLeft + courtYardEdgeVector,
                    heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector
                );
            }

            heightVector.y += 3;
        }

        if (CumbaExists)
        {
            if (edgeSize > windowWidth * 2 * 6)
            {
                Roof(
                    heightVector + lowerLeft + courtYardEdgeVector,
                    heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector
                );

                Roof(
                    heightVector + lowerLeft + courtYardEdgeVector + (upperLeft - lowerLeft) / 3,
                    heightVector
                        + lowerLeft
                        + courtYardEdgeVector
                        + (upperLeft - lowerLeft) / 3
                        + coveredAreaVector
                        + coveredAreaVector / coveredAreaVector.magnitude,
                    heightVector
                        + lowerLeft
                        + courtYardEdgeVector
                        + 2 * (upperLeft - lowerLeft) / 3
                        + coveredAreaVector
                        + coveredAreaVector / coveredAreaVector.magnitude,
                    heightVector + lowerLeft + courtYardEdgeVector + 2 * (upperLeft - lowerLeft) / 3
                );
            }
            else
            {
                Roof(
                    heightVector + lowerLeft + courtYardEdgeVector,
                    heightVector
                        + lowerLeft
                        + courtYardEdgeVector
                        + coveredAreaVector
                        + coveredAreaVector / coveredAreaVector.magnitude,
                    heightVector
                        + upperLeft
                        + courtYardEdgeVector
                        + coveredAreaVector
                        + coveredAreaVector / coveredAreaVector.magnitude,
                    heightVector + upperLeft + courtYardEdgeVector
                );
            }
        }
        else
        {
            if (UnityEngine.Random.value < 0.5f)
            {
                Roof(
                    heightVector + lowerLeft + courtYardEdgeVector,
                    heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector
                );
            }
            else
            {
                Roof2(
                    heightVector + lowerLeft + courtYardEdgeVector,
                    heightVector + lowerLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector + coveredAreaVector,
                    heightVector + upperLeft + courtYardEdgeVector
                );
            }
        }

        //for each floor generate walls
        //buffer
        //walls including door
        //for each extra floor
        //buffer
        //walls with windows (maybe extrusion (cumba))
        //buffer
        //generate roof
        //Roof();
    }

    bool randomCosts = false;

    private float FindBestOrientation(float[] privacyCosts, float[] sceneryScores)
    {
        GameObject cityBuilder = GameObject.Find("CityBuilder");
        ProceduralCityGenerator pcg = cityBuilder.GetComponent<ProceduralCityGenerator>();

        float[] totalCosts = new float[privacyCosts.Length];
        for (int i = 0; i < privacyCosts.Length; i++)
        {
            float angleI = i * (360 / privacyCosts.Length);
            //calculate the cost of each orientation;
            for (int j = 0; j < privacyCosts.Length; j++)
            {
                //check front side
                float angleJ = j * (360 / privacyCosts.Length);
                float weight = Mathf.Cos(Mathf.Deg2Rad * (angleI - angleJ));
                if (weight > 0)
                    totalCosts[i] +=
                        weight * privacyCosts[j] * pcg.Privacy
                        - weight * sceneryScores[i] * pcg.Beauty;

                //check back side
                weight = Mathf.Cos(Mathf.Deg2Rad * (angleI - angleJ + 180));
                if (weight > 0)
                    totalCosts[i] +=
                        (
                            weight * privacyCosts[j] * pcg.Privacy
                            - weight * sceneryScores[i] * pcg.Beauty
                        ) * (courtyardArea > 5 ? 0.5f : 0.9f);

                if (randomCosts)
                    totalCosts[i] = UnityEngine.Random.value;
            }
        }

        float minCost = float.PositiveInfinity;
        float bestIndex = 0;
        for (int i = 0; i < privacyCosts.Length; i++)
        {
            if (totalCosts[i] < minCost)
            {
                minCost = totalCosts[i];
                bestIndex = i;
            }
        }
        return bestIndex * (360 / privacyCosts.Length);
    }

    /*
    //returns an array of costs
    private float[] CheckPrivacyCost(Vector3 position)
    {
        float[] costs = new float[36];
        foreach( GameObject go in GameObject.FindGameObjectsWithTag("PrivateRegion"))
        {
            Vector3 dif = go.transform.position - position;
            if (dif.magnitude*ProceduralCityGenerator.ScaleFactor < viewPrivacyRadius)
            {
                Vector3 direction = dif;
                direction.Normalize();
                float cosAngle = Vector3.Dot(go.transform.forward, direction);
                if(cosAngle > 0)
                {
                    //make ray casting
                    Ray r = new Ray(position, direction);
                    RaycastHit hinfo;
                    Physics.Raycast(r, out hinfo, viewPrivacyRadius / ProceduralCityGenerator.ScaleFactor);
                    if (Mathf.Abs(hinfo.distance - dif.magnitude) < 0.5f)
                    {
                        float cost = go.transform.localScale.sqrMagnitude * cosAngle/dif.magnitude;
                        //Debug.DrawLine(go.transform.position, position, new Color(cosAngle, cosAngle, 1 - cosAngle), 500);
                        //Debug.Log(cosAngle);
                        Vector3 dirPlanar = direction;
                        dirPlanar.y = 0;
                        dirPlanar.Normalize();
                        float angle = Vector3.SignedAngle(Vector3.forward, dirPlanar, Vector3.up);
                        costs[((int)(angle / (360F/costs.Length))+ costs.Length) % costs.Length] += cost;

                    }

                }
            }
        }
        return costs;
    }*/

    private void MiddleBuffer(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
    {
        GameObject middleWall = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //adjustTexture
        Renderer middleWallRenderer = middleWall.GetComponent<Renderer>();
        middleWallRenderer.material.SetTexture("_MainTex", houseWallTexture);
        middleWallRenderer.material.SetTextureScale("_MainTex", new Vector2(0.1f, 0.1f));
        middleWallRenderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        middleWallRenderer.material.SetColor("_Color", Color.white * colorDarkening * 0.7f);

        middleWall.transform.SetParent(model.transform, false);
        middleWall.transform.localScale = new Vector3(
            0.3f,
            (br - bl).magnitude,
            (tr - br).magnitude
        );
        middleWall.transform.localPosition =
            (bl + br + tr + tl) / 4f + new Vector3(0, houseWallHeight + 0.15f, 0);
        middleWall.transform.Rotate(Vector3.forward, 90);
    }

    private void BottomBuffer(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
    {
        GameObject bottomWall = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //adjustTexture
        Renderer middleWallRenderer = bottomWall.GetComponent<Renderer>();
        middleWallRenderer.material.SetTexture("_MainTex", stoneWallTexture);
        middleWallRenderer.material.SetTextureScale(
            "_MainTex",
            new Vector2((br - bl).magnitude / 5, 2.5f)
        );
        middleWallRenderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        middleWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        bottomWall.transform.SetParent(model.transform, false);
        bottomWall.transform.localScale = new Vector3((br - bl).magnitude, 4f, (tr - br).magnitude);
        bottomWall.transform.localPosition = (bl + br + tr + tl) / 4f + new Vector3(0, -2, 0);
    }

    private void HouseWall(Vector3 from, Vector3 to, bool door = false, bool windows = false)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        if (!door && !windows)
        {
            GameObject houseWall = GameObject.CreatePrimitive(PrimitiveType.Cube);

            //adjustTexture
            Renderer houseWallRenderer = houseWall.GetComponent<Renderer>();
            if (colorStyle == 1)
            {
                houseWallRenderer.material.SetTexture("_MainTex", houseWallTexture);
                houseWallRenderer.material.SetTextureScale(
                    "_MainTex",
                    new Vector2((to - from).magnitude / 4f, 1)
                );
                houseWallRenderer.material.SetTextureOffset(
                    "_MainTex",
                    new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
                );
                houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
            }
            else
            {
                houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
            }

            houseWall.transform.SetParent(model.transform, false);
            houseWall.transform.localScale = new Vector3(
                (to - from).magnitude,
                houseWallHeight,
                houseWallThickness
            );
            houseWall.transform.localPosition =
                (from + to + towardsIn * houseWallThickness + Vector3.up * houseWallHeight) / 2f;
            houseWall.transform.Rotate(
                Vector3.up,
                Vector3.SignedAngle(dir, Vector3.right, Vector3.up)
            );
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
            int windowsCount = (int)(
                (sideLength - houseWallThickness * 2)
                / (windowWidth * ProceduralCityGenerator.GoldenRatio)
            );
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
        underWindow.transform.localScale = new Vector3(
            (to - from).magnitude,
            houseWallHeight / 3,
            houseWallThickness
        );

        //adjustTexture
        Renderer houseWallRenderer = underWindow.GetComponent<Renderer>();
        if (colorStyle == 1)
        {
            houseWallRenderer.material.SetTexture("_MainTex", houseWallTexture);
            houseWallRenderer.material.SetTextureScale(
                "_MainTex",
                new Vector2((to - from).magnitude / 4f, 0.3f)
            );
            houseWallRenderer.material.SetTextureOffset(
                "_MainTex",
                new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
            );
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
        }
        else
        {
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
        }

        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);
        underWindow.transform.localPosition =
            (from + to + towardsIn * houseWallThickness + Vector3.up * houseWallHeight / 3f) / 2f;
        underWindow.transform.Rotate(
            Vector3.up,
            Vector3.SignedAngle(dir, Vector3.right, Vector3.up)
        );

        GameObject overWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overWindow.transform.SetParent(model.transform, false);
        overWindow.transform.localScale = new Vector3(
            (to - from).magnitude,
            houseWallHeight / 6,
            houseWallThickness
        );

        //adjustTexture
        houseWallRenderer = overWindow.GetComponent<Renderer>();
        if (colorStyle == 1)
        {
            houseWallRenderer.material.SetTexture("_MainTex", houseWallTexture);
            houseWallRenderer.material.SetTextureScale(
                "_MainTex",
                new Vector2((to - from).magnitude / 4f, 0.3f)
            );
            houseWallRenderer.material.SetTextureOffset(
                "_MainTex",
                new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
            );
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
        }
        else
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        overWindow.transform.localPosition =
            (from + to + towardsIn * houseWallThickness + 22f * Vector3.up * houseWallHeight / 12f)
            / 2f;
        overWindow.transform.Rotate(
            Vector3.up,
            Vector3.SignedAngle(dir, Vector3.right, Vector3.up)
        );

        //make inner window
        Vector3 WindowCornersUpperLeft = from;
        Vector3 WindowCornersLowerLeft = from;
        Vector3 WindowCornersUpperRight = to;
        Vector3 WindowCornersLowerRight = to;

        WindowCornersUpperLeft.y += 5f * houseWallHeight / 6;
        WindowCornersLowerLeft.y += houseWallHeight / 3;
        WindowCornersUpperRight.y += 5f * houseWallHeight / 6;
        WindowCornersLowerRight.y += houseWallHeight / 3;

        AddPrivateRegion(
            WindowCornersUpperLeft,
            WindowCornersLowerLeft,
            WindowCornersUpperRight,
            WindowCornersLowerRight
        );

        MakeElongatedCube(WindowCornersLowerLeft, WindowCornersLowerRight, 0.15f);
        MakeElongatedCube(WindowCornersUpperLeft, WindowCornersUpperRight, 0.15f);
        MakeElongatedCube(WindowCornersUpperLeft, WindowCornersLowerLeft, 0.15f);
        MakeElongatedCube(WindowCornersUpperRight, WindowCornersLowerRight, 0.15f);
        MakeElongatedCube(
            (WindowCornersUpperRight + WindowCornersUpperLeft) / 2,
            (WindowCornersLowerRight + WindowCornersLowerLeft) / 2,
            0.05f
        );
        MakeElongatedCube(
            WindowCornersLowerLeft / 3f + WindowCornersUpperLeft * 2f / 3,
            WindowCornersLowerRight / 3f + WindowCornersUpperRight * 2f / 3f,
            0.05f
        );
        MakeElongatedCube(
            WindowCornersLowerLeft * 2f / 3f + WindowCornersUpperLeft / 3f,
            WindowCornersLowerRight * 2f / 3f + WindowCornersUpperRight / 3f,
            0.05f
        );
    }

    private void MakeElongatedCube(Vector3 from, Vector3 to, float width, bool wooden = false)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //adjustTexture
        Renderer houseWallRenderer = cube.GetComponent<Renderer>();
        if (!wooden)
        {
            if (colorStyle == 1)
                houseWallRenderer.material.SetColor("_Color", Color.white * 0.8f * colorDarkening);
            else
            {
                houseWallRenderer.material.SetColor(
                    "_Color",
                    new Color(117f / 255, 112f / 255, 96f / 255) * colorDarkening
                );
            }
        }
        else
        {
            houseWallRenderer.material.SetTexture("_MainTex", houseWallTexture);
            houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2(0.1f, 0.1f));
            houseWallRenderer.material.SetTextureOffset(
                "_MainTex",
                new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
            );
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening * 0.7f);
        }

        cube.transform.SetParent(model.transform, false);
        cube.transform.localScale = new Vector3((to - from).magnitude + width, width, width);
        cube.transform.localPosition = (from + to) / 2f;
        Vector3 dir = to - from;
        dir.Normalize();
        cube.transform.Rotate(
            Vector3.Cross(Vector3.right, dir),
            Vector3.SignedAngle(Vector3.right, dir, Vector3.Cross(Vector3.right, dir))
        );
    }

    private void Door(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        GameObject overDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overDoor.transform.SetParent(model.transform, false);
        overDoor.transform.localScale = new Vector3(
            (to - from).magnitude,
            houseWallHeight / 6,
            houseWallThickness
        );

        //adjustTexture
        Renderer houseWallRenderer = overDoor.GetComponent<Renderer>();
        if (colorStyle == 1)
        {
            houseWallRenderer.material.SetTexture("_MainTex", houseWallTexture);
            houseWallRenderer.material.SetTextureScale(
                "_MainTex",
                new Vector2((to - from).magnitude / 4f, 0.3f)
            );
            houseWallRenderer.material.SetTextureOffset(
                "_MainTex",
                new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
            );
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
        }
        else
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        overDoor.transform.localPosition =
            (from + to + towardsIn * houseWallThickness + 22f * Vector3.up * houseWallHeight / 12f)
            / 2f;
        overDoor.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));

        //draw the door
        GameObject doorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObject.transform.SetParent(model.transform, false);
        doorObject.transform.localScale = new Vector3(
            (to - from).magnitude,
            5f * houseWallHeight / 6,
            houseWallThickness * 0.25f
        );
        doorObject.transform.localPosition =
            (
                from
                + to
                + towardsIn * houseWallThickness * 0.5f
                + 10f * Vector3.up * houseWallHeight / 12f
            ) / 2f;
        doorObject.transform.Rotate(
            Vector3.up,
            Vector3.SignedAngle(dir, Vector3.right, Vector3.up)
        );
        houseWallRenderer = doorObject.GetComponent<Renderer>();
        houseWallRenderer.material.SetColor(
            "_Color",
            new Color(0.3f, 0.22f, 0.05f) * UnityEngine.Random.value
        );
        MakeElongatedCube(
            from + towardsIn * houseWallThickness * 0.25f,
            to + towardsIn * houseWallThickness * 0.25f,
            houseWallThickness * 0.4f
        );
        MakeElongatedCube(
            from + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * houseWallThickness * 0.25f,
            to + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * houseWallThickness * 0.25f,
            houseWallThickness * 0.4f
        );
        MakeElongatedCube(
            from + towardsIn * houseWallThickness * 0.25f,
            from + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * houseWallThickness * 0.25f,
            houseWallThickness * 0.4f
        );
        MakeElongatedCube(
            to + towardsIn * houseWallThickness * 0.25f,
            to + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * houseWallThickness * 0.25f,
            houseWallThickness * 0.4f
        );
        //put a few steps in front of the door
        BottomBuffer(from - towardsIn * 0.5f, from, to, to - towardsIn * 0.5f);
    }

    private void OuterWall(Vector3 from, Vector3 to, bool door = false)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        if (door)
        {
            OuterWall(from, from + dir * ((to - from).magnitude / 2 - 1 - outerWallThickness));
            float owh = outerWallHeight;
            outerWallHeight = houseWallHeight;
            OuterWall(
                from + dir * ((to - from).magnitude / 2 - 1 - outerWallThickness),
                from + dir * ((to - from).magnitude / 2 - 1)
            );
            OuterWall(
                from + dir * ((to - from).magnitude / 2 + 1),
                from + dir * ((to - from).magnitude / 2 + 1 + outerWallThickness)
            );
            outerWallHeight = owh;
            OuterWall(from + dir * ((to - from).magnitude / 2 + 1), to);
            HouseWall(
                from + dir * ((to - from).magnitude / 2 - 1),
                from + dir * ((to - from).magnitude / 2 + 1)
            );
            return;
        }

        GameObject outerWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outerWall.transform.SetParent(model.transform, false);
        outerWall.transform.localScale = new Vector3(
            (to - from).magnitude,
            outerWallHeight,
            outerWallThickness
        );
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);
        outerWall.transform.localPosition =
            (from + to + towardsIn * outerWallThickness + Vector3.up * outerWallHeight) / 2f;
        outerWall.transform.Rotate(Vector3.up, Vector3.SignedAngle(dir, Vector3.right, Vector3.up));

        //adjustTexture
        Renderer houseWallRenderer = outerWall.GetComponent<Renderer>();
        houseWallRenderer.material.SetTexture("_MainTex", stoneWallTexture);
        houseWallRenderer.material.SetTextureScale(
            "_MainTex",
            new Vector2((to - from).magnitude / 5f, 1.25f)
        );
        houseWallRenderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
    }

    private void Roof(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
    {
        //For determining number of accommodated
        GameObject cityBuilder = GameObject.Find("CityBuilder");
        ProceduralCityGenerator pcg = cityBuilder.GetComponent<ProceduralCityGenerator>();
        float accommodationRate = pcg.GetAccommodationRate();
        if (accommodationRate > 1)
            accommodationRate = 1f;
        if (accommodationRate < 0)
            accommodationRate = 0f;

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
            faces[0] = 0;
            faces[2] = 1;
            faces[1] = 8;
            faces[3] = 1;
            faces[5] = 9;
            faces[4] = 8;
            faces[6] = 2;
            faces[8] = 3;
            faces[7] = 10;
            faces[9] = 4;
            faces[11] = 5;
            faces[10] = 11;
            faces[12] = 5;
            faces[14] = 12;
            faces[13] = 11;
            faces[15] = 6;
            faces[17] = 7;
            faces[16] = 13;
        }
        else
        {
            faces[0] = 0;
            faces[2] = 1;
            faces[1] = 8;
            faces[3] = 2;
            faces[5] = 3;
            faces[4] = 9;
            faces[6] = 3;
            faces[8] = 10;
            faces[7] = 9;
            faces[9] = 4;
            faces[11] = 5;
            faces[10] = 11;
            faces[12] = 6;
            faces[14] = 7;
            faces[13] = 12;
            faces[15] = 7;
            faces[17] = 13;
            faces[16] = 12;
        }
        m.triangles = faces;
        Vector3 n1,
            n2,
            n3,
            n4;
        n1 = -Vector3.Cross(vertices[1] - vertices[0], vertices[8] - vertices[0]);
        n2 = -Vector3.Cross(vertices[3] - vertices[2], vertices[10] - vertices[2]);
        n3 = -Vector3.Cross(vertices[5] - vertices[4], vertices[11] - vertices[4]);
        n4 = -Vector3.Cross(vertices[7] - vertices[6], vertices[13] - vertices[6]);

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
            if (
                (uEdge.magnitude > vEdge.magnitude && (i == 0 || i == 1 || i == 8 || i == 9))
                || (uEdge.magnitude <= vEdge.magnitude && (i == 0 || i == 1 || i == 8))
            )
                texCoords[i] = new Vector2(
                    vertices[i].x - vertices[0].x,
                    vertices[i].z - vertices[0].z
                );
            if (
                (uEdge.magnitude > vEdge.magnitude && (i == 4 || i == 5 || i == 11 || i == 12))
                || (uEdge.magnitude <= vEdge.magnitude && (i == 4 || i == 5 || i == 11))
            )
                texCoords[i] = new Vector2(
                    vertices[5].x - vertices[i].x,
                    vertices[5].z - vertices[i].z
                );
            if (
                (uEdge.magnitude > vEdge.magnitude && (i == 2 || i == 3 || i == 10))
                || (uEdge.magnitude <= vEdge.magnitude && (i == 2 || i == 3 || i == 9 || i == 10))
            )
                texCoords[i] = new Vector2(
                    vertices[5].z - vertices[i].z,
                    vertices[5].x - vertices[i].x
                );
            if (
                (uEdge.magnitude > vEdge.magnitude && (i == 6 || i == 7 || i == 13))
                || (uEdge.magnitude <= vEdge.magnitude && (i == 6 || i == 7 || i == 12 || i == 13))
            )
                texCoords[i] = new Vector2(
                    vertices[i].z - vertices[0].z,
                    vertices[i].x - vertices[0].x
                );
            texCoords[i] *= 0.5f;
        }
        m.uv = texCoords;

        //adjustTexture
        Renderer houseWallRenderer = roof.GetComponent<Renderer>();

        houseWallRenderer.material.SetTexture("_MainTex", roofTexture);
        houseWallRenderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        houseWallRenderer.material.SetColor(
            "_Color",
            Color.white * UnityEngine.Random.Range(0.6f, 1)
        );

        //for visualizing accomodation rate on roofs
        //houseWallRenderer.material.SetColor("_Color", new Color(1f - accommodationRate, 1f - accommodationRate, accommodationRate));

        roof.transform.SetParent(model.transform, false);

        GameObject underRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //adjustTexture
        Renderer middleWallRenderer = underRoof.GetComponent<Renderer>();
        middleWallRenderer.material.SetTexture("_MainTex", houseWallTexture);
        middleWallRenderer.material.SetTextureScale("_MainTex", new Vector2(0.1f, 0.1f));
        middleWallRenderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        middleWallRenderer.material.SetColor("_Color", Color.black);

        underRoof.transform.SetParent(model.transform, false);
        underRoof.transform.localScale = new Vector3(
            0.05f,
            (vertices[1] - vertices[0]).magnitude,
            (vertices[3] - vertices[1]).magnitude
        );
        underRoof.transform.localPosition = (bl + br + tr + tl) / 4f;
        underRoof.transform.Rotate(Vector3.forward, 90);
    }

    private void Roof2(Vector3 obl, Vector3 obr, Vector3 otr, Vector3 otl)
    {
        //adust according to shorter edge
        if ((obr - obl).magnitude < (otl - obl).magnitude)
            Roof2Helper(obl, obr, otr, otl);
        else
            Roof2Helper(obr, otr, otl, obl);
    }

    private void Roof2Helper(Vector3 obl, Vector3 obr, Vector3 otr, Vector3 otl)
    {
        //For determining number of accommodated
        GameObject cityBuilder = GameObject.Find("CityBuilder");
        ProceduralCityGenerator pcg = cityBuilder.GetComponent<ProceduralCityGenerator>();
        float accommodationRate = pcg.GetAccommodationRate();
        if (accommodationRate > 1)
            accommodationRate = 1f;
        if (accommodationRate < 0)
            accommodationRate = 0f;

        Vector3 bl =
            obl
            - 0.5f * (obr - obl) / (obr - obl).magnitude
            - 0.5f * (otl - obl) / (otl - obl).magnitude;
        Vector3 br =
            obr
            + 0.5f * (obr - obl) / (obr - obl).magnitude
            - 0.5f * (otl - obl) / (otl - obl).magnitude;
        Vector3 tr =
            otr
            + 0.5f * (obr - obl) / (obr - obl).magnitude
            + 0.5f * (otl - obl) / (otl - obl).magnitude;
        Vector3 tl =
            otl
            - 0.5f * (obr - obl) / (obr - obl).magnitude
            + 0.5f * (otl - obl) / (otl - obl).magnitude;
        GameObject roof = new GameObject("roof");
        roof.transform.SetParent(model.transform, false);
        Vector3 uEdge = br - bl; //yatay / egik
        Vector3 vEdge = tl - bl; //yukarı / duz kenar

        Vector3 uDir = uEdge / uEdge.magnitude;
        Vector3 vDir = vEdge / vEdge.magnitude;
        float roofAngle = UnityEngine.Random.Range(23f, 30f) * Mathf.Deg2Rad;
        float roofHeight = Mathf.Tan(roofAngle) * (uEdge.magnitude - 1) * 0.5f;
        float roofUEdgeLength = uEdge.magnitude * 0.5f / Mathf.Cos(roofAngle);

        //make two cubes textured with the roof texture
        GameObject roofLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject roofRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofLeft.transform.SetParent(roof.transform, false);
        roofRight.transform.SetParent(roof.transform, false);
        roofLeft.name = "roof left";
        roofRight.name = "roof right";

        roofLeft.transform.localScale = new Vector3(
            houseWallThickness * 0.25f,
            roofUEdgeLength,
            vEdge.magnitude
        );
        roofRight.transform.localScale = new Vector3(
            houseWallThickness * 0.25f,
            roofUEdgeLength,
            vEdge.magnitude
        );

        roofLeft.transform.Rotate(vDir, (roofAngle - Mathf.PI / 2f) * Mathf.Rad2Deg);
        roofRight.transform.Rotate(vDir, (Mathf.PI / 2f - roofAngle) * Mathf.Rad2Deg);
        roofLeft.transform.Rotate(
            Vector3.up,
            Vector3.SignedAngle(vDir, Vector3.forward, Vector3.up)
        );
        roofRight.transform.Rotate(
            Vector3.up,
            Vector3.SignedAngle(vDir, Vector3.forward, Vector3.up)
        );
        roofLeft.transform.localPosition =
            bl
            + vEdge * 0.5f
            + uEdge * 0.25f
            + uDir * Mathf.Tan(roofAngle) * houseWallThickness * 0.125f
            + Vector3.up * (roofHeight - 0.25f * uEdge.magnitude * Mathf.Tan(roofAngle));
        roofRight.transform.localPosition =
            bl
            + vEdge * 0.5f
            + uEdge * 0.75f
            - uDir * Mathf.Tan(roofAngle) * houseWallThickness * 0.125f
            + Vector3.up * (roofHeight - 0.25f * uEdge.magnitude * Mathf.Tan(roofAngle));

        float roofDarkening = UnityEngine.Random.Range(0.6f, 1.0f);
        Renderer roofLeftRenderer = roofLeft.GetComponent<Renderer>();
        roofLeftRenderer.material.SetTexture("_MainTex", roofTexture);
        roofLeftRenderer.material.SetTextureScale(
            "_MainTex",
            new Vector2(vEdge.magnitude, roofUEdgeLength)
        );
        roofLeftRenderer.material.SetColor("_Color", Color.white * roofDarkening);

        //for visualizing accomodation rate on roofs
        //roofLeftRenderer.material.SetColor("_Color", new Color(1f - accommodationRate, 1f - accommodationRate, accommodationRate));

        roofLeftRenderer = roofRight.GetComponent<Renderer>();
        roofLeftRenderer.material.SetTexture("_MainTex", roofTexture);
        roofLeftRenderer.material.SetTextureScale(
            "_MainTex",
            new Vector2(vEdge.magnitude, roofUEdgeLength)
        );
        roofLeftRenderer.material.SetColor("_Color", Color.white * roofDarkening);

        //for visualizing accomodation rate on roofs
        //roofLeftRenderer.material.SetColor("_Color", new Color(1f - accommodationRate, 1f - accommodationRate, accommodationRate));


        //Make two sides for the shorter edges
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
        primitive.transform.SetParent(model.transform, true);
        primitive.SetActive(false);
        Material diffuse = primitive.GetComponent<MeshRenderer>().sharedMaterial;

        GameObject roofSides = new GameObject();
        roofSides.transform.SetParent(roof.transform, false);
        roofSides.AddComponent<MeshFilter>();
        roofSides.AddComponent<MeshRenderer>();
        roofSides.GetComponent<Renderer>().sharedMaterial = diffuse;

        Mesh m = roofSides.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = new Vector3[6];
        Vector2[] texCoords = new Vector2[6];
        vertices[0] = obr;
        vertices[1] = obl;
        vertices[2] = vertices[0] * 0.5f + vertices[1] * 0.5f + Vector3.up * roofHeight;
        vertices[3] = otl;
        vertices[4] = otr;
        vertices[5] = vertices[3] * 0.5f + vertices[4] * 0.5f + Vector3.up * roofHeight;
        m.vertices = vertices;

        int[] faces = new int[6];
        faces[0] = 0;
        faces[1] = 1;
        faces[2] = 2;
        faces[3] = 3;
        faces[4] = 4;
        faces[5] = 5;
        m.triangles = faces;

        Vector3[] normals = new Vector3[vertices.Length];
        normals[0] = -vDir;
        normals[1] = -vDir;
        normals[2] = -vDir;
        normals[3] = vDir;
        normals[4] = vDir;
        normals[5] = vDir;
        m.normals = normals;

        texCoords[0] = new Vector2(0, 0);
        texCoords[1] = new Vector2(uEdge.magnitude - 1, 0);
        texCoords[2] = new Vector2(uEdge.magnitude / 2 - 0.5f, roofHeight);
        texCoords[4] = new Vector2(0, 0);
        texCoords[3] = new Vector2(uEdge.magnitude - 1, 0);
        texCoords[5] = new Vector2(uEdge.magnitude / 2 - 0.5f, roofHeight);
        m.uv = texCoords;

        //adjustTexture

        Renderer houseWallRenderer = roofSides.GetComponent<Renderer>();
        if (colorStyle == 1)
        {
            houseWallRenderer.material.SetTexture("_MainTex", houseWallTexture);
            houseWallRenderer.material.SetTextureScale("_MainTex", new Vector2(0.25f, 1));
            houseWallRenderer.material.SetTextureOffset(
                "_MainTex",
                new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
            );
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
        }
        else
        {
            houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);
        }

        roof.transform.SetParent(model.transform, false);
    }
}
