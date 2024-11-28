using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ProceduralCityGenerator : MonoBehaviour
{
    public Terrain CityTerrain;

    public float BaseHeight = 0F;
    public float Latitude = 40f;

    public int InitialPopulation = 300;

    [Space(10)]
    [Header("Weights of Principles")]
    [Range(0, 1)]
    public float Privacy = 0.5f;

    [Range(0, 1)]
    public float Security = 0.5f;

    [Range(0, 1)]
    public float Sustainability = 0.5f;

    [Range(0, 1)]
    public float Social_Life = 0.5f;

    [Range(0, 1)]
    public float Economy = 0.5f;

    [Range(0, 1)]
    public float Beauty = 0.5f;

    [Header("Standard Textures")]
    public Texture StoneWallTexture;
    public Texture WoodWallTexture;
    public Texture RoofTexture;
    public Texture DoorTexture;
    public Texture WindowTexture;
    public Texture MiddleTexture;

    public GameObject tree1;
    public GameObject tree2;
    public GameObject tree3;
    GameObject trees,
        sceneryRegions;

    private GameObject roads;

    float maxHeight = 0;
    int maxHeightX = 0;
    int maxHeightY = 0;

    float elapsed = 0;
    float privacyCalculationTime = 0;
    float sceneryCalculationTime = 0;
    float sunExposureCalculationTime = 0;
    int ballCount = 0;
    int hmw,
        hmh; //heightmap width and height
    Vector2 cityCore;

    private bool[,,,] VisibilityTable;
    public float[,] VisibilityScores;
    public float[,] ClimateScores;
    public float[,] CityCoreScores;
    public float[,] WaterProximityScores;
    public float[,] AccessibilityInScores;
    public float[,] AccessibilityOutScores;
    public float[,] AccessibilityWithinScores;
    private float[,,,] AccessibilityTable;
    private Vector2Int[,,,] AccessPath;

    //tables for inside the city
    public float CityRadius = 0;
    private float[,,,] CityAccessibilityTable;
    private Vector2Int[,,,] CityAccessPath;
    public float[,] CityAccessibilityScores;
    private int CityAccessibilityApproach = 2; //1 Floyd, 2 Euler distance
    private float[,] CityCoreAccessibilityTable;
    private Vector2Int[,] CityCoreAccessibilityPath;
    public float[,] CityWaterProximityTable;
    public float[,] EuclideanWaterProximityTable;

    public enum Occupation
    {
        Free,
        Walkable,
        Buildable,
        Occupied,
    };

    public Occupation[,] CityOccupation; //0 Free, 1 Walkable, 2 Buildable
    private float[,,] CityAlphas;
    private ushort[,] Roads; //1 east, 2northeast, 4north, 8northwest, 16w, 32sw, 64s, 128se

    const int ScoreSamplingScale = 16;
    public static float ScaleFactor = 8f; //one unit corresponds to how many meters Goceri icin harita indirilirken 8
    public static float GoldenRatio = 1.618034f;
    const int SandChannel = 0;
    const int TreeChannel = 1;
    const int WaterChannel = 2;
    const int RockChannel = 3;
    private float overlap = 0.2f;

    private const int WaterNearbyThreshold = 100; //meters, highest proximity score, score = 1 until that
    private const int WaterFarAwayThreshold = 1000; //meters, lowest proximity score, score = 0 after that
    private const float ViewPrivacyRadius = 50; //meters
    private float CostOf1mStraight = -1f; //assigned in accessibility scores

    private float CoreSideLength = 1;

    //private float m2perperson = 60f;
    private float m2perperson = 150f;
    private int ConstructionState = 0; //0 means trying to find city core 1 means building initial city, 2 means playing

    public Vector3 CityCorePosition;
    public bool FirstTime = true;
    public bool AvoidWater = false;
    Vector2Int CityBoundaryMin;
    Vector2Int CityBoundaryMax;
    public int cw,
        ch;

    PCHamam hamam;
    PCMadrasah madrasah;

    Dictionary<Vector3, float> sunExposureValues;
    Dictionary<Vector3, float> sceneryValues;

    void Start()
    {
        // Set culture to "en-US" for consistent formatting
        System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");

        // Get terrain heightmap resouluion
        hmw = CityTerrain.terrainData.heightmapResolution;
        hmh = CityTerrain.terrainData.heightmapResolution;

        FindHighestPoint();
        if (FirstTime)
        {
            ClearMap();
            for (int i = 0; i < 5; i++)
            {
                MakeRockyArea(
                    new Vector2Int(
                        UnityEngine.Random.Range(20, hmw - 20),
                        UnityEngine.Random.Range(20, hmw - 20)
                    ),
                    20 + UnityEngine.Random.value * 20
                );
            }
        }

        float cbeginning = Time.realtimeSinceStartup;
        FindCityCore();
        ConstructionState = 1;

        Debug.Log(
            "City core is determined as: "
                + CityCorePosition
                + " in "
                + (Time.realtimeSinceStartup - cbeginning)
        );

        var pc = GameObject.Find("Player");
        pc.transform.position = CityCorePosition + 5 * Vector3.up / ScaleFactor;
    }

    int failedAttempts = 0;
    int accommodated = 0;
    int numCities = 0;
    int targetNumCities = 3;

    // Update is called once per frame
    [Obsolete]
    void Update()
    {
        // if (ConstructionState == 0 && Input.GetKeyDown("space"))
        // {
        //     ConstructionState++;
        // }
        // if (ConstructionState == 0 && Input.GetKeyDown("v"))
        // {
        //     RecalculateCosts();
        //     VisualizeCosts(CityCoreScores);
        // }

        if (ConstructionState == 1)
        {
            CalculateEuclideanWaterProximity();
            sceneryRegions = new GameObject("Scenery Regions");
            sunExposureValues = new Dictionary<Vector3, float>();
            sceneryValues = new Dictionary<Vector3, float>();
            AddRiverSceneryRegions();

            InitiateCity();
            GameObject playerObj = GameObject.Find("ThirdPersonController");
            if (playerObj != null)
            {
                playerObj.transform.position = CityCorePosition + 5 * Vector3.up / ScaleFactor;
                playerObj.transform.localScale = new Vector3(1, 1, 1) / ScaleFactor;
            }
            ConstructionState++;
        }
        if (ConstructionState == 2)
        {
            elapsed += Time.deltaTime;
            if (elapsed > 1f && ballCount < 0)
            {
                GameObject ball2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball2.transform.position = new Vector3(maxHeightX, maxHeight + 2, maxHeightY);
                ball2.transform.localScale = new Vector3(4, 4, 4);
                Rigidbody ballRigidBody = ball2.AddComponent<Rigidbody>(); // Add the rigidbody.
                ballRigidBody.mass = 5; // Set the GO's mass to 5 via the Rigidbody.
                elapsed -= 1f;
                ballCount++;
            }
            //madrasah.updateTexture();

            float beginning = Time.realtimeSinceStartup;
            bool b;
            if (accommodated < InitialPopulation && failedAttempts < 10)
            {
                //int accommodates = (int)Mathf.Min(UnityEngine.Random.Range(3, 25), UnityEngine.Random.Range(3, 25));
                int accommodates = (int)UnityEngine.Random.Range(3, 30);
                b = BuildHouse(accommodates);
                if (b)
                {
                    accommodated += accommodates;
                    //generate trees close to river
                    FindWastedSpace(true);
                    //GenerateCityTrees(3);
                    CalculateDistanceToCityCore();
                    Debug.Log("house built in: " + (Time.realtimeSinceStartup - beginning));
                    beginning = Time.realtimeSinceStartup;
                    failedAttempts = 0;

                    Debug.Log(
                        ""
                            + accommodated
                            + "/"
                            + InitialPopulation
                            + " times for: all costs: "
                            + buildingCostCalculationTime
                            + "\tprivacy: "
                            + privacyCalculationTime
                            + "\tscenery: "
                            + sceneryCalculationTime
                            + "\tsunexposure: "
                            + sunExposureCalculationTime
                    );
                }
                //b = BuildHouse(Mathf.Sqrt(120f + (80f * UnityEngine.Random.value)));
                if (!b)
                {
                    failedAttempts++;
                }
            }
            else
                ConstructionState++;
        }
        if (ConstructionState == 3)
        {
            new PCWalls(CityBoundaryMin, CityBoundaryMax, StoneWallTexture);
            ConstructionState++;
        }
        if (ConstructionState == 4)
        {
            GenerateCityTrees(200);
            ConstructionState++;
            numCities++;
            if (numCities < targetNumCities)
            {
                ConstructionState = 0;
                accommodated = 0;
            }
        }
    }

    //Generates public buildings and accommodation for the initial population
    [Obsolete]
    void InitiateCity()
    {
        //find the boundary of the city
        CityRadius = 1.8f * (float)Math.Sqrt(InitialPopulation * m2perperson / Mathf.PI); //in meters
        CityBoundaryMin = new Vector2Int(
            Mathf.FloorToInt(CityCorePosition.x - CityRadius / ScaleFactor),
            Mathf.FloorToInt(CityCorePosition.z - CityRadius / ScaleFactor)
        );
        CityBoundaryMax = new Vector2Int(
            Mathf.CeilToInt(CityCorePosition.x + CityRadius / ScaleFactor),
            Mathf.CeilToInt(CityCorePosition.z + CityRadius / ScaleFactor)
        );
        if (CityBoundaryMin.x < 0)
            CityBoundaryMin.x = 0;
        if (CityBoundaryMin.y < 0)
            CityBoundaryMin.y = 0;
        if (CityBoundaryMax.x > hmw - 1)
            CityBoundaryMax.x = hmw - 1;
        if (CityBoundaryMax.y > hmh - 1)
            CityBoundaryMax.y = hmh - 1;
        cw = CityBoundaryMax.x - CityBoundaryMin.x + 1;
        ch = CityBoundaryMax.y - CityBoundaryMin.y + 1;

        //calculate necessary cost
        Roads = new ushort[cw, ch];
        InitiateCityOcccupation();

        InitiateInformationObjects();

        float beginning = Time.realtimeSinceStartup;
        CalculateDistanceToCityCore();
        Debug.Log("dist to city core calculated in: " + (Time.realtimeSinceStartup - beginning));

        beginning = Time.realtimeSinceStartup;
        CalculateAccessibilityWithinCity();
        Debug.Log("accessibility table filled in: " + (Time.realtimeSinceStartup - beginning));

        beginning = Time.realtimeSinceStartup;
        CalculateWaterProximityWithinCity();
        Debug.Log("water proximity table filled in: " + (Time.realtimeSinceStartup - beginning));

        //initiate roads parent
        roads = new GameObject("roads");

        //initiate tree parent object
        trees = new GameObject("trees");

        //build Hamam, close to water source
        BuildHamam();
        FindWastedSpace(true);

        //build Mosque, high visibility, size according to population
        BuildMosque();
        FindWastedSpace(true);

        //build Medrese, city center
        BuildMadrasah();
        FindWastedSpace(true);

        //build Medrese, city center
        BuildCarsi();
        FindWastedSpace(true);

        //generate trees close to river
        beginning = Time.realtimeSinceStartup;
        //GenerateInitialTrees(2000);
        Debug.Log("dist to add initial trees: " + (Time.realtimeSinceStartup - beginning));

        //generate the initial road network
        beginning = Time.realtimeSinceStartup;
        CalculateDistanceToCityCore();
        Debug.Log("dist to city core table filled in: " + (Time.realtimeSinceStartup - beginning));

        MakeMainRoads(4);
        //MakeRoadGrids(40);
        CalculateDistanceToCityCore();
        //Make roads for all initial buildings

        LinkedList<Vector3> pathList = MakeCityPath(CityBoundaryMin + hamam.entranceGrid);
        MakeRoad(pathList);
    }

    private void FindWastedSpace(bool convertToWalkable)
    {
        //check 8 way neighborhood
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                if (CityOccupation[i, j] == Occupation.Occupied)
                {
                    //find the cost
                    Vector3 from1 = new Vector3(
                        i + CityBoundaryMin.x - 0.5f,
                        0f,
                        j + CityBoundaryMin.y - 0.5f
                    );
                    Vector3 from2 = new Vector3(
                        i + CityBoundaryMin.x - 0.5f,
                        0f,
                        j + CityBoundaryMin.y + 0.5f
                    );
                    Vector3 from3 = new Vector3(
                        i + CityBoundaryMin.x + 0.5f,
                        0f,
                        j + CityBoundaryMin.y - 0.5f
                    );
                    Vector3 from4 = new Vector3(
                        i + CityBoundaryMin.x + 0.5f,
                        0f,
                        j + CityBoundaryMin.y + 0.5f
                    );
                    Vector3 from5 = new Vector3(i + CityBoundaryMin.x, 0f, j + CityBoundaryMin.y);

                    float fromHeight1 = CityTerrain.SampleHeight(from1);
                    from1.y = fromHeight1 + 0.01f;
                    float fromHeight2 = CityTerrain.SampleHeight(from2);
                    from2.y = fromHeight2 + 0.01f;
                    float fromHeight3 = CityTerrain.SampleHeight(from3);
                    from3.y = fromHeight3 + 0.01f;
                    float fromHeight4 = CityTerrain.SampleHeight(from4);
                    from4.y = fromHeight4 + 0.01f;
                    float fromHeight5 = CityTerrain.SampleHeight(from5);
                    from5.y = fromHeight5 + 0.01f;

                    Vector3 from1down = from1 - Vector3.down * 0.02f;
                    Vector3 from2down = from2 - Vector3.down * 0.02f;
                    Vector3 from3down = from3 - Vector3.down * 0.02f;
                    Vector3 from4down = from4 - Vector3.down * 0.02f;
                    Vector3 from5down = from5 - Vector3.down * 0.02f;

                    if (
                        !Physics.Raycast(from1, Vector3.up)
                        && !Physics.Raycast(from2, Vector3.up)
                        && !Physics.Raycast(from3, Vector3.up)
                        && !Physics.Raycast(from4, Vector3.up)
                        && !Physics.Raycast(from5, Vector3.up)
                        && !Physics.Raycast(from1down, Vector3.down)
                        && !Physics.Raycast(from2down, Vector3.down)
                        && !Physics.Raycast(from3down, Vector3.down)
                        && !Physics.Raycast(from4down, Vector3.down)
                        && !Physics.Raycast(from5down, Vector3.down)
                    )
                    {
                        //GameObject riobj = GameObject.Find("ri" + i + j);
                        //Renderer riRenderer = riobj.GetComponent<Renderer>();
                        //riRenderer.material.SetColor("_Color", Color.black);
                        if (convertToWalkable)
                        {
                            if (RoadExists(i, j))
                                CityOccupation[i, j] = Occupation.Walkable;
                            else
                                CityOccupation[i, j] = Occupation.Free;
                        }
                    }
                }
            }
        }
    }

    private void MakeRoadGrids(float seperation)
    {
        int stepx = Mathf.RoundToInt(seperation / ScaleFactor);
        int stepy = Mathf.RoundToInt(seperation / ScaleFactor * (seperation / ScaleFactor) / stepx);
        for (int i = 1; i < cw - 1; i += stepx)
        {
            for (int j = 1; j < ch - 1; j += stepy)
            {
                Debug.Log("making grid between " + i + ", " + j);
                //horizontal
                if (
                    (
                        CityOccupation[i, j] == Occupation.Free
                        || CityOccupation[i, j] == Occupation.Walkable
                    ) && GridInsideCity(i + stepx, j)
                )
                {
                    bool allPossible = true;
                    for (int k = i; k <= i + stepx; k++)
                        if (
                            CityOccupation[k, j] != Occupation.Free
                            && CityOccupation[k, j] != Occupation.Walkable
                        )
                        {
                            allPossible = false;
                            break;
                        }
                    if (allPossible)
                    {
                        Debug.Log("horizontal possible");
                        LinkedList<Vector3> pathList = new LinkedList<Vector3>();
                        for (int k = i; k <= i + stepx; k++)
                        {
                            Vector3 current = new Vector3(
                                k + CityBoundaryMin.x,
                                0,
                                j + CityBoundaryMin.y
                            );
                            current.y = CityTerrain.SampleHeight(current);
                            pathList.AddLast(current);
                            CityOccupation[k, j] = Occupation.Walkable;
                        }
                        MakeRoad(pathList);
                    }
                }
                //vertical
                if (
                    (
                        CityOccupation[i, j] == Occupation.Free
                        || CityOccupation[i, j] == Occupation.Walkable
                    ) && GridInsideCity(i, j + stepy)
                )
                {
                    bool allPossible = true;
                    for (int k = j; k <= j + stepy; k++)
                        if (
                            CityOccupation[i, k] != Occupation.Free
                            && CityOccupation[i, k] != Occupation.Walkable
                        )
                        {
                            allPossible = false;
                            break;
                        }
                    if (allPossible)
                    {
                        Debug.Log("vertical possible");
                        LinkedList<Vector3> pathList = new LinkedList<Vector3>();
                        for (int k = j; k <= j + stepy; k++)
                        {
                            Vector3 current = new Vector3(
                                i + CityBoundaryMin.x,
                                0,
                                k + CityBoundaryMin.y
                            );
                            current.y = CityTerrain.SampleHeight(current);
                            pathList.AddLast(current);
                            CityOccupation[i, k] = Occupation.Walkable;
                        }
                        MakeRoad(pathList);
                    }
                }
            }
        }
    }

    private void MakeRoadGrids2(float seperation)
    {
        int stepx = Mathf.RoundToInt(seperation / ScaleFactor);
        int stepy = Mathf.RoundToInt(seperation / ScaleFactor * (seperation / ScaleFactor) / stepx);
        for (int i = 1; i < cw - 1; i += stepx)
        {
            for (int j = 1; j < ch - 1; j += stepy)
            {
                //horizontal
                if (
                    (
                        CityOccupation[i, j] == Occupation.Free
                        || CityOccupation[i, j] == Occupation.Walkable
                    ) && GridInsideCity(i + stepx, j)
                )
                {
                    //LinkedList<Vector3> pathList = MakeCityPath(CityBoundaryMin + new Vector2Int(minIndex1, 0));
                    //MakeRoad(pathList);

                    bool allPossible = true;
                    for (int k = i; k <= i + stepx; k++)
                        if (
                            CityOccupation[k, j] != Occupation.Free
                            && CityOccupation[k, j] != Occupation.Walkable
                        )
                        {
                            allPossible = false;
                            break;
                        }
                    if (allPossible)
                    {
                        LinkedList<Vector3> pathList = new LinkedList<Vector3>();
                        for (int k = i; k <= i + stepx; k++)
                        {
                            Vector3 current = new Vector3(
                                k + CityBoundaryMin.x,
                                0,
                                j + CityBoundaryMin.y
                            );
                            current.y = CityTerrain.SampleHeight(current);
                            pathList.AddLast(current);
                            CityOccupation[k, j] = Occupation.Walkable;
                        }
                        MakeRoad(pathList);
                    }
                }
                //vertical
                if (
                    (
                        CityOccupation[i, j] == Occupation.Free
                        || CityOccupation[i, j] == Occupation.Walkable
                    ) && GridInsideCity(i, j + stepy)
                )
                {
                    bool allPossible = true;
                    for (int k = j; k <= j + stepy; k++)
                        if (
                            CityOccupation[i, k] != Occupation.Free
                            && CityOccupation[i, k] != Occupation.Walkable
                        )
                        {
                            allPossible = false;
                            break;
                        }
                    if (allPossible)
                    {
                        LinkedList<Vector3> pathList = new LinkedList<Vector3>();
                        for (int k = j; k <= j + stepy; k++)
                        {
                            Vector3 current = new Vector3(
                                i + CityBoundaryMin.x,
                                0,
                                k + CityBoundaryMin.y
                            );
                            current.y = CityTerrain.SampleHeight(current);
                            pathList.AddLast(current);
                            CityOccupation[i, k] = Occupation.Walkable;
                        }
                        MakeRoad(pathList);
                    }
                }
            }
        }
    }

    private void MakeMainRoads(int numRoads)
    {
        if (numRoads > 4)
        {
            numRoads = 4;
            Debug.Log("At most 4 main roads!");
        }
        //make roads to city center from boundary
        float minCost1,
            minCost2,
            minCost3,
            minCost4;
        int minIndex1,
            minIndex2,
            minIndex3,
            minIndex4;

        minCost1 = minCost2 = minCost3 = minCost4 = float.PositiveInfinity;
        minIndex1 = minIndex2 = minIndex3 = minIndex4 = 0;
        for (int i = 1; i < cw - 1; i++)
        {
            if (CityCoreAccessibilityTable[i, 0] < minCost1)
            {
                minCost1 = CityCoreAccessibilityTable[i, 0];
                minIndex1 = i;
            }
            if (CityCoreAccessibilityTable[i, ch - 1] < minCost2)
            {
                minCost2 = CityCoreAccessibilityTable[i, ch - 1];
                minIndex2 = i;
            }
            if (CityCoreAccessibilityTable[0, i] < minCost3)
            {
                minCost3 = CityCoreAccessibilityTable[0, i];
                minIndex3 = i;
            }
            if (CityCoreAccessibilityTable[cw - 1, i] < minCost4)
            {
                minCost4 = CityCoreAccessibilityTable[cw - 1, i];
                minIndex4 = i;
            }
        }
        int order1 = 0;
        if (minCost1 > minCost2)
            order1++;
        if (minCost1 > minCost3)
            order1++;
        if (minCost1 > minCost4)
            order1++;

        int order2 = 0;
        if (minCost2 > minCost1)
            order2++;
        if (minCost2 > minCost3)
            order2++;
        if (minCost2 > minCost4)
            order2++;

        int order3 = 0;
        if (minCost3 > minCost1)
            order3++;
        if (minCost3 > minCost2)
            order3++;
        if (minCost3 > minCost4)
            order3++;

        int order4 = 0;
        if (minCost4 > minCost1)
            order4++;
        if (minCost4 > minCost2)
            order4++;
        if (minCost4 > minCost3)
            order4++;

        if (order1 < numRoads)
        {
            LinkedList<Vector3> pathList = MakeCityPath(
                CityBoundaryMin + new Vector2Int(minIndex1, 0)
            );
            MakeRoad(pathList);
        }
        if (order2 < numRoads)
        {
            LinkedList<Vector3> pathList = MakeCityPath(
                CityBoundaryMin + new Vector2Int(minIndex2, ch - 1)
            );
            MakeRoad(pathList);
        }
        if (order3 < numRoads)
        {
            LinkedList<Vector3> pathList = MakeCityPath(
                CityBoundaryMin + new Vector2Int(0, minIndex3)
            );
            MakeRoad(pathList);
        }
        if (order4 < numRoads)
        {
            LinkedList<Vector3> pathList = MakeCityPath(
                CityBoundaryMin + new Vector2Int(cw - 1, minIndex4)
            );
            MakeRoad(pathList);
        }
    }

    private void GenerateInitialTrees(int approximateNumTrees)
    {
        if (tree1 == null && tree2 == null && tree3 == null)
        {
            Debug.Log("No trees assigned!");
            return;
        }

        trees = new GameObject("trees");

        //calculate a table showing the probabilities of having a tree around
        float total = 0;
        for (int i = 0; i < hmw - 1; i++)
        {
            for (int j = 0; j < hmh - 1; j++)
            {
                if (
                    EuclideanWaterProximityTable[i, j] != float.PositiveInfinity
                    && EuclideanWaterProximityTable[i, j] > 2
                )
                {
                    total +=
                        1
                        / (EuclideanWaterProximityTable[i, j] * EuclideanWaterProximityTable[i, j]);
                }
            }
        }

        //int maxTreesInGrid = Mathf.Max(1, (int)(ScaleFactor*ScaleFactor/9));
        int treeCount = 0;
        for (int i = 0; i < hmw - 1; i++)
        {
            for (int j = 0; j < hmh - 1; j++)
            {
                if (
                    EuclideanWaterProximityTable[i, j] != float.PositiveInfinity
                    && EuclideanWaterProximityTable[i, j] > 2
                )
                {
                    int numTrees = 0;
                    for (int k = 0; k < approximateNumTrees; k++)
                        numTrees =
                            UnityEngine.Random.Range(0, total)
                            < 1f
                                / (
                                    EuclideanWaterProximityTable[i, j]
                                    * EuclideanWaterProximityTable[i, j]
                                )
                                ? numTrees + 1
                                : numTrees;
                    for (int k = 0; k < numTrees; k++)
                    {
                        treeCount++;
                        MakeTree(
                            new Vector2(
                                i + UnityEngine.Random.Range(-0.5f, 0.5f),
                                j + UnityEngine.Random.Range(-0.5f, 0.5f)
                            )
                        );
                    }
                }
            }
        }
        Debug.Log("Initial tree count: " + treeCount);
    }

    private void GenerateCityTrees(int approximateNumTrees)
    {
        if (tree1 == null && tree2 == null && tree3 == null)
        {
            Debug.Log("No trees assigned!");
            return;
        }

        trees = new GameObject("trees");

        //calculate a table showing the probabilities of having a tree around
        float total = 0;
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                if (
                    CityWaterProximityTable[i, j] != float.PositiveInfinity
                    && CityAlphas[j, i, SandChannel] > 0.5f
                )
                {
                    total += 1 / (CityWaterProximityTable[i, j] * CityWaterProximityTable[i, j]);
                }
            }
        }

        //int maxTreesInGrid = Mathf.Max(1, (int)(ScaleFactor*ScaleFactor/9));
        int treeCount = 0;
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                if (
                    CityWaterProximityTable[i, j] != float.PositiveInfinity
                    && CityAlphas[j, i, SandChannel] > 0.5f
                )
                {
                    int numTrees = 0;
                    for (int k = 0; k < approximateNumTrees; k++)
                        numTrees =
                            UnityEngine.Random.Range(0, total)
                            < 1f / (CityWaterProximityTable[i, j] * CityWaterProximityTable[i, j])
                                ? numTrees + 1
                                : numTrees;
                    for (int k = 0; k < numTrees; k++)
                    {
                        treeCount++;
                        MakeTree(
                            new Vector2(
                                i + CityBoundaryMin.x + UnityEngine.Random.Range(-0.5f, 0.5f),
                                j + CityBoundaryMin.y + UnityEngine.Random.Range(-0.5f, 0.5f)
                            )
                        );
                        CityOccupation[i, j] = Occupation.Walkable;
                    }
                }
            }
        }
        Debug.Log("Tree count: " + treeCount);
    }

    //generates a tree over the terrain, size, type and orientation is random
    public void MakeTree(Vector2 treePosition2D)
    {
        GameObject htree;
        int treeType = UnityEngine.Random.value > 0.3f ? 1 : 2;
        if (treeType == 1)
            htree = Instantiate(tree1);
        else
            htree = Instantiate(tree2);

        htree.transform.SetParent(trees.transform, true);

        Vector3 treePos = new Vector3(treePosition2D.x, 0, treePosition2D.y);
        float height = CityTerrain.SampleHeight(treePos);
        treePos.y = height;
        htree.transform.position = treePos;
        float treeSize = UnityEngine.Random.Range(0.2f, 0.5f);
        htree.transform.localScale = new Vector3(treeSize, treeSize, treeSize) / ScaleFactor;
        htree.transform.Rotate(0, UnityEngine.Random.Range(0, 360), 0);
        Vector3 treeSize3D =
            ((treeType == 1) ? new Vector3(18, 22, 18) : new Vector3(10, 30, 10))
            * htree.transform.localScale.y;
        AddSceneryRegion(trees.transform, treePos + Vector3.up * treeSize3D.y / 2, treeSize3D);
    }

    public void AddSceneryRegion(Transform t, Vector3 center, Vector3 size)
    {
        AddEmptyRegion(t, center, size, "SceneryRegion");
    }

    public void AddEmptyRegion(
        Transform t,
        Vector3 center,
        Vector3 size,
        String rtag = "EmptyRegion"
    )
    {
        GameObject sr = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sr.tag = rtag;

        sr.transform.SetParent(t, false);
        sr.transform.localScale = size;
        sr.transform.localPosition = center;
        sr.GetComponent<MeshRenderer>().enabled = false;
    }

    private void CalculateEuclideanWaterProximity()
    {
        float tbegin = Time.realtimeSinceStartup;
        EuclideanWaterProximityTable = new float[
            CityTerrain.terrainData.alphamapWidth,
            CityTerrain.terrainData.alphamapHeight
        ];
        float[,,] alphas = CityTerrain.terrainData.GetAlphamaps(
            0,
            0,
            CityTerrain.terrainData.alphamapWidth,
            CityTerrain.terrainData.alphamapHeight
        );
        for (int i = 0; i < CityTerrain.terrainData.alphamapWidth; i++)
        {
            for (int j = 0; j < CityTerrain.terrainData.alphamapHeight; j++)
            {
                EuclideanWaterProximityTable[i, j] = hmw * ScaleFactor;
                //check if water source
                if (alphas[j, i, WaterChannel] > 0.5f)
                {
                    for (int di = 0; di < hmw - 1; di++)
                    {
                        for (int dj = 0; dj < hmh - 1; dj++)
                        {
                            //calculate distance from water to that point
                            float distance =
                                Mathf.Sqrt((i - di) * (i - di) + (j - dj) * (j - dj)) * ScaleFactor; //gives us approximate distance in meters
                            EuclideanWaterProximityTable[di, dj] = Mathf.Min(
                                EuclideanWaterProximityTable[di, dj],
                                distance
                            );
                        }
                    }
                }
            }
        }

        float tend = Time.realtimeSinceStartup;
        Debug.Log("time to calculate euclidean water proximity: " + (tend - tbegin));
    }

    private void CalculateWaterProximityWithinCity()
    {
        CityWaterProximityTable = new float[cw, ch];

        //firstly use the general terrrain measurements in case no water source within the city

        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                CityWaterProximityTable[i, j] = hmw * ScaleFactor;
            }
        }

        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                //check if water source
                if (CityAlphas[j, i, WaterChannel] > 0.5f)
                {
                    for (int di = 0; di < cw; di++)
                    {
                        for (int dj = 0; dj < ch; dj++)
                        {
                            //calculate distance from water to that point
                            float distance = CityAccessibilityTable[i, j, di, dj]; //gives us approximate distance in meters
                            CityWaterProximityTable[di, dj] = Mathf.Min(
                                CityWaterProximityTable[di, dj],
                                distance
                            );
                        }
                    }
                }
            }
        }
    }

    private void InitiateInformationObjects()
    {
        GameObject RegionInformationParent = new GameObject("region information");
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                GameObject riobj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                riobj.name = "ri" + i + j;
                riobj.transform.SetParent(RegionInformationParent.transform);
                float height = CityTerrain.SampleHeight(
                    new Vector3(i + CityBoundaryMin.x, 0, j + CityBoundaryMin.y)
                );
                riobj.transform.position = new Vector3(
                    i + CityBoundaryMin.x,
                    height - 1,
                    j + CityBoundaryMin.y
                );
                riobj.AddComponent<RegionInformation>();
                RegionInformation ri = riobj.GetComponent<RegionInformation>();
            }
        }
    }

    private void InitiateCityOcccupation()
    {
        CityOccupation = new Occupation[cw, ch];
        CityAlphas = CityTerrain.terrainData.GetAlphamaps(
            CityBoundaryMin.x,
            CityBoundaryMin.y,
            cw,
            ch
        );
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                if (CityAlphas[j, i, WaterChannel] > 0.1f)
                {
                    for (int ti = i - 1; ti < i + 2; ti++)
                    {
                        for (int tj = j - 1; tj < j + 2; tj++)
                        {
                            //if source != target and target is inside the boundaries
                            if (ti >= 0 && tj >= 0 && ti < cw && tj < ch)
                            {
                                CityOccupation[ti, tj] = Occupation.Walkable;
                            }
                        }
                    }
                }
                else
                {
                    if (CityOccupation[i, j] != Occupation.Walkable)
                        CityOccupation[i, j] = Occupation.Free;
                }
            }
        }
    }

    private void CalculateDistanceToCityCore()
    {
        CityCoreAccessibilityTable = new float[cw, ch];
        CityCoreAccessibilityPath = new Vector2Int[cw, ch];
        Vector2Int cityCoreGrid = new Vector2Int(cw / 2, ch / 2);

        for (int fi = 0; fi < cw; fi++)
        {
            for (int fj = 0; fj < ch; fj++)
            {
                CityCoreAccessibilityTable[fi, fj] = float.PositiveInfinity;
                CityCoreAccessibilityPath[fi, fj] = new Vector2Int(-2, -2);
            }
        }

        CityCoreAccessibilityPath[cityCoreGrid.x, cityCoreGrid.y] = new Vector2Int(-1, -1);
        CityCoreAccessibilityTable[cityCoreGrid.x, cityCoreGrid.y] = 0;
        LinkedList<Vector2Int> toBeVisited = new LinkedList<Vector2Int>();

        toBeVisited.AddFirst(cityCoreGrid);

        float EasiestWalkingSlope = 0; // -0.087f / 2f; //sin(5 deg) = 0.087

        while (toBeVisited.Count > 0)
        {
            //get the smallest one in the list
            Vector2Int current = toBeVisited.First.Value;

            for (
                LinkedListNode<Vector2Int> iNode = toBeVisited.First.Next;
                iNode != null;
                iNode = iNode.Next
            )
            {
                if (
                    CityCoreAccessibilityTable[iNode.Value.x, iNode.Value.y]
                    < CityCoreAccessibilityTable[current.x, current.y]
                )
                {
                    current.x = iNode.Value.x;
                    current.y = iNode.Value.y;
                }
            }

            Vector3 to = new Vector3(
                current.x + CityBoundaryMin.x,
                0f,
                current.y + CityBoundaryMin.y
            );
            float toHeight = CityTerrain.SampleHeight(to);
            to.y = toHeight;

            //check 8 way neighborhood
            for (int ti = current.x - 2; ti < current.x + 3; ti++)
            {
                for (int tj = current.y - 2; tj < current.y + 3; tj++)
                {
                    int fi = current.x;
                    int fj = current.y;

                    if (
                        (Mathf.Abs(ti - fi) + Mathf.Abs(tj - fj) == 4)
                        || Mathf.Abs(Mathf.Abs(ti - fi) - Mathf.Abs(tj - fj)) == 2
                    )
                        continue;
                    //if source != target and target is inside the boundaries
                    if (((ti != fi) || (tj != fj)) && GridInsideCity(ti, tj))
                    {
                        if (
                            !(
                                CityOccupation[ti, tj] == Occupation.Free
                                || CityOccupation[ti, tj] == Occupation.Walkable
                            )
                        )
                            continue;

                        //find the cost
                        Vector3 from = new Vector3(
                            ti + CityBoundaryMin.x,
                            0f,
                            tj + CityBoundaryMin.y
                        );
                        float fromHeight = CityTerrain.SampleHeight(from);
                        from.y = fromHeight;

                        //calculate a cost from source to target
                        float slope =
                            (to.y - from.y) / new Vector2(to.x - from.x, to.z - from.z).magnitude;
                        //float cost = ((to - from).magnitude + 10 * (slope - EasiestWalkingSlope) * (slope - EasiestWalkingSlope)) * ScaleFactor;
                        float cost =
                            (to - from).magnitude
                            * (
                                1
                                + 10
                                    * (
                                        (slope - EasiestWalkingSlope)
                                        * (slope - EasiestWalkingSlope)
                                    )
                            )
                            * ScaleFactor;
                        //if (CityOccupation[ti, tj] == Occupation.Walkable && CityOccupation[current.x, current.y] == Occupation.Walkable)
                        if (RoadExists(current.x, current.y, ti, tj))
                        {
                            cost *= 0.5f;
                            //Debug.DrawLine(from + Vector3.up, to + Vector3.up, Color.blue, 3000);
                        }
                        if (CityAlphas[tj, ti, WaterChannel] > 0.1f)
                        {
                            cost *= 4;
                        }

                        //if found cost plus cost of current < cost of target update it
                        if (
                            CityCoreAccessibilityTable[current.x, current.y] + cost
                            < CityCoreAccessibilityTable[ti, tj]
                        )
                        {
                            CityCoreAccessibilityTable[ti, tj] =
                                CityCoreAccessibilityTable[current.x, current.y] + cost;
                            CityCoreAccessibilityPath[ti, tj] = current;
                            //check if it exists in the
                            if (!toBeVisited.Contains(new Vector2Int(ti, tj)))
                                toBeVisited.AddLast(new Vector2Int(ti, tj));
                        }
                    }
                }
            }

            toBeVisited.Remove(current);
        }
    }

    private void CalculateAccessibilityWithinCity2()
    {
        CityAccessibilityScores = new float[cw, ch];
        CityAccessibilityTable = new float[cw, ch, cw, ch];
        CityAccessPath = new Vector2Int[cw, ch, cw, ch];

        for (int fi = CityBoundaryMin.x; fi <= CityBoundaryMax.x; fi++)
        {
            for (int fj = CityBoundaryMin.y; fj <= CityBoundaryMax.y; fj++)
            {
                for (int ti = CityBoundaryMin.x; ti <= CityBoundaryMax.x; ti++)
                {
                    for (int tj = CityBoundaryMin.y; tj <= CityBoundaryMax.y; tj++)
                    {
                        if (fi == ti && fj == tj)
                            CityAccessibilityTable[
                                fi - CityBoundaryMin.x,
                                fj - CityBoundaryMin.y,
                                ti - CityBoundaryMin.x,
                                tj - CityBoundaryMin.y
                            ] = 0;
                        else
                        {
                            CityAccessibilityTable[
                                fi - CityBoundaryMin.x,
                                fj - CityBoundaryMin.y,
                                ti - CityBoundaryMin.x,
                                tj - CityBoundaryMin.y
                            ] =
                                Mathf.Sqrt((fi - ti) * (fi - ti) + (fj - tj) * (fj - tj))
                                * ScaleFactor;
                        }
                    }
                }
            }
        }
    }

    private void CalculateAccessibilityWithinCity()
    {
        if (CityAccessibilityApproach == 2)
        {
            CalculateAccessibilityWithinCity2();
            return;
        }
        //firstly find direct costs for moving from each sample point to its neighbors
        CityAccessibilityScores = new float[cw, ch];
        CityAccessibilityTable = new float[cw, ch, cw, ch];
        CityAccessPath = new Vector2Int[cw, ch, cw, ch];

        float EasiestWalkingSlope = -0.087f / 2f; //sin(5 deg) = 0.087

        for (int fi = CityBoundaryMin.x; fi <= CityBoundaryMax.x; fi++)
        {
            for (int fj = CityBoundaryMin.y; fj <= CityBoundaryMax.y; fj++)
            {
                for (int ti = CityBoundaryMin.x; ti <= CityBoundaryMax.x; ti++)
                {
                    for (int tj = CityBoundaryMin.y; tj <= CityBoundaryMax.y; tj++)
                    {
                        if (fi == ti && fj == tj)
                            CityAccessibilityTable[
                                fi - CityBoundaryMin.x,
                                fj - CityBoundaryMin.y,
                                ti - CityBoundaryMin.x,
                                tj - CityBoundaryMin.y
                            ] = 0;
                        else
                            CityAccessibilityTable[
                                fi - CityBoundaryMin.x,
                                fj - CityBoundaryMin.y,
                                ti - CityBoundaryMin.x,
                                tj - CityBoundaryMin.y
                            ] = float.PositiveInfinity;
                    }
                }
            }
        }

        for (int fi = CityBoundaryMin.x; fi <= CityBoundaryMax.x; fi++)
        {
            for (int fj = CityBoundaryMin.y; fj <= CityBoundaryMax.y; fj++)
            {
                if (
                    !(
                        CityOccupation[fi - CityBoundaryMin.x, fj - CityBoundaryMin.y]
                            == Occupation.Free
                        || CityOccupation[fi - CityBoundaryMin.x, fj - CityBoundaryMin.y]
                            == Occupation.Walkable
                    )
                )
                    continue;
                Vector3 from = new Vector3(fi, 0f, fj);
                float fromHeight = CityTerrain.SampleHeight(from);
                from.y = fromHeight;

                //check 8 way neighborhood
                for (int ti = fi - 2; ti < fi + 3; ti++)
                {
                    for (int tj = fj - 2; tj < fj + 3; tj++)
                    {
                        if (
                            (Mathf.Abs(ti - fi) + Mathf.Abs(tj - fj) == 4)
                            || Mathf.Abs(Mathf.Abs(ti - fi) - Mathf.Abs(tj - fj)) == 2
                        )
                            continue;
                        //if source != target and target is inside the boundaries
                        if (((ti != fi) || (tj != fj)) && GridInsideCity(ti, tj, false))
                        {
                            if (
                                !(
                                    CityOccupation[ti - CityBoundaryMin.x, tj - CityBoundaryMin.y]
                                        == Occupation.Free
                                    || CityOccupation[
                                        ti - CityBoundaryMin.x,
                                        tj - CityBoundaryMin.y
                                    ] == Occupation.Walkable
                                )
                            )
                                continue;

                            Vector3 to = new Vector3(ti, 0f, tj);
                            float toHeight = CityTerrain.SampleHeight(to);
                            to.y = toHeight;

                            //calculate a cost from source to target
                            float slope = (to.y - from.y) / (to - from).magnitude;
                            float cost =
                                (to - from).magnitude
                                * (
                                    1
                                    + 10
                                        * (slope - EasiestWalkingSlope)
                                        * (slope - EasiestWalkingSlope)
                                )
                                * ScaleFactor;

                            CityAccessibilityTable[
                                fi - CityBoundaryMin.x,
                                fj - CityBoundaryMin.y,
                                ti - CityBoundaryMin.x,
                                tj - CityBoundaryMin.y
                            ] = cost;
                            CityAccessPath[
                                fi - CityBoundaryMin.x,
                                fj - CityBoundaryMin.y,
                                ti - CityBoundaryMin.x,
                                tj - CityBoundaryMin.y
                            ] = new Vector2Int(ti - CityBoundaryMin.x, tj - CityBoundaryMin.y);
                        }
                    }
                }
            }
        }

        for (int ni = 0; ni < cw; ni++)
        {
            //Debug.Log(ni + "/" + cw);
            for (int nj = 0; nj < ch; nj++)
            {
                if (
                    !(
                        CityOccupation[ni, nj] == Occupation.Free
                        || CityOccupation[ni, nj] == Occupation.Walkable
                    )
                )
                    continue;
                for (int fi = 0; fi < cw; fi++)
                {
                    for (int fj = 0; fj < ch; fj++)
                    {
                        if (float.IsInfinity(CityAccessibilityTable[fi, fj, ni, nj]))
                            continue;
                        for (int ti = 0; ti < cw; ti++)
                        {
                            for (int tj = 0; tj < ch; tj++)
                            {
                                if (
                                    CityAccessibilityTable[fi, fj, ni, nj]
                                        + CityAccessibilityTable[ni, nj, ti, tj]
                                    < CityAccessibilityTable[fi, fj, ti, tj]
                                )
                                {
                                    CityAccessibilityTable[fi, fj, ti, tj] =
                                        CityAccessibilityTable[fi, fj, ni, nj]
                                        + CityAccessibilityTable[ni, nj, ti, tj];
                                    CityAccessPath[fi, fj, ti, tj] = new Vector2Int(ni, nj);
                                }
                            }
                        }
                    }
                }
            }
        }

        for (int fi = 0; fi < cw; fi++)
        {
            for (int fj = 0; fj < ch; fj++)
            {
                for (int ti = 0; ti < cw; ti++)
                {
                    for (int tj = 0; tj < ch; tj++)
                    {
                        CityAccessibilityScores[fi, fj] +=
                            (
                                CityAccessibilityTable[fi, fj, ti, tj]
                                + CityAccessibilityTable[ti, tj, fi, fj]
                            ) / 2f;
                    }
                }
            }
        }
    }

    float buildingCostCalculationTime = 0;
    float[,] privacyCosts,
        sceneryScores,
        illuminationScores;

    bool randomCosts = false;

    [Obsolete]
    private float[,] CalculateBuildingCosts(
        float maxDistToCenter,
        float bestDistToCenter,
        float WeightDistToCenter,
        float WeightSlope,
        float WeightWaterProximity,
        float height = 3,
        float WeightPrivacy = -1,
        float WeightView = -1,
        float WeightSunExposure = -1
    )
    {
        float t_start = Time.realtimeSinceStartup;
        float[,] costs = new float[cw, ch];
        float[,] privacyCosts = new float[cw, ch];
        float[,] sceneryScores = new float[cw, ch];
        float[,] illuminationScores = new float[cw, ch];
        //initiate the costs with negatve values which means dirty flag
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                privacyCosts[i, j] = -1;
                sceneryScores[i, j] = -1;
                illuminationScores[i, j] = -1;
            }
        }

        if (WeightPrivacy > 0)
            privacyCosts = CheckViewPrivacyCosts(height / ScaleFactor);
        if (WeightView > 0)
        {
            sceneryScores = CheckSceneryScores(height / ScaleFactor);
        }
        if (WeightSunExposure > 0)
            illuminationScores = CheckSunExposureValues(height / ScaleFactor);
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                //calculate cost for that point;
                //if occupied or river skip
                costs[i, j] = float.PositiveInfinity;

                GameObject riobj = GameObject.Find("ri" + i + j);
                Renderer riRenderer = riobj.GetComponent<Renderer>();
                if (CityOccupation[i, j] == Occupation.Free)
                    riRenderer.material.SetColor("_Color", Color.white);
                else if (CityOccupation[i, j] == Occupation.Walkable)
                    riRenderer.material.SetColor("_Color", Color.yellow);
                else if (CityOccupation[i, j] == Occupation.Buildable)
                    riRenderer.material.SetColor("_Color", Color.blue);
                else
                    riRenderer.material.SetColor("_Color", Color.red);

                if (
                    CityOccupation[i, j] == Occupation.Free
                    || CityOccupation[i, j] == Occupation.Buildable
                )
                {
                    //float height = CityTerrain.terrainData.GetInterpolatedHeight(i * ScoreSamplingScale / (hmw - 1.0f), j * ScoreSamplingScale / (hmh - 1.0f));
                    Vector3 normal = CityTerrain.terrainData.GetInterpolatedNormal(
                        (i + CityBoundaryMin.x) / (hmw - 1.0f),
                        (j + CityBoundaryMin.y) / (hmh - 1.0f)
                    );
                    float distToCenter = CityCoreAccessibilityTable[i, j]; // CityAccessibilityTable[i, j, cw / 2, ch / 2];
                    float distCost =
                        Mathf.Abs(distToCenter - bestDistToCenter)
                        / (maxDistToCenter - bestDistToCenter);
                    distCost = distCost > 1 ? distCost + 2f : distCost;

                    float slopeCost =
                        Math.Abs(Mathf.Sin(20f * Mathf.Deg2Rad) - Vector3.Dot(normal, Vector3.up))
                        / Mathf.Sin(20f * Mathf.Deg2Rad);
                    slopeCost = slopeCost > 1 ? slopeCost + 2f : slopeCost;
                    slopeCost *= -1;
                    float waterProximityCost = CityWaterProximityTable[i, j];
                    float waterAvoidance =
                        waterProximityCost < 40 ? (80 - waterProximityCost) / 5 : 0;

                    costs[i, j] =
                        -1f * illuminationScores[i, j] * WeightSunExposure
                        + -1f * sceneryScores[i, j] * WeightView
                        + privacyCosts[i, j] * WeightPrivacy
                        + WeightDistToCenter * distCost
                        + WeightSlope * slopeCost
                        + WeightWaterProximity * waterProximityCost / 200f
                        + waterAvoidance; //1 point per 200 meter++

                    if (randomCosts)
                        costs[i, j] = UnityEngine.Random.value;
                    //fill debug information

                    RegionInformation ri = riobj.GetComponent<RegionInformation>();

                    ri.DistToCenter = distToCenter;
                    ri.Slope = slopeCost;
                    ri.WaterProximity = waterProximityCost;
                    ri.Privacy = privacyCosts[i, j];
                    ri.View = sceneryScores[i, j];
                    ri.SunExposure = illuminationScores[i, j];
                    ri.WeightDistToCenter = WeightDistToCenter;
                    ri.WeightSlope = WeightSlope;
                    ri.WeightWaterProximity = WeightWaterProximity;
                    ri.WeightPrivacy = WeightPrivacy;
                    ri.WeightView = WeightView;
                    ri.WeightSunExposure = WeightSunExposure;
                    ri.Height = height;
                    ri.Accommodates = -1;
                }
            }
        }

        buildingCostCalculationTime += Time.realtimeSinceStartup - t_start;
        return costs;
    }

    private float[,] CheckSunExposureValues(float height)
    {
        float t_start = Time.realtimeSinceStartup;
        float[,] scores = new float[cw, ch];

        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                Vector3 position = new Vector3(
                    CityBoundaryMin.x + i,
                    height,
                    CityBoundaryMin.y + j
                );
                position.y = CityTerrain.SampleHeight(position) + height;
                scores[i, j] = CheckSunExposure(position);
            }
        }
        done = true;
        sunExposureCalculationTime += Time.realtimeSinceStartup - t_start;
        return scores;
    }

    bool done = true;

    public float CheckSunExposure(Vector3 position)
    {
        if (sunExposureValues.ContainsKey(position) && sunExposureValues[position] >= 0)
            return sunExposureValues[position];

        float maxTilt = 23.5f;
        int halfYearSamples = 6;
        int shaded = 0;
        int sunny = 0;
        //yearly samples
        for (int m = -halfYearSamples; m <= halfYearSamples; m++)
        {
            float tiltAngle = 90 - Latitude + (m * maxTilt / halfYearSamples);
            //approximate daily samples
            for (int h = 7; h < 18; h++)
            {
                float dayAngle = 180f * (h - 6f) / 12f;
                Vector3 dir = new Vector3();
                dir.x = Mathf.Cos(dayAngle * Mathf.Deg2Rad);
                float smallR = Mathf.Sin(dayAngle * Mathf.Deg2Rad);
                dir.y = Mathf.Sin(tiltAngle * Mathf.Deg2Rad) * smallR;
                dir.z = -Mathf.Cos(tiltAngle * Mathf.Deg2Rad) * smallR;
                if (Physics.Raycast(position, dir, hmw * 2))
                {
                    shaded++;
                    if (!done)
                        Debug.DrawRay(position, dir, Color.black, 3000);
                }
                else
                {
                    sunny++;
                    if (!done)
                        Debug.DrawRay(position, dir, Color.yellow, 3000);
                }
            }
        }

        float score = 1.0f * sunny / (shaded + sunny);
        if (!sunExposureValues.ContainsKey(position))
            sunExposureValues.Add(position, score);
        else
            sunExposureValues[position] = score;

        return score;
    }

    [Obsolete]
    private float[,] CheckSceneryScores(float height)
    {
        float t_start = Time.realtimeSinceStartup;
        float[,] scores = new float[cw, ch];
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                Vector3 position = new Vector3(
                    CityBoundaryMin.x + i,
                    height,
                    CityBoundaryMin.y + j
                );
                position.y = CityTerrain.SampleHeight(position) + height;

                scores[i, j] = CheckSceneryScore(position);
            }
        }
        //normalize costs
        float maxCost = 0;
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                if (scores[i, j] > maxCost)
                    maxCost = scores[i, j];
            }
        }
        if (maxCost > 0.1f)
        {
            for (int i = 0; i < cw; i++)
            {
                for (int j = 0; j < ch; j++)
                {
                    scores[i, j] /= maxCost;
                }
            }
        }
        sceneryCalculationTime += Time.realtimeSinceStartup - t_start;
        return scores;
    }

    //returns an array of costs
    private float[,] CheckViewPrivacyCosts(float height)
    {
        float t_start = Time.realtimeSinceStartup;
        float viewPrivacyRadius = 50;
        float[,] costs = new float[cw, ch];
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PrivateRegion"))
        {
            for (int i = 0; i < cw; i++)
            {
                for (int j = 0; j < ch; j++)
                {
                    Vector3 position = new Vector3(
                        CityBoundaryMin.x + i,
                        height,
                        CityBoundaryMin.y + j
                    );
                    position.y = CityTerrain.SampleHeight(position) + height;

                    Vector3 dif = go.transform.position - position;
                    if (dif.magnitude * ScaleFactor < viewPrivacyRadius)
                    {
                        Vector3 direction = dif;
                        direction.Normalize();
                        float cosAngle = Vector3.Dot(go.transform.forward, direction);
                        if (cosAngle > 0)
                        {
                            //make ray casting
                            Ray r = new Ray(position, direction);
                            RaycastHit hinfo;
                            Physics.Raycast(r, out hinfo, viewPrivacyRadius / ScaleFactor);
                            if (Mathf.Abs(hinfo.distance - dif.magnitude) < 0.5f)
                            {
                                float cost =
                                    go.transform.localScale.sqrMagnitude * cosAngle / dif.magnitude;
                                costs[i, j] += cost;
                            }
                        }
                    }
                }
            }
        }

        //normalize costs
        float maxCost = 0;
        for (int i = 0; i < cw; i++)
        {
            for (int j = 0; j < ch; j++)
            {
                if (costs[i, j] > maxCost)
                    maxCost = costs[i, j];
            }
        }
        if (maxCost > 0.1f)
        {
            for (int i = 0; i < cw; i++)
            {
                for (int j = 0; j < ch; j++)
                {
                    costs[i, j] /= maxCost;
                }
            }
        }

        privacyCalculationTime += Time.realtimeSinceStartup - t_start;
        return costs;
    }

    /// <summary>
    /// After reserving an area to build a block, finds an entrance grid touching that area
    /// </summary>
    /// <param name="center"></param>
    /// <param name="checkRadius"></param>
    /// <returns></returns>
    private Vector2Int FindEntranceGrid(
        Vector2Int center,
        float checkRadius,
        bool twoEntrances = false
    )
    {
        int cr = Mathf.CeilToInt(checkRadius / ScaleFactor);
        Vector2Int entranceGrid = new Vector2Int(-1, -1);
        float minCost = float.PositiveInfinity;
        for (int ci = center.x - cr - 1; ci <= center.x + cr + 1; ci++)
        for (int cj = center.y - cr - 1; cj <= center.y + cr + 1; cj++)
        {
            if (ci < 0 || cj < 0 || ci >= cw || cj >= ch)
                continue;
            float distToCenter = Mathf.Sqrt(
                (float)(ci - center.x) * (ci - center.x) + (cj - center.y) * (cj - center.y)
            );
            if (
                distToCenter >= (checkRadius / ScaleFactor + 0.5f)
                && distToCenter < (checkRadius / ScaleFactor + 1.5f)
            )
            {
                //Debug.DrawLine(new Vector3(ci + CityBoundaryMin.x, -100, cj + CityBoundaryMin.y), new Vector3(ci + CityBoundaryMin.x, 100, cj + CityBoundaryMin.y), Color.blue, 1000);
                //Debug.DrawLine(new Vector3(ci + CityBoundaryMin.x, -100, cj + CityBoundaryMin.y), new Vector3(ci + CityBoundaryMin.x, 100, cj + CityBoundaryMin.y), Color.yellow, 1000);
                if (
                    CityOccupation[ci, cj] == Occupation.Free
                    || CityOccupation[ci, cj] == Occupation.Walkable
                ) //  && CityAccessibilityTable[ci,cj,cw/2,ch/2] < minCost)
                {
                    //float cost = CityAccessibilityTable[ci, cj, cw / 2, ch / 2] + distToCenter * 5;
                    float cost = CityCoreAccessibilityTable[ci, cj] + distToCenter * 5;
                    if (cost < minCost)
                    {
                        minCost = cost;
                        entranceGrid.Set(ci, cj);
                    }
                }
            }
        }

        return entranceGrid;
    }

    private Vector2Int FindBestBuildingArea(
        float[,] costs,
        float checkRadius,
        bool ConvertToOccupied = true,
        float padding = 1.0f
    )
    {
        Vector2Int bestPos = new Vector2Int(-1, -1);
        float mincost = float.PositiveInfinity;
        int cr = Mathf.CeilToInt(checkRadius / ScaleFactor);

        for (int i = cr; i < cw - cr; i++)
        {
            for (int j = cr; j < ch - cr; j++)
            {
                //find cost of that area
                float currentCost = 0f;
                for (int ci = i - cr; ci <= i + cr; ci++)
                for (int cj = j - cr; cj <= j + cr; cj++)
                {
                    float xdif = Mathf.Max(Mathf.Abs(ci - i) - 0.5f, 0);
                    float ydif = Mathf.Max(Mathf.Abs(cj - j) - 0.5f, 0);
                    if (
                        (xdif * xdif + ydif * ydif)
                        < (checkRadius / ScaleFactor * (checkRadius / ScaleFactor))
                    )
                        currentCost += costs[ci, cj];
                }

                if (currentCost < mincost)
                {
                    mincost = currentCost;
                    bestPos.x = i;
                    bestPos.y = j;
                }
            }
        }
        if (bestPos.x < 0)
            return bestPos;
        if (ConvertToOccupied)
        {
            //update occupation and accessibility information
            //loop through the affected points
            //update occupations to occupied
            for (int ci = bestPos.x - cr; ci <= bestPos.x + cr; ci++)
            for (int cj = bestPos.y - cr; cj <= bestPos.y + cr; cj++)
            {
                float xdif = Mathf.Max(Mathf.Abs(ci - bestPos.x) - 0.5f, 0);
                float ydif = Mathf.Max(Mathf.Abs(cj - bestPos.y) - 0.5f, 0);
                if (
                    (xdif * xdif + ydif * ydif)
                    < (checkRadius / ScaleFactor * (checkRadius / ScaleFactor))
                )
                {
                    CityOccupation[ci, cj] = Occupation.Occupied;

                    //draw a line and stop;
                    Vector3 from = new Vector3(ci + CityBoundaryMin.x, 0, cj + CityBoundaryMin.y);
                    float fh = CityTerrain.SampleHeight(from);
                    from.y = fh;

                    Vector3 to = new Vector3(
                        bestPos.x + CityBoundaryMin.x,
                        0,
                        bestPos.y + CityBoundaryMin.y
                    );
                    float nh = CityTerrain.SampleHeight(to);
                    to.y = nh;

                    //Debug.DrawLine(from, to, Color.red, 1000, false);
                }
                if (
                    CityOccupation[ci, cj] == Occupation.Free
                    && (
                        (xdif * xdif + ydif * ydif)
                        <= (
                            (checkRadius + padding)
                            / ScaleFactor
                            * ((checkRadius + padding) / ScaleFactor)
                        )
                    )
                )
                {
                    //draw a line and stop;
                    Vector3 from = new Vector3(ci + CityBoundaryMin.x, 0, cj + CityBoundaryMin.y);
                    float fh = CityTerrain.SampleHeight(from);
                    from.y = fh;

                    Vector3 to = new Vector3(
                        bestPos.x + CityBoundaryMin.x,
                        0,
                        bestPos.y + CityBoundaryMin.y
                    );
                    float nh = CityTerrain.SampleHeight(to);
                    to.y = nh;

                    //Debug.DrawLine(from, to, Color.yellow, 1000, false);
                    CityOccupation[ci, cj] = Occupation.Walkable;
                }
            }
        }
        return bestPos;
    }

    [Obsolete]
    private void BuildMosque()
    {
        Vector3 mosquePosition = CityCorePosition;
        float mosqueSize = Mathf.Sqrt(2 * (InitialPopulation * 2f * 0.75f + 50)); //0.75m2 per person, 2 times area for the courtyard

        float[,] costs = CalculateBuildingCosts(400, mosqueSize + 10, 1, 1, 0); //TODO add visibility related costs

        float checkRadius = mosqueSize / Mathf.Sqrt(2f);

        Vector2Int mosqueGrid = FindBestBuildingArea(costs, checkRadius, true, 3);

        mosquePosition.x = mosqueGrid.x + CityBoundaryMin.x;
        mosquePosition.z = mosqueGrid.y + CityBoundaryMin.y;
        float h = CityTerrain.SampleHeight(mosquePosition);
        mosquePosition.y = h;

        //build a representative shape

        PCMosque mosque = new PCMosque(
            mosquePosition,
            mosqueSize,
            10,
            "mosque",
            StoneWallTexture,
            RoofTexture
        );
        Vector2Int entrance = FindEntranceGrid(mosqueGrid, checkRadius);
        CityOccupation[entrance.x, entrance.y] = Occupation.Walkable;
        CityOccupation[2 * mosqueGrid.x - entrance.x, 2 * mosqueGrid.y - entrance.y] =
            Occupation.Walkable;
        mosque.entranceGrid = entrance;
        LinkedList<Vector3> pathList = MakeCityPath(CityBoundaryMin + mosque.entranceGrid);
    }

    private void MakeRoad(LinkedList<Vector3> path)
    {
        GameObject road = new GameObject("road");
        road.transform.SetParent(roads.transform);
        path.AddFirst(2 * path.First.Value - path.First.Next.Value);
        path.AddLast(2 * path.Last.Value - path.Last.Previous.Value);
        LinkedListNode<Vector3> p0 = path.First;
        LinkedListNode<Vector3> p1 = p0.Next;
        LinkedListNode<Vector3> p2 = p1.Next;
        LinkedListNode<Vector3> p3 = p2.Next;
        float stepSize = 0.2f;
        while (p3 != null)
        {
            for (float i = 0; i < 1; i += stepSize)
            {
                Vector3 from = GetCatmullRomPosition(i, p0.Value, p1.Value, p2.Value, p3.Value);
                Vector3 to = GetCatmullRomPosition(
                    i + stepSize,
                    p0.Value,
                    p1.Value,
                    p2.Value,
                    p3.Value
                );

                float h = CityTerrain.SampleHeight(from);
                from.y = h;
                h = CityTerrain.SampleHeight(to);
                to.y = h;

                GameObject roadSegment = MakeElongatedCube(from, to, 1f / ScaleFactor);
                //MakeElongatedWalls(from, to, 1f / ScaleFactor);
                roadSegment.transform.SetParent(road.transform, true);
            }
            //update roads table
            AddRoad(p1.Value, p2.Value);
            p0 = p1;
            p1 = p2;
            p2 = p3;
            p3 = p3.Next;
        }
    }

    private void AddRoad(Vector3 from, Vector3 to)
    {
        int fx = Mathf.RoundToInt(from.x) - CityBoundaryMin.x;
        int fy = Mathf.RoundToInt(from.z) - CityBoundaryMin.y;
        int tx = Mathf.RoundToInt(to.x) - CityBoundaryMin.x;
        int ty = Mathf.RoundToInt(to.z) - CityBoundaryMin.y;
        AddRoad(fx, fy, tx, ty);
    }

    private void AddRoad(int fx, int fy, int tx, int ty)
    {
        if (
            Mathf.Abs(fx - tx) + Mathf.Abs(fy - ty) > 3
            || Mathf.Abs(Mathf.Abs(fx - tx) - Mathf.Abs(fy - ty)) > 1
        )
        {
            Debug.Log(
                "Cannot add road between"
                    + new Vector2Int(fx, fy)
                    + " and "
                    + new Vector2Int(tx, ty)
                    + ". Not neighbors!"
            );
        }
        else if (fx == tx && fy == ty)
        {
            Debug.Log(
                "Cannot add road between"
                    + new Vector2Int(fx, fy)
                    + " and "
                    + new Vector2Int(tx, ty)
                    + ". Same point!"
            );
        }
        else
        {
            //east case
            if (tx == fx + 1 && ty == fy) //e case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0000_0000_0001);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0001_0000_0001);
            }
            else if (tx == fx + 2 && ty == fy + 1) //nee case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0000_0000_0010);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0010_0000_0000);
            }
            else if (tx == fx + 1 && ty == fy + 1) //ne case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0000_0000_0100);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0100_0000_0000);
            }
            else if (tx == fx + 1 && ty == fy + 2) //nne case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0000_0000_1000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_1000_0000_0000);
            }
            else if (tx == fx && ty == fy + 1) //n case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0000_0001_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0001_0000_0000_0000);
            }
            else if (tx == fx - 1 && ty == fy + 2) //nnw case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0000_0010_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0010_0000_0000_0000);
            }
            else if (tx == fx - 1 && ty == fy + 1) //nw case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0000_0100_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0100_0000_0000_0000);
            }
            else if (tx == fx - 2 && ty == fy + 1) //nww case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0000_1000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_1000_0000_0000_0000);
            }
            else if (tx == fx - 1 && ty == fy) //w case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0001_0000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0000_0000_0001);
            }
            else if (tx == fx - 2 && ty == fy - 1) //sww case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0010_0000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0000_0000_0010);
            }
            else if (tx == fx - 1 && ty == fy - 1) //sw case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_0100_0000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0000_0000_0100);
            }
            else if (tx == fx - 1 && ty == fy - 2) //ssw case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0000_1000_0000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0000_0000_1000);
            }
            else if (tx == fx && ty == fy - 1) //s case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0001_0000_0000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0000_0001_0000);
            }
            else if (tx == fx + 1 && ty == fy - 2) //sse case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0010_0000_0000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0000_0010_0000);
            }
            else if (tx == fx + 1 && ty == fy - 1) //se case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_0100_0000_0000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0000_0100_0000);
            }
            else if (tx == fx + 2 && ty == fy - 1) //see case
            {
                Roads[fx, fy] = (ushort)(Roads[fx, fy] | 0b_1000_0000_0000_0000);
                Roads[tx, ty] = (ushort)(Roads[tx, ty] | 0b_0000_0000_1000_0000);
            }
        }
    }

    private bool RoadExists(int fx, int fy)
    {
        ushort r = Roads[fx, fy];
        return Roads[fx, fy] > 0;
    }

    private bool RoadExists(Vector3 from, Vector3 to)
    {
        int fx = Mathf.RoundToInt(from.x) - CityBoundaryMin.x;
        int fy = Mathf.RoundToInt(from.z) - CityBoundaryMin.y;
        int tx = Mathf.RoundToInt(to.x) - CityBoundaryMin.x;
        int ty = Mathf.RoundToInt(to.z) - CityBoundaryMin.y;
        return RoadExists(fx, fy, tx, ty);
    }

    private bool RoadExists(int fx, int fy, int tx, int ty)
    {
        if (!GridInsideCity(fx, fy) || !GridInsideCity(tx, ty))
            return false;

        if (tx == fx + 1 && ty == fy) //e case
        {
            return (Roads[fx, fy] & 0b_0000_0000_0000_0001) > 0;
        }
        else if (tx == fx + 2 && ty == fy + 1) //nee case
        {
            return (Roads[fx, fy] & 0b_0000_0000_0000_0010) > 0;
        }
        else if (tx == fx + 1 && ty == fy + 1) //ne case
        {
            return (Roads[fx, fy] & 0b_0000_0000_0000_0100) > 0;
        }
        else if (tx == fx + 1 && ty == fy + 2) //nne case
        {
            return (Roads[fx, fy] & 0b_0000_0000_0000_1000) > 0;
        }
        else if (tx == fx && ty == fy + 1) //n case
        {
            return (Roads[fx, fy] & 0b_0000_0000_0001_0000) > 0;
        }
        else if (tx == fx - 1 && ty == fy + 2) //nnw case
        {
            return (Roads[fx, fy] & 0b_0000_0000_0010_0000) > 0;
        }
        else if (tx == fx - 1 && ty == fy + 1) //nw case
        {
            return (Roads[fx, fy] & 0b_0000_0000_0100_0000) > 0;
        }
        else if (tx == fx - 2 && ty == fy + 1) //nww case
        {
            return (Roads[fx, fy] & 0b_0000_0000_1000_0000) > 0;
        }
        else if (tx == fx - 1 && ty == fy) //w case
        {
            return (Roads[fx, fy] & 0b_0000_0001_0000_0000) > 0;
        }
        else if (tx == fx - 2 && ty == fy - 1) //sww case
        {
            return (Roads[fx, fy] & 0b_0000_0010_0000_0000) > 0;
        }
        else if (tx == fx - 1 && ty == fy - 1) //sw case
        {
            return (Roads[fx, fy] & 0b_0000_0100_0000_0000) > 0;
        }
        else if (tx == fx - 1 && ty == fy - 2) //ssw case
        {
            return (Roads[fx, fy] & 0b_0000_1000_0000_0000) > 0;
        }
        else if (tx == fx && ty == fy - 1) //s case
        {
            return (Roads[fx, fy] & 0b_0001_0000_0000_0000) > 0;
        }
        else if (tx == fx + 1 && ty == fy - 2) //sse case
        {
            return (Roads[fx, fy] & 0b_0010_0000_0000_0000) > 0;
        }
        else if (tx == fx + 1 && ty == fy - 1) //se case
        {
            return (Roads[fx, fy] & 0b_0100_0000_0000_0000) > 0;
        }
        else if (tx == fx + 2 && ty == fy - 1) //see case
        {
            return (Roads[fx, fy] & 0b_1000_0000_0000_0000) > 0;
        }
        return false;
    }

    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        p0 = (p1 + p0) / 2;
        p3 = (p2 + p3) / 2;
        //The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        //The cubic polynomial: a + b * t + c * t^2 + d * t^3
        Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));

        return pos;
    }

    private void MakeElongatedWalls(Vector3 from, Vector3 to, float width)
    {
        Vector3 dir = to - from;
        dir.Normalize();

        Vector3 toRight = Vector3.Cross(dir, Vector3.up);
        MakeElongatedCube2(from + toRight * width, to + toRight * width, 0.4f / ScaleFactor);
        MakeElongatedCube2(from - toRight * width, to - toRight * width, 0.4f / ScaleFactor);
    }

    private GameObject MakeElongatedCube2(Vector3 from, Vector3 to, float width)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = new Vector3(
            (to - from).magnitude + width,
            4 / ScaleFactor,
            width
        );
        cube.transform.localPosition = (from + to) / 2f;
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 dir2 = dir;
        dir2.y = 0;
        dir2.Normalize();
        cube.transform.Rotate(
            Vector3.Cross(dir2, dir),
            Vector3.SignedAngle(dir2, dir, Vector3.Cross(dir2, dir))
        );
        cube.transform.Rotate(Vector3.up, Vector3.SignedAngle(Vector3.right, dir2, Vector3.up));

        //adjustTexture
        Renderer roadRenderer = cube.GetComponent<Renderer>();
        roadRenderer.material.SetTexture("_MainTex", StoneWallTexture);
        roadRenderer.material.SetTextureScale("_MainTex", new Vector2(1f, 1f));
        roadRenderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        //roadRenderer.material.SetColor("_Color", Color.white * colorDarkening * 0.95f);

        return cube;
    }

    private GameObject MakeElongatedCube(Vector3 from, Vector3 to, float width)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = new Vector3((to - from).magnitude + width, width / 20, width);
        cube.transform.localPosition = (from + to) / 2f;
        Vector3 dir = to - from;
        dir.Normalize();
        Vector3 dir2 = dir;
        dir2.y = 0;
        dir2.Normalize();
        cube.transform.Rotate(
            Vector3.Cross(dir2, dir),
            Vector3.SignedAngle(dir2, dir, Vector3.Cross(dir2, dir))
        );
        cube.transform.Rotate(Vector3.up, Vector3.SignedAngle(Vector3.right, dir2, Vector3.up));

        cube.GetComponent<Collider>().enabled = false;

        //adjustTexture
        Renderer roadRenderer = cube.GetComponent<Renderer>();
        roadRenderer.material.SetTexture("_MainTex", StoneWallTexture);
        roadRenderer.material.SetTextureScale("_MainTex", new Vector2(1f, 1f));
        roadRenderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        //roadRenderer.material.SetColor("_Color", Color.white * colorDarkening * 0.95f);

        return cube;
    }

    [Obsolete]
    private bool BuildHouse(int numPeople)
    {
        Vector3 housePosition = CityCorePosition;

        int numRooms = 2 + numPeople / 2 + (int)UnityEngine.Random.Range(0, 2.99f); //kitchen + kiler + guestroom + numPeople/2
        int numFloors =
            numRooms <= 4 ? 1
            : numRooms <= 9 ? 2
            : 3;

        float coveredArea =
            Mathf.CeilToInt(1.0f * numRooms / numFloors) * 15f + UnityEngine.Random.Range(0, 10);
        float courtyardArea = 25 + numPeople * UnityEngine.Random.Range(5, 15);
        if (UnityEngine.Random.value < (0.1f + 0.8f * (1f - Privacy)))
            courtyardArea = 0;

        float checkRadius = Mathf.Sqrt(coveredArea + courtyardArea) / Mathf.Sqrt(2f);

        //calculate costs
        float[,] costs = CalculateBuildingCosts(
            400,
            20 + (1 - Security) * 30,
            0.5f + Privacy * UnityEngine.Random.Range(0.4f, 0.6f),
            0.5f + Social_Life * UnityEngine.Random.Range(0.4f, 0.6f),
            Mathf.Max(0.5f * UnityEngine.Random.value - 0.25f, 0f),
            numFloors * 3,
            Privacy * UnityEngine.Random.Range(0.8f, 1.2f),
            Beauty * UnityEngine.Random.Range(0.8f, 1.2f),
            Sustainability * UnityEngine.Random.Range(0.8f, 1.2f)
        ); //TODO add visibility related costs
        //float[,] costs = CalculateBuildingCosts(400, 100, 0, 1, 0);//TODO add visibility related costs

        //find a suitable place
        Vector2Int houseGrid = FindBestBuildingArea(costs, checkRadius * (1f - overlap), true, 0);

        //check if there is a suitable area
        if (houseGrid.x < 0)
            return false;

        //Build the house
        housePosition.x = houseGrid.x + CityBoundaryMin.x;
        housePosition.z = houseGrid.y + CityBoundaryMin.y;
        float h = CityTerrain.SampleHeight(housePosition);
        housePosition.y = h;

        Vector2Int entrance = FindEntranceGrid(houseGrid, checkRadius, true);
        if (entrance.x < 0)
            return false;

        Vector3 entrancePosition = new Vector3();
        entrancePosition.x = entrance.x + CityBoundaryMin.x;
        entrancePosition.z = entrance.y + CityBoundaryMin.y;
        h = CityTerrain.SampleHeight(entrancePosition);
        entrancePosition.y = h;

        PCHouse house = new PCHouse(
            housePosition,
            numFloors,
            coveredArea,
            courtyardArea,
            numPeople,
            "house",
            WoodWallTexture,
            StoneWallTexture,
            RoofTexture,
            tree1,
            tree2
        );

        RegionInformation ri1 = house.model.AddComponent<RegionInformation>();

        GameObject go = GameObject.Find("ri" + houseGrid.x + houseGrid.y);
        RegionInformation ri2 = go.GetComponent<RegionInformation>();
        ri1.CopyInformationFrom(ri2);
        ri1.Accommodates = numPeople;

        CityOccupation[entrance.x, entrance.y] = Occupation.Walkable;
        house.entranceGrid = entrance;
        LinkedList<Vector3> list = MakeCityPath(CityBoundaryMin + house.entranceGrid);

        bool secondEntrance = false;
        //adjust the second door if there is courtyard
        if (
            courtyardArea > 5
            && GridInsideCity(house.entranceGrid)
            && GridInsideCity(houseGrid * 2 - house.entranceGrid)
        )
        {
            house.secondaryEntranceGrid = houseGrid * 2 - house.entranceGrid;
            Vector3 entrancePosition2 = new Vector3();
            entrancePosition2.x = house.secondaryEntranceGrid.x + CityBoundaryMin.x;
            entrancePosition2.z = house.secondaryEntranceGrid.y + CityBoundaryMin.y;
            h = CityTerrain.SampleHeight(entrancePosition2);
            entrancePosition2.y = h;

            CityOccupation[house.secondaryEntranceGrid.x, house.secondaryEntranceGrid.y] =
                Occupation.Walkable;
            secondEntrance = true;
        }

        //update dictionaries
        DetermineDirtyCosts(houseGrid, checkRadius, numFloors * 3);

        //list.AddFirst(entrancePosition);
        list.AddFirst(housePosition);
        if (list.Count > 0)
        {
            MakeRoad(list);

            if (secondEntrance)
            {
                CalculateDistanceToCityCore();
                LinkedList<Vector3> list2 = MakeCityPath(
                    CityBoundaryMin + house.secondaryEntranceGrid
                );

                if (list2 != null && list2.Count > 0)
                {
                    list2.AddFirst(housePosition);
                    MakeRoad(list2);
                }
            }
            return true;
        }

        return false;
    }

    private void DetermineDirtyCosts(Vector2Int grid, float radius, float height = 3)
    {
        float negligibleSunAngle = 20;
        Vector3 pos = new Vector3(
            grid.x + CityBoundaryMin.x,
            height / ScaleFactor
                + radius * Mathf.Tan(negligibleSunAngle * Mathf.Deg2Rad) / ScaleFactor,
            grid.y + CityBoundaryMin.y
        );
        pos.y += CityTerrain.SampleHeight(pos);
        List<Vector3> dirtyList = new List<Vector3>();
        foreach (KeyValuePair<Vector3, float> item in sunExposureValues)
        {
            if (item.Value < 0)
                continue;
            Vector3 p2 = item.Key;
            if (Latitude < 0 && p2.z > pos.z + radius / ScaleFactor)
                continue;
            if (Latitude > 0 && p2.z < pos.z - radius / ScaleFactor)
                continue;
            if (
                (pos.y - p2.y) / new Vector2(pos.x - p2.x, pos.z - p2.z).magnitude
                < Mathf.Tan(negligibleSunAngle * Mathf.Deg2Rad)
            )
                continue;
            dirtyList.Add(p2);
            //if(drawn > 0)
            //Debug.DrawLine(p2, pos, Color.black, 1000);
        }
        for (int i = 0; i < dirtyList.Count; i++)
            sunExposureValues[dirtyList[i]] = -1;

        pos.y = height / ScaleFactor + CityTerrain.SampleHeight(pos);

        float negligibleVisArea = 25; //degrees^2
        dirtyList = new List<Vector3>();
        foreach (KeyValuePair<Vector3, float> item in sceneryValues)
        {
            if (item.Value < 0)
                continue;
            Vector3 p2 = item.Key;
            float dist = (pos - p2).magnitude;
            float deg1 = 2 * Mathf.Rad2Deg * Mathf.Atan(radius / ScaleFactor / dist);
            float deg2 = 2 * Mathf.Rad2Deg * Mathf.Atan(height / ScaleFactor / dist);
            if (deg1 * deg2 > negligibleVisArea)
            {
                if (dist < (20 + radius) / ScaleFactor)
                {
                    dirtyList.Add(p2);
                    if (drawn > 0)
                        Debug.DrawLine(p2, pos, Color.black, 1000);
                }
                else
                {
                    Vector3 rdir = pos - p2;
                    rdir.Normalize();
                    Ray r = new Ray(p2 + rdir * 20 / ScaleFactor, rdir);
                    if (!Physics.Raycast(r, dist - (radius + 20) / ScaleFactor))
                    {
                        dirtyList.Add(p2);
                        if (drawn <= 3 && drawn > 0)
                            Debug.DrawLine(p2, pos, Color.red, 1000);
                    }
                }
            }
        }
        for (int i = 0; i < dirtyList.Count; i++)
            sceneryValues[dirtyList[i]] = -1;
        drawn--;
    }

    private bool GridInsideCity(Vector2Int p, bool local = true)
    {
        return GridInsideCity(p.x, p.y, local);
    }

    private bool GridInsideCity(int x, int y, bool local = true)
    {
        if (local)
        {
            return x >= 0 && x < cw && y >= 0 && y < ch;
        }
        else
            return GridInsideCity(x - CityBoundaryMin.x, y - CityBoundaryMin.y);
    }

    [Obsolete]
    private bool BuildHouse(float size)
    {
        Vector3 housePosition = CityCorePosition;
        float houseSize = size;
        float[,] costs = CalculateBuildingCosts(
            400,
            100,
            0.5f + UnityEngine.Random.value,
            0.5f + UnityEngine.Random.value,
            0.5f * UnityEngine.Random.value
        ); //TODO add visibility related costs

        float checkRadius = houseSize / Mathf.Sqrt(2f);

        Vector2Int houseGrid = FindBestBuildingArea(costs, checkRadius, true, 0);

        housePosition.x = houseGrid.x + CityBoundaryMin.x;
        housePosition.z = houseGrid.y + CityBoundaryMin.y;
        float h = CityTerrain.SampleHeight(housePosition);
        housePosition.y = h;

        //build a representative shape
        PCBuilding house = new PCBuilding(housePosition, houseSize, 6f, "");
        Vector2Int entrance = FindEntranceGrid(houseGrid, checkRadius);
        CityOccupation[entrance.x, entrance.y] = Occupation.Walkable;
        house.entranceGrid = entrance;
        return MakeCityPath(CityBoundaryMin + house.entranceGrid).Count > 0;
    }

    [Obsolete]
    private void BuildMadrasah()
    {
        Vector3 madrasahPosition = CityCorePosition;
        float madrasahSize = Mathf.Sqrt(1.5f * (InitialPopulation * 2f * 0.75f + 50)); //0.75m2 per person, 1.5 times area for the courtyard

        float[,] costs = CalculateBuildingCosts(400, madrasahSize + 20, 1, 1, 0); //TODO add visibility related costs

        float checkRadius = madrasahSize / Mathf.Sqrt(2f);

        Vector2Int madrasahGrid = FindBestBuildingArea(costs, checkRadius);

        madrasahPosition.x = madrasahGrid.x + CityBoundaryMin.x;
        madrasahPosition.z = madrasahGrid.y + CityBoundaryMin.y;
        float h = CityTerrain.SampleHeight(madrasahPosition);
        madrasahPosition.y = h;

        madrasah = new PCMadrasah(
            madrasahPosition,
            madrasahSize,
            10f,
            "madrasah",
            StoneWallTexture,
            RoofTexture
        );
        Vector2Int entrance = FindEntranceGrid(madrasahGrid, checkRadius);
        CityOccupation[entrance.x, entrance.y] = Occupation.Walkable;
        madrasah.entranceGrid = entrance;
        float angle = Vector3.SignedAngle(
            Vector3.back,
            new Vector3(entrance.x - madrasahGrid.x, 0, entrance.y - madrasahGrid.y),
            Vector3.up
        );
        madrasah.model.transform.Rotate(Vector3.up, angle);
        MakeCityPath(CityBoundaryMin + madrasahGrid);
    }

    [Obsolete]
    private void BuildCarsi()
    {
        Vector3 carsiPosition = CityCorePosition;
        float carsiSize = Mathf.Sqrt((1.0f + Economy) * (InitialPopulation * 2f * 1.0f + 100)); //0.75m2 per person, 2 times area for the courtyard

        float[,] costs = CalculateBuildingCosts(400, carsiSize + 10, 0.5f, 1, 0); //TODO add visibility related costs

        float checkRadius = carsiSize / Mathf.Sqrt(2f);

        Vector2Int carsiGrid = FindBestBuildingArea(costs, checkRadius, true, 2);

        carsiPosition.x = carsiGrid.x + CityBoundaryMin.x;
        carsiPosition.z = carsiGrid.y + CityBoundaryMin.y;
        float h = CityTerrain.SampleHeight(carsiPosition);
        carsiPosition.y = h;

        PCBedesten bedesten = new PCBedesten(
            carsiPosition,
            carsiSize,
            5f,
            "bedesten",
            StoneWallTexture,
            RoofTexture
        );
        Vector2Int entrance = FindEntranceGrid(carsiGrid, checkRadius, true);
        CityOccupation[entrance.x, entrance.y] = Occupation.Walkable;
        bedesten.entranceGrid = entrance;
        float angle = Vector3.SignedAngle(
            Vector3.back,
            new Vector3(entrance.x - carsiGrid.x, 0, entrance.y - carsiGrid.y),
            Vector3.up
        );
        bedesten.model.transform.Rotate(Vector3.up, angle);
        MakeCityPath(CityBoundaryMin + carsiGrid);
    }

    [Obsolete]
    private void BuildHamam()
    {
        //find the suitable place

        //Best: 150 meters away from city center, flat, close to water
        //At most 400 meters from city center


        Vector3 hamamPosition = CityCorePosition;
        float hamamSize = Mathf.Sqrt(InitialPopulation / 2 + 100); //1 side in meters

        float[,] costs = CalculateBuildingCosts(400, 150, 1f, 1f, 1f);

        //check the most suitable areas
        float checkRadius = hamamSize / Mathf.Sqrt(2f);
        /* explanation of check radius
         * x.......r
         * .x.....r.
         * ..x...r..
         * ...x.r...
         * ....r....
         * ...x.x...
         * ..x...x..
         * .x.....x.
         * x.......x
         */

        Vector2Int hamamGrid = FindBestBuildingArea(costs, checkRadius);

        hamamPosition.x = hamamGrid.x + CityBoundaryMin.x;
        hamamPosition.z = hamamGrid.y + CityBoundaryMin.y;
        float h = CityTerrain.SampleHeight(hamamPosition);
        hamamPosition.y = h;

        hamam = new PCHamam(hamamPosition, hamamSize, 7f, "hamam", StoneWallTexture);
        Vector2Int entrance = FindEntranceGrid(hamamGrid, checkRadius);
        CityOccupation[entrance.x, entrance.y] = Occupation.Walkable;
        hamam.entranceGrid = entrance;
    }

    // Find the core of the city depending on several factors
    void FindCityCore()
    {
        cityCore = new Vector2(250, 250);
        float h = CityTerrain.SampleHeight(new Vector3(cityCore.x, 0, cityCore.y));
        float[,,] alphas = CityTerrain.terrainData.GetAlphamaps(
            0,
            0,
            CityTerrain.terrainData.alphamapWidth,
            CityTerrain.terrainData.alphamapHeight
        );

        //to avoid cities directly over water
        float[,] WaterOccupation = new float[
            hmw / ScoreSamplingScale + 1,
            hmh / ScoreSamplingScale + 1
        ];
        for (int i = 0; i < hmw; i++)
        for (int j = 0; j < hmh; j++)
            WaterOccupation[i / ScoreSamplingScale, j / ScoreSamplingScale] = 0;
        for (int i = 0; i < CityTerrain.terrainData.alphamapWidth; i++)
        {
            for (int j = 0; j < CityTerrain.terrainData.alphamapHeight; j++)
            {
                WaterOccupation[i / ScoreSamplingScale, j / ScoreSamplingScale] +=
                    alphas[j, i, WaterChannel] / (ScoreSamplingScale * ScoreSamplingScale);
            }
        }

        CoreSideLength = Mathf.Sqrt(
            InitialPopulation * 2f * (m2perperson / (ScaleFactor * ScaleFactor))
        );

        CalculateVisibilityScores();
        CalculateClimateScores();
        CalculateAccessibilityScores();
        CalculateWaterProximityScores();

        RecalculateCosts();
    }

    private void RecalculateCosts()
    {
        float[,,] alphas = CityTerrain.terrainData.GetAlphamaps(
            0,
            0,
            CityTerrain.terrainData.alphamapWidth,
            CityTerrain.terrainData.alphamapHeight
        );

        //to avoid cities directly over water
        float[,] WaterOccupation = new float[
            hmw / ScoreSamplingScale + 1,
            hmh / ScoreSamplingScale + 1
        ];
        for (int i = 0; i < hmw; i++)
        for (int j = 0; j < hmh; j++)
            WaterOccupation[i / ScoreSamplingScale, j / ScoreSamplingScale] = 0;
        for (int i = 0; i < CityTerrain.terrainData.alphamapWidth; i++)
        {
            for (int j = 0; j < CityTerrain.terrainData.alphamapHeight; j++)
            {
                WaterOccupation[i / ScoreSamplingScale, j / ScoreSamplingScale] +=
                    alphas[j, i, WaterChannel] / (ScoreSamplingScale * ScoreSamplingScale);
            }
        }

        //calculate the scores of each point
        CityCoreScores = new float[hmw / ScoreSamplingScale, hmh / ScoreSamplingScale];
        float maxScore = 0f;
        float minScore = 1000f;
        CityCorePosition = new Vector3();

        //neighborhood check size -nc to +nc
        int nc = Mathf.RoundToInt(CoreSideLength / 2f / ScoreSamplingScale);
        int sampleCount = (2 * nc + 1) * (2 * nc + 1);
        Debug.Log("samples size for determining city core: " + sampleCount);
        float[] securityScores = new float[sampleCount];
        float[] sustainabilityScores = new float[sampleCount];
        float[] socialScores = new float[sampleCount];
        float[] economyScores = new float[sampleCount];

        for (int i = nc; i < hmw / ScoreSamplingScale - nc; i++)
        {
            for (int j = nc; j < hmh / ScoreSamplingScale - nc; j++)
            {
                int regionIndex = 0;
                for (int ii = i - nc; ii <= i + nc; ii++)
                {
                    for (int jj = j - nc; jj <= j + nc; jj++)
                    {
                        float secScore =
                            -0.4f * AccessibilityInScores[ii, jj] + 0.5f * VisibilityScores[ii, jj];
                        float susScore =
                            0.5f * WaterProximityScores[ii, jj] + 0.5f * ClimateScores[ii, jj];
                        float socScore =
                            0.2f * VisibilityScores[ii, jj]
                            + 0.0f * AccessibilityOutScores[ii, jj]
                            + 0.2f * AccessibilityInScores[ii, jj]
                            + 0.5f * AccessibilityWithinScores[ii, jj];
                        //float socScore = 0.7f * AccessibilityWithinScores[ii, jj];
                        float ecoScore =
                            0.3f * AccessibilityInScores[ii, jj]
                            + 0.2f * AccessibilityOutScores[ii, jj];
                        ;
                        if (AvoidWater)
                        {
                            securityScores[regionIndex] = secScore * (1 - WaterOccupation[ii, jj]);
                            sustainabilityScores[regionIndex] =
                                susScore * (1 - WaterOccupation[ii, jj]);
                            ;
                            socialScores[regionIndex] = socScore * (1 - WaterOccupation[ii, jj]);
                            ;
                            economyScores[regionIndex] = ecoScore * (1 - WaterOccupation[ii, jj]);
                            ;
                            regionIndex++;
                        }
                        else
                        {
                            securityScores[regionIndex] = secScore;
                            sustainabilityScores[regionIndex] = susScore;
                            socialScores[regionIndex] = socScore;
                            economyScores[regionIndex] = ecoScore;
                            regionIndex++;
                        }
                    }
                }

                Array.Sort(securityScores);
                Array.Sort(sustainabilityScores);
                Array.Sort(socialScores);
                Array.Sort(economyScores);

                int indexToCheck = sampleCount - 1 - sampleCount / 4; //median of the best quarter of the values

                float privacyScore = 0;
                float securityScore = 0; //securityScores[indexToCheck];
                float sustainabilityScore = 0; //sustainabilityScores[indexToCheck];
                float socialScore = 0; // socialScores[indexToCheck];
                float economyScore = 0; // economyScores[indexToCheck];
                float beautyScore = 0;
                for (int sampleIndex = indexToCheck; sampleIndex < sampleCount; sampleIndex++)
                {
                    securityScore += securityScores[sampleIndex];
                    sustainabilityScore += sustainabilityScores[sampleIndex];
                    socialScore += socialScores[sampleIndex];
                    economyScore += economyScores[sampleIndex];
                }

                CityCoreScores[i, j] =
                    privacyScore * Privacy
                    + securityScore * Security
                    + sustainabilityScore * Sustainability
                    + socialScore * Social_Life
                    + economyScore * Economy
                    + beautyScore * Beauty;

                //CityCoreScores[i, j] = privacyScore * Privacy + securityScore * Security + sustainabilityScore * Sustainability + socialScore * Social_Life + economyScore * Economy + beautyScore * Beauty;
                if (maxScore < CityCoreScores[i, j])
                {
                    CityCorePosition.x = i * ScoreSamplingScale;
                    CityCorePosition.z = j * ScoreSamplingScale;
                }
                maxScore = Mathf.Max(maxScore, CityCoreScores[i, j]);
                minScore = Mathf.Min(minScore, CityCoreScores[i, j]);
            }
        }
        float citycoreheight = CityTerrain.SampleHeight(CityCorePosition);
        CityCorePosition.y = citycoreheight;

        //Normalize and Visualize the scores for debug
        for (int i = 0; i < hmw / ScoreSamplingScale; i++)
        {
            for (int j = 0; j < hmh / ScoreSamplingScale; j++)
            {
                CityCoreScores[i, j] = (CityCoreScores[i, j] - minScore) / (maxScore - minScore);
                //Debug.Log("Final Score: " + i + "\t" + j + "\t: " + CityCoreScores[i, j]);

                Vector3 p = new Vector3(i * ScoreSamplingScale, 0, j * ScoreSamplingScale);
                float height = CityTerrain.SampleHeight(p);
                p.y = height + 2;
                float s = Mathf.Max(CityCoreScores[i, j], 0);

                for (int a = 0; a < 8; a++)
                {
                    Vector3 dir = new Vector3(
                        s * Mathf.Cos(2 * Mathf.PI * a / 8.0f),
                        0,
                        s * Mathf.Sin(2 * Mathf.PI * a / 8.0f)
                    );
                    //Debug.DrawRay(p, dir * 5f, new Color(s, s, 1f - s), 500);
                }
            }
        }
    }

    GameObject CostVisualization = null;

    private void VisualizeCosts(float[,] scores)
    {
        if (CostVisualization == null)
        {
            CostVisualization = new GameObject();
            for (int i = 0; i < hmw / ScoreSamplingScale; i++)
            {
                for (int j = 0; j < hmh / ScoreSamplingScale; j++)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "costcube" + i + "_" + j;
                    go.transform.SetParent(CostVisualization.transform);
                    go.transform.localScale = new Vector3(
                        ScoreSamplingScale,
                        ScoreSamplingScale,
                        ScoreSamplingScale
                    );
                    Vector3 pos = new Vector3(i * ScoreSamplingScale, 0, j * ScoreSamplingScale);
                    float height = CityTerrain.SampleHeight(pos);
                    pos.y = height;
                    go.transform.position = pos;

                    Renderer gorenderer = go.GetComponent<Renderer>();
                    //gorenderer.material.SetColor("_Color", new Color(scores[i, j], scores[i, j], 1 - scores[i, j]));
                    //gorenderer.material.SetFloat("_Shininess", 0.0f);
                    Material material = new Material(Shader.Find("Unlit/Color"));
                    material.SetColor(
                        "_Color",
                        new Color(scores[i, j], scores[i, j], 1 - scores[i, j])
                    );
                    gorenderer.material = material;
                }
            }
        }
        else
        {
            for (int i = 0; i < hmw / ScoreSamplingScale; i++)
            {
                for (int j = 0; j < hmh / ScoreSamplingScale; j++)
                {
                    GameObject go = GameObject.Find("costcube" + i + "_" + j);

                    Renderer gorenderer = go.GetComponent<Renderer>();
                    gorenderer.material.SetColor(
                        "_Color",
                        new Color(scores[i, j], scores[i, j], 1 - scores[i, j])
                    );
                }
            }
        }
    }

    private void CalculateVisibilityScores()
    {
        VisibilityTable = new bool[
            hmw / ScoreSamplingScale,
            hmh / ScoreSamplingScale,
            hmw / ScoreSamplingScale,
            hmh / ScoreSamplingScale
        ];
        VisibilityScores = new float[hmw / ScoreSamplingScale, hmh / ScoreSamplingScale];
        float maxVisibilityScore = 0f;
        //check visibility scores
        for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
        {
            for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
            {
                //get height of the point
                float fromHeight = CityTerrain.terrainData.GetInterpolatedHeight(
                    fi * ScoreSamplingScale / (hmw - 1.0f),
                    fj * ScoreSamplingScale / (hmh - 1.0f)
                );
                Vector3 from = new Vector3(
                    fi * ScoreSamplingScale,
                    fromHeight + 2.02f,
                    fj * ScoreSamplingScale
                );

                //fill visibility table
                for (int ti = 0; ti < hmw / ScoreSamplingScale; ti++)
                {
                    for (int tj = 0; tj < hmh / ScoreSamplingScale; tj++)
                    {
                        if (fi == ti && fj == tj)
                            VisibilityTable[fi, fj, ti, tj] = true;
                        else
                        {
                            //get height of the point
                            float toHeight = CityTerrain.terrainData.GetInterpolatedHeight(
                                ti * ScoreSamplingScale / (hmw - 1.0f),
                                tj * ScoreSamplingScale / (hmh - 1.0f)
                            );
                            Vector3 to = new Vector3(
                                ti * ScoreSamplingScale,
                                toHeight,
                                tj * ScoreSamplingScale
                            );

                            Vector3 dir = to - from;
                            dir.Normalize();

                            Ray r = new Ray(from, dir);
                            RaycastHit rhit = new RaycastHit();
                            if (Physics.Raycast(r, out rhit, hmw * 2f))
                            {
                                if (
                                    rhit.distance * rhit.distance * 1.05
                                    > Mathf.Pow((to - from).magnitude, 2)
                                )
                                {
                                    //Debug.DrawLine(from, dir * rhit.distance, Color.yellow, 500, false);
                                    //Debug.Log(rhit.distance);
                                    Vector3 toNormal =
                                        CityTerrain.terrainData.GetInterpolatedNormal(
                                            ti * ScoreSamplingScale / (hmw - 1.0f),
                                            tj * ScoreSamplingScale / (hmh - 1.0f)
                                        );
                                    float visibilityFactor = Vector3.Dot(toNormal, -dir);

                                    VisibilityTable[fi, fj, ti, tj] = true;
                                    VisibilityScores[fi, fj] += 0.1f + visibilityFactor; //1;
                                    //VisibilityScores[fi, fj] += 1;
                                }
                            }
                        }
                    }
                }
            }
        }
        for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
        {
            for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
            {
                VisibilityScores[fi, fj] = Mathf.Sqrt(VisibilityScores[fi, fj]);
                if (VisibilityScores[fi, fj] > maxVisibilityScore)
                {
                    maxVisibilityScore = VisibilityScores[fi, fj];
                }
            }
        }

        //Normalize the scores between 0-1 interval
        for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
        {
            for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
            {
                VisibilityScores[fi, fj] = VisibilityScores[fi, fj] / maxVisibilityScore;
            }
        }
    }

    private void CalculateClimateScores()
    {
        ClimateScores = new float[hmw / ScoreSamplingScale, hmh / ScoreSamplingScale];
        AccessibilityWithinScores = new float[hmw / ScoreSamplingScale, hmh / ScoreSamplingScale];
        float maxClimateScore = 0f;
        float minClimateScore = float.PositiveInfinity;
        float maxAccessibilityWithin = 0f;

        for (int i = 0; i < hmw / ScoreSamplingScale; i++)
        {
            for (int j = 0; j < hmh / ScoreSamplingScale; j++)
            {
                //check height and heat difference
                //float height = CityTerrain.terrainData.GetInterpolatedHeight(i * ScoreSamplingScale / (hmw - 1.0f), j * ScoreSamplingScale / (hmh - 1.0f));
                float height = CityTerrain.SampleHeight(
                    new Vector3(i * ScoreSamplingScale, 0, j * ScoreSamplingScale)
                );
                float heatDif = height * ScaleFactor / 200f; //1 degrees per 200 meters
                Vector3 pos = new Vector3(i * ScoreSamplingScale, height, j * ScoreSamplingScale);
                Vector3 pos2 = new Vector3(
                    i * ScoreSamplingScale,
                    height + 2,
                    j * ScoreSamplingScale
                );

                //check direction of the city, find the
                //int sampleCount = 8;
                Vector3 generalGradient = CityTerrain.terrainData.GetInterpolatedNormal(
                    i * ScoreSamplingScale / (hmw - 1.0f),
                    j * ScoreSamplingScale / (hmh - 1.0f)
                );
                float walkability = 0;
                int numContributors = 0;

                for (
                    int si = (int)((i - 0.5f) * ScoreSamplingScale);
                    si <= (int)((i + 0.5f) * ScoreSamplingScale);
                    si += 1
                )
                {
                    for (
                        int sj = (int)((j - 0.5f) * ScoreSamplingScale);
                        sj <= (int)((j + 0.5f) * ScoreSamplingScale);
                        sj += 1
                    )
                    {
                        if (!IsOutOfBounds(new Vector2(si, sj)))
                        {
                            numContributors++;
                            Vector3 gradient = CityTerrain.terrainData.GetInterpolatedNormal(
                                si / (hmw - 1.0f),
                                sj / (hmh - 1.0f)
                            );
                            generalGradient += gradient;
                            float slope =
                                Mathf.Atan(Mathf.Sqrt(1 - gradient.y * gradient.y) / gradient.y)
                                * Mathf.Rad2Deg; //30 will be 0, 0 will be 1
                            slope = Mathf.Max(slope, 3f);
                            walkability += Mathf.Max(5 - Mathf.Log(slope - 2, 2), 0f); //Mathf.Max(Mathf.Min(Vector3.Dot(gradient, Vector3.up), Mathf.Cos(3f * Mathf.Deg2Rad))-Mathf.Cos(20f * Mathf.Deg2Rad),0f); // up to 3 degrees of slope is ok
                        }
                    }
                }

                generalGradient.Normalize();
                Vector3 sunDir = new Vector3(
                    0,
                    Mathf.Cos(Mathf.Deg2Rad * Latitude),
                    -1f * Mathf.Sin(Mathf.Deg2Rad * Latitude)
                );
                float gradientScore = 1 + Vector3.Dot(sunDir, generalGradient);
                //gradientScore = Mathf.Max(0f, 2 * (gradientScore - 1));
                Vector3 pos3 = pos + generalGradient * gradientScore * 3f;
                //Debug.DrawLine(pos, pos3, new Color(gradientScore / 2.0f, gradientScore / 2.0f, 1 - gradientScore / 2.0f), 500, true);

                AccessibilityWithinScores[i, j] = walkability / numContributors;
                if (AccessibilityWithinScores[i, j] > maxAccessibilityWithin)
                    maxAccessibilityWithin = AccessibilityWithinScores[i, j];
                ClimateScores[i, j] = 2 * gradientScore - 5 * heatDif * heatDif;
                if (ClimateScores[i, j] > maxClimateScore)
                    maxClimateScore = ClimateScores[i, j];
                if (ClimateScores[i, j] < minClimateScore)
                    minClimateScore = ClimateScores[i, j];
            }
        }

        //Normalize the scores
        for (int i = 0; i < hmw / ScoreSamplingScale; i++)
        {
            for (int j = 0; j < hmh / ScoreSamplingScale; j++)
            {
                AccessibilityWithinScores[i, j] =
                    AccessibilityWithinScores[i, j] / maxAccessibilityWithin;
                if (maxClimateScore == minClimateScore)
                    ClimateScores[i, j] = 0;
                else
                    ClimateScores[i, j] =
                        (ClimateScores[i, j] - minClimateScore)
                        / (maxClimateScore - minClimateScore);
            }
        }
    }

    // Find the highest Point of Terrain
    void FindHighestPoint()
    {
        Debug.Log(CityTerrain.terrainData.heightmapResolution);
        Debug.Log(hmh);
        Debug.Log(hmw);

        for (int i = 0; i < hmw; i++)
        {
            for (int j = 0; j < hmh; j++)
            {
                float h = CityTerrain.SampleHeight(new Vector3(i, 0, j));
                if (h > maxHeight)
                {
                    maxHeight = h;
                    maxHeightX = i;
                    maxHeightY = j;
                }
            }
        }
    }

    /*
    Uses Floyd's algorithm for all pairs shortest path problem
    */
    private void CalculateAccessibilityScores()
    {
        //firstly find direct costs for moving from each sample point to its neighbors
        AccessibilityInScores = new float[hmw / ScoreSamplingScale, hmh / ScoreSamplingScale];
        AccessibilityOutScores = new float[hmw / ScoreSamplingScale, hmh / ScoreSamplingScale];
        AccessibilityTable = new float[
            hmw / ScoreSamplingScale,
            hmh / ScoreSamplingScale,
            hmw / ScoreSamplingScale,
            hmh / ScoreSamplingScale
        ];
        AccessPath = new Vector2Int[
            hmw / ScoreSamplingScale,
            hmh / ScoreSamplingScale,
            hmw / ScoreSamplingScale,
            hmh / ScoreSamplingScale
        ];

        for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
        {
            for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
            {
                for (int ti = 0; ti < hmw / ScoreSamplingScale; ti++)
                {
                    for (int tj = 0; tj < hmh / ScoreSamplingScale; tj++)
                    {
                        if (fi == ti && fj == tj)
                            AccessibilityTable[fi, fj, ti, tj] = 0;
                        else
                            AccessibilityTable[fi, fj, ti, tj] = float.PositiveInfinity;
                    }
                }
            }
        }

        int numSamples = 4;
        if (ScoreSamplingScale < numSamples)
            numSamples = ScoreSamplingScale;
        float EasiestWalkingSlope = -0.087f / 2; //sin(5 deg) = 0.087
        CostOf1mStraight =
            numSamples
            * (0.25f + EasiestWalkingSlope * EasiestWalkingSlope)
            / (ScoreSamplingScale * ScaleFactor);
        Debug.Log("CostOf1mstraight: " + CostOf1mStraight);

        for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
        {
            for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
            {
                Vector3 from = new Vector3(fi * ScoreSamplingScale, 0f, fj * ScoreSamplingScale);
                float fromHeight = CityTerrain.SampleHeight(from);
                from.y = fromHeight;

                //check 8 way neighborhood
                for (int ti = fi - 1; ti < fi + 2; ti++)
                {
                    for (int tj = fj - 1; tj < fj + 2; tj++)
                    {
                        //if source != target and target is inside the boundaries
                        if (
                            ((ti != fi) || (tj != fj))
                            && ti >= 0
                            && tj >= 0
                            && ti < hmw / ScoreSamplingScale
                            && tj < hmh / ScoreSamplingScale
                        )
                        {
                            Vector3 to = new Vector3(
                                ti * ScoreSamplingScale,
                                0f,
                                tj * ScoreSamplingScale
                            );
                            float toHeight = CityTerrain.SampleHeight(to);
                            to.y = toHeight;

                            //calculate a cost from source to target
                            Vector3 step = (to - from) / numSamples;
                            float sm = new Vector3(step.x, 0, step.z).magnitude;
                            Vector3 current = from;
                            Vector3 next = current + step;
                            float cost = 0;
                            float constantPart =
                                new Vector2(fi - ti, fj - tj).magnitude / numSamples;

                            for (int s = 0; s < numSamples; s++)
                            {
                                //get nexts height
                                float hn = CityTerrain.SampleHeight(next);
                                next.y = hn;
                                //check slope and add to cost
                                float slope = (next.y - current.y) / sm;
                                cost +=
                                    constantPart
                                    * (
                                        1
                                        + 220
                                            * (slope - EasiestWalkingSlope)
                                            * (slope - EasiestWalkingSlope)
                                    ); //sin(5 deg) = 0.087float cost = ((to - from).magnitude * (1 + 5*(slope - EasiestWalkingSlope) * (slope - EasiestWalkingSlope))) * ScaleFactor;

                                //assign next to current
                                current = next;
                                next += step;
                            }
                            AccessibilityTable[fi, fj, ti, tj] = cost;
                            AccessPath[fi, fj, ti, tj] = new Vector2Int(ti, tj);
                        }
                    }
                }
            }
        }

        for (int ni = 0; ni < hmw / ScoreSamplingScale; ni++)
        {
            for (int nj = 0; nj < hmh / ScoreSamplingScale; nj++)
            {
                for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
                {
                    for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
                    {
                        if (float.IsInfinity(AccessibilityTable[fi, fj, ni, nj]))
                            continue;
                        for (int ti = 0; ti < hmw / ScoreSamplingScale; ti++)
                        {
                            for (int tj = 0; tj < hmh / ScoreSamplingScale; tj++)
                            {
                                if (
                                    AccessibilityTable[fi, fj, ni, nj]
                                        + AccessibilityTable[ni, nj, ti, tj]
                                    < AccessibilityTable[fi, fj, ti, tj]
                                )
                                {
                                    AccessibilityTable[fi, fj, ti, tj] =
                                        AccessibilityTable[fi, fj, ni, nj]
                                        + AccessibilityTable[ni, nj, ti, tj];
                                    AccessPath[fi, fj, ti, tj] = new Vector2Int(ni, nj);
                                }
                            }
                        }
                    }
                }
            }
        }

        for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
        {
            for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
            {
                for (int ti = 0; ti < hmw / ScoreSamplingScale; ti++)
                {
                    for (int tj = 0; tj < hmh / ScoreSamplingScale; tj++)
                    {
                        if (fi != ti || fj != tj)
                        {
                            AccessibilityOutScores[fi, fj] +=
                                AccessibilityTable[fi, fj, ti, tj]
                                / new Vector2(fi - ti, fj - tj).magnitude;
                            AccessibilityInScores[fi, fj] +=
                                AccessibilityTable[ti, tj, fi, fj]
                                / new Vector2(fi - ti, fj - tj).magnitude;
                        }
                    }
                }
            }
        }
        float maxAccessibilityCost = 0f;
        float minAccessibilityCost = float.PositiveInfinity;
        for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
        {
            for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
            {
                maxAccessibilityCost = Mathf.Max(
                    maxAccessibilityCost,
                    AccessibilityInScores[fi, fj],
                    AccessibilityOutScores[fi, fj]
                );
                minAccessibilityCost = Mathf.Min(
                    minAccessibilityCost,
                    AccessibilityInScores[fi, fj],
                    AccessibilityOutScores[fi, fj]
                );
            }
        }

        //Normalize the scores max = 1, (possibly negative values here)
        for (int fi = 0; fi < hmw / ScoreSamplingScale; fi++)
        {
            for (int fj = 0; fj < hmh / ScoreSamplingScale; fj++)
            {
                AccessibilityInScores[fi, fj] =
                    (AccessibilityInScores[fi, fj] - maxAccessibilityCost)
                    / (minAccessibilityCost - maxAccessibilityCost);
                AccessibilityOutScores[fi, fj] =
                    (AccessibilityOutScores[fi, fj] - maxAccessibilityCost)
                    / (minAccessibilityCost - maxAccessibilityCost);
            }
        }

        //show one path from (3,3) to (20,15)
        Vector2Int fromInt = new Vector2Int(3, 3);
        Vector2Int toInt = new Vector2Int(20, 15);
        drawPath(fromInt, toInt);
        drawPath(new Vector2Int(5, 8), new Vector2Int(2, 18));
        drawPath(new Vector2Int(6, 2), new Vector2Int(20, 15));
        drawPath(new Vector2Int(9, 2), new Vector2Int(20, 15));
    }

    int infinityavoider = 500;

    private void drawPath(Vector2Int fromInt, Vector2Int toInt)
    {
        if (infinityavoider-- < 0)
        {
            Debug.Log("infinity!!!");
            return;
        }
        //find a middle node
        Vector2Int nextInt = AccessPath[fromInt.x, fromInt.y, toInt.x, toInt.y];

        if (nextInt.x == toInt.x && nextInt.y == toInt.y)
        {
            //draw a line and stop;
            Vector3 from = new Vector3(
                fromInt.x * ScoreSamplingScale,
                0,
                fromInt.y * ScoreSamplingScale
            );
            float fh = CityTerrain.SampleHeight(from);
            from.y = fh;

            Vector3 to = new Vector3(
                nextInt.x * ScoreSamplingScale,
                0,
                nextInt.y * ScoreSamplingScale
            );
            float nh = CityTerrain.SampleHeight(to);
            to.y = nh;

            //Debug.DrawLine(from, to, Color.white, 500, false);
            //Debug.Log(fromInt.x + " " + fromInt.y + " -> " + nextInt.x + " " + nextInt.y);
        }
        else
        {
            drawPath(fromInt, nextInt);
            drawPath(nextInt, toInt);
        }
    }

    private LinkedList<Vector3> MakeCityPath(Vector2Int fromInt)
    {
        if (
            CityCoreAccessibilityPath[
                fromInt.x - CityBoundaryMin.x,
                fromInt.y - CityBoundaryMin.y
            ].x == -2
        )
            return null;

        Vector2Int current = fromInt;
        Vector3 current3 = new Vector3(current.x, 0, current.y);
        float h = CityTerrain.SampleHeight(current3);
        current3.y = h;
        LinkedList<Vector3> path = new LinkedList<Vector3>();
        path.AddLast(current3);

        while (
            CityCoreAccessibilityPath[
                current.x - CityBoundaryMin.x,
                current.y - CityBoundaryMin.y
            ].x != -1
        )
        {
            current =
                CityCoreAccessibilityPath[
                    current.x - CityBoundaryMin.x,
                    current.y - CityBoundaryMin.y
                ] + CityBoundaryMin;

            if (
                CityOccupation[current.x - CityBoundaryMin.x, current.y - CityBoundaryMin.y]
                == Occupation.Free
            )
                CityOccupation[current.x - CityBoundaryMin.x, current.y - CityBoundaryMin.y] =
                    Occupation.Walkable;

            current3 = new Vector3(current.x, 0, current.y);
            h = CityTerrain.SampleHeight(current3);
            current3.y = h;
            path.AddLast(current3);
        }
        return path;
    }

    private LinkedList<Vector3> MakeCityPath(Vector2Int fromInt, Vector2Int toInt)
    {
        if (infinityavoider-- < 0)
        {
            Debug.Log("infinity!!!");
            return null;
        }
        //find a middle node
        Vector2Int nextInt =
            CityAccessPath[
                fromInt.x - CityBoundaryMin.x,
                fromInt.y - CityBoundaryMin.y,
                toInt.x - CityBoundaryMin.x,
                toInt.y - CityBoundaryMin.y
            ] + CityBoundaryMin;

        if (nextInt.x == toInt.x && nextInt.y == toInt.y)
        {
            //draw a line and stop;
            Vector3 from = new Vector3(fromInt.x, 0, fromInt.y);
            float fh = CityTerrain.SampleHeight(from);
            from.y = fh;

            Vector3 to = new Vector3(nextInt.x, 0, nextInt.y);
            float nh = CityTerrain.SampleHeight(to);
            to.y = nh;

            //Debug.DrawLine(from, to, Color.white, 500,false);
            //Debug.Log(fromInt.x + " " + fromInt.y + " -> " + nextInt.x + " " + nextInt.y);
            if (
                CityOccupation[fromInt.x - CityBoundaryMin.x, fromInt.y - CityBoundaryMin.y]
                == Occupation.Free
            )
                CityOccupation[fromInt.x - CityBoundaryMin.x, fromInt.y - CityBoundaryMin.y] =
                    Occupation.Walkable;
            if (
                CityOccupation[nextInt.x - CityBoundaryMin.x, nextInt.y - CityBoundaryMin.y]
                == Occupation.Free
            )
                CityOccupation[nextInt.x - CityBoundaryMin.x, nextInt.y - CityBoundaryMin.y] =
                    Occupation.Walkable;
            LinkedList<Vector3> list = new LinkedList<Vector3>();
            list.AddFirst(to);
            return list;
        }
        else
        {
            LinkedList<Vector3> list1 = MakeCityPath(fromInt, nextInt);
            LinkedList<Vector3> list2 = MakeCityPath(nextInt, toInt);
            foreach (Vector3 v3 in list2)
                list1.AddLast(v3);
            return list1;
            //bool b = true;
            //b = b && MakeCityPath(fromInt, nextInt);
            //b = b && MakeCityPath(nextInt, toInt);
            //return b;
        }
    }

    private void AddRiverSceneryRegions()
    {
        float[,,] alphas = CityTerrain.terrainData.GetAlphamaps(
            0,
            0,
            CityTerrain.terrainData.alphamapWidth,
            CityTerrain.terrainData.alphamapHeight
        );

        for (int i = 0; i < ScoreSamplingScale * (hmw / ScoreSamplingScale); i++)
        {
            for (int j = 0; j < ScoreSamplingScale * (hmh / ScoreSamplingScale); j++)
            {
                //check if water source
                if (alphas[j, i, 2] > 0.5f)
                {
                    float height = CityTerrain.terrainData.GetInterpolatedHeight(
                        i / (hmw - 1.0f),
                        j / (hmh - 1.0f)
                    );
                    AddSceneryRegion(
                        sceneryRegions.transform,
                        new Vector3(i + 0.5f, height, j + 0.5f),
                        new Vector3(1.5f, 1.5f, 1.5f)
                    );
                }
            }
        }
    }

    //calculates proximities to water sources using accessibility table
    //prerequisite: accessibility table should be ready
    private void CalculateWaterProximityScores()
    {
        WaterProximityScores = new float[hmw / ScoreSamplingScale, hmh / ScoreSamplingScale];
        float[,,] alphas = CityTerrain.terrainData.GetAlphamaps(
            0,
            0,
            CityTerrain.terrainData.alphamapWidth,
            CityTerrain.terrainData.alphamapHeight
        );
        for (int di = 0; di < hmw / ScoreSamplingScale; di++)
        {
            for (int dj = 0; dj < hmh / ScoreSamplingScale; dj++)
            {
                WaterProximityScores[di, dj] = 0f;
            }
        }

        for (int i = 0; i < ScoreSamplingScale * (hmw / ScoreSamplingScale); i++)
        {
            for (int j = 0; j < ScoreSamplingScale * (hmh / ScoreSamplingScale); j++)
            {
                //check if water source
                if (alphas[j, i, 2] > 0.5f)
                {
                    for (int di = 0; di < hmw / ScoreSamplingScale; di++)
                    {
                        for (int dj = 0; dj < hmh / ScoreSamplingScale; dj++)
                        {
                            float distance =
                                AccessibilityTable[
                                    di,
                                    dj,
                                    i / ScoreSamplingScale,
                                    j / ScoreSamplingScale
                                ] / CostOf1mStraight; //gives us approximate distance in meters
                            if (distance < WaterFarAwayThreshold)
                            {
                                distance = Mathf.Max(distance, WaterNearbyThreshold);
                                //distance = Mathf.Min(distance, WaterFarAwayThreshold);
                                //1st strategy. Close to any water source wins
                                WaterProximityScores[di, dj] = Mathf.Max(
                                    WaterProximityScores[di, dj],
                                    (WaterFarAwayThreshold - distance)
                                        / (WaterFarAwayThreshold - WaterNearbyThreshold)
                                );
                                //2nd strategy. Each water source adds to score
                                //WaterProximityScores[di, dj] += Mathf.Min(1,Mathf.Max(0, (WaterFarAwayThreshold - distance) / (WaterFarAwayThreshold - WaterNearbyThreshold)));
                                //3rd strategy. Each water source adds to score until a threshold

                                //WaterProximityScores[di, dj] = Mathf.Min(WaterProximityScores[di, dj], maxwaterproximityscore);
                            }
                        }
                    }
                }
            }
        }

        float maxScore = 1;
        for (int i = 0; i < hmw / ScoreSamplingScale; i++)
        for (int j = 0; j < hmh / ScoreSamplingScale; j++)
            maxScore = Mathf.Max(maxScore, WaterProximityScores[i, j]);

        for (int i = 0; i < hmw / ScoreSamplingScale; i++)
        for (int j = 0; j < hmh / ScoreSamplingScale; j++)
            WaterProximityScores[i, j] /= maxScore;
    }

    //returns an array of costs, circular manner
    public float[] CheckPrivacyCost(Vector3 position)
    {
        float[] costs = new float[36];
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PrivateRegion"))
        {
            Vector3 dif = go.transform.position - position;
            if (dif.magnitude * ScaleFactor < ViewPrivacyRadius)
            {
                Vector3 direction = dif;
                direction.Normalize();
                float cosAngle = Vector3.Dot(go.transform.forward, direction);
                if (cosAngle > 0)
                {
                    //make ray casting
                    Ray r = new Ray(position, direction);
                    RaycastHit hinfo;
                    Physics.Raycast(r, out hinfo, ViewPrivacyRadius / ScaleFactor);
                    if (Mathf.Abs(hinfo.distance - dif.magnitude) < 0.5f)
                    {
                        float cost =
                            go.transform.localScale.sqrMagnitude * cosAngle / dif.magnitude;
                        //Debug.DrawLine(go.transform.position, position, new Color(cosAngle, cosAngle, 1 - cosAngle), 500);
                        //Debug.Log(cosAngle);
                        Vector3 dirPlanar = direction;
                        dirPlanar.y = 0;
                        dirPlanar.Normalize();
                        float angle = Vector3.SignedAngle(Vector3.forward, dirPlanar, Vector3.up);
                        costs[
                            ((int)(angle / (360F / costs.Length)) + costs.Length) % costs.Length
                        ] += cost;
                    }
                }
            }
        }

        return costs;
    }

    int drawn = 0;

    public float[] CheckSceneryScoreOld(Vector3 position)
    {
        float[] scores = new float[36];
        float[] avdists = new float[36];
        float[] sceneryHits = new float[36];
        int[] samples = new int[36];

        int hstep = 360 / scores.Length;
        int vstep = 9;
        for (int i = 0; i < 360; i += hstep)
        {
            float totalDist = 0;
            int samplesCount = 0;
            int sceneryHitsCount = 0;
            for (int j = -45; j < 45; j += vstep)
            {
                Vector3 dir = new Vector3(
                    Mathf.Sin(i * Mathf.Deg2Rad),
                    Mathf.Sin(j * Mathf.Deg2Rad),
                    Mathf.Cos(i * Mathf.Deg2Rad)
                );
                Ray r = new Ray(position, dir);
                RaycastHit h = new RaycastHit();
                if (Physics.Raycast(r, out h, hmw * 2))
                {
                    if (h.collider.gameObject.CompareTag("SceneryRegion"))
                    {
                        //if(!drawn)
                        //    Debug.DrawLine(position, h.point, Color.red, 3000);
                        sceneryHitsCount++;
                    }
                    else
                        //if(!drawn)
                        //    Debug.DrawLine(position, h.point, Color.yellow, 3000);
                        totalDist += h.distance;
                    samplesCount++;
                }
                else
                    break;
            }
            float averageDist = totalDist / samplesCount;
            avdists[i / hstep] = averageDist;
            sceneryHits[i / hstep] = 1.0f * sceneryHitsCount / samplesCount;
            samples[i / hstep] = samplesCount;
        }

        //calculate a score for each direction
        for (int i = 0; i < scores.Length; i++)
            scores[i] = 2 * sceneryHits[i] / samples[i] + avdists[i] / hmw;
        //drawn = true;
        return scores;
    }

    /// <summary>
    /// Calculates a "scenery score" for a given position based on nearby objects and terrain using raycasting.
    /// The score considers how scenic the area is (more scenery hits) and the average distance to obstacles.
    /// </summary>
    /// <param name="position">The position to check the scenery score for.</param>
    /// <returns>The calculated scenery score for the position.</returns>
    [Obsolete]
    public float CheckSceneryScore(Vector3 position)
    {
        // If the scenery score is already cached, return it
        if (sceneryValues.ContainsKey(position) && sceneryValues[position] >= 0)
        {
            return sceneryValues[position];
        }

        const int scenerySamplesCount = 18;
        float[] scores = new float[scenerySamplesCount];
        float[] avgDistances = new float[scenerySamplesCount];
        float[] sceneryHits = new float[scenerySamplesCount];
        int[] samples = new int[scenerySamplesCount];

        int hstep = 360 / scores.Length; // Horizontal step in degrees
        const int vstep = 9; // Vertical step in degrees

        // Initialize the raycast commands and results arrays for the batch processing
        NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(
            360 / hstep * (90 / vstep),
            Allocator.TempJob
        );
        NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(
            360 / hstep * (90 / vstep),
            Allocator.TempJob
        );

        // Prepare raycast commands for horizontal and vertical angles
        for (int i = 0; i < 360; i += hstep)
        {
            for (int j = -45; j < 45; j += vstep)
            {
                Vector3 dir = new Vector3(
                    Mathf.Sin(i * Mathf.Deg2Rad),
                    Mathf.Sin(j * Mathf.Deg2Rad),
                    Mathf.Cos(i * Mathf.Deg2Rad)
                );
                RaycastCommand r = new RaycastCommand(position, dir, hmw * 2);
                commands[i / hstep * (90 / vstep) + ((j + 45) / vstep)] = r;
            }
        }

        // Execute raycast commands in parallel using the job system
        JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 4);
        handle.Complete(); // Wait for the job to complete

        commands.Dispose(); // Release the allocated memory for the commands

        for (int i = 0; i < 360; i += hstep)
        {
            float totalDist = 0;
            int samplesCount = 0;
            int sceneryHitsCount = 0;

            for (int j = -45; j < 45; j += vstep)
            {
                RaycastHit h = results[i / hstep * (90 / vstep) + ((j + 45) / vstep)];
                if (h.collider != null)
                {
                    if (h.collider.gameObject.CompareTag("SceneryRegion"))
                    {
                        sceneryHitsCount++;
                    }
                    else
                    {
                        totalDist += h.distance;
                    }
                    samplesCount++;
                }
                else
                {
                    break;
                }
            }

            // Compute average distance and scenery hit percentage
            avgDistances[i / hstep] = totalDist / samplesCount;
            sceneryHits[i / hstep] = 1.0f * sceneryHitsCount / samplesCount;
            samples[i / hstep] = samplesCount;
        }

        results.Dispose(); // Release the allocated memory for the results

        // Calculate the final score based on the average distance and scenery hits
        for (int i = 0; i < scores.Length; i++)
        {
            scores[i] = 2 * sceneryHits[i] / samples[i] + avgDistances[i] / hmw;
        }

        // Compute the final scenery score as the sum of individual direction scores
        float finalScore = 0;
        for (int i = 0; i < scores.Length; i++)
        {
            finalScore += scores[i];
        }

        // Cache the computed score for future reference
        if (!sceneryValues.ContainsKey(position))
        {
            sceneryValues.Add(position, finalScore);
        }
        else
        {
            sceneryValues[position] = finalScore;
        }

        return finalScore;
    }

    /// <summary>
    /// Creates a rocky area on the terrain by modifying height and
    /// texture (alpha map) within a specified radius from the given position.
    /// </summary>
    /// <param name="sourcePos">The center position of the rocky area.</param>
    /// <param name="radius">The radius of the rocky area.</param>
    void MakeRockyArea(Vector2Int sourcePos, float radius)
    {
        // Randomize radius values for variation
        float radiusX = radius - 0.5f + UnityEngine.Random.value;
        float radiusY = radius * radius / radiusX;

        // Calculate the region boundaries on the terrain
        int StartX = Mathf.Max((int)Mathf.Floor(sourcePos.x - radiusX / ScaleFactor), 0);
        int EndX = Mathf.Min((int)Mathf.Ceil(sourcePos.x + radiusX / ScaleFactor), hmw - 1);
        int StartY = Mathf.Max((int)Mathf.Floor(sourcePos.y - radiusY / ScaleFactor), 0);
        int EndY = Mathf.Min((int)Mathf.Ceil(sourcePos.y + radiusY / ScaleFactor), hmh - 1);

        if (IsOutOfBounds(new Vector2(StartX, StartY)) || IsOutOfBounds(new Vector2(EndX, EndY)))
        {
            return;
        }

        Debug.Log(sourcePos);
        Debug.Log($"({StartX}, {StartY}) - ({EndX}, {EndY})");

        // Get the height and alpha map (texture blending) for the region
        float[,] heights = CityTerrain.terrainData.GetHeights(
            StartX,
            StartY,
            EndX - StartX + 1,
            EndY - StartY + 1
        );
        float[,,] alphas = CityTerrain.terrainData.GetAlphamaps(
            StartX,
            StartY,
            EndX - StartX + 1,
            EndY - StartY + 1
        );

        // Calculate the center height of the selected region
        float centerHeight = heights[(EndY - StartY + 1) / 2, (EndX - StartX + 1) / 2];

        // Iterate through the region and modify heights and texture blending
        for (int i = StartX; i <= EndX; i++)
        {
            for (int j = StartY; j <= EndY; j++)
            {
                if (IsOutOfBounds(new Vector2(i, j)))
                {
                    continue;
                }

                // Calculate the distance from the source position
                Vector3 sourceToPos = new Vector3(i - sourcePos.x, j - sourcePos.y);
                // Randomize the target height for rocky area based on distance from the center
                float targetHeight =
                    centerHeight
                    + (UnityEngine.Random.value * 1.2f * (sourceToPos.magnitude / radius) - 0.3f)
                        * 0.001f;

                // If the position is within the radius and the height is lower than the target height
                if (
                    sourceToPos.magnitude < radius * (0.8f * UnityEngine.Random.value * 0.4f)
                    && heights[j - StartY, i - StartX] < targetHeight
                    && alphas[j - StartY, i - StartX, 2] < 0.5f
                )
                {
                    // Set new height for the terrain point
                    heights[j - StartY, i - StartX] = targetHeight;

                    // Set alpha values for rocky area (remove other textures and add rock)
                    alphas[j - StartY, i - StartX, SandChannel] = 0f;
                    alphas[j - StartY, i - StartX, TreeChannel] = 0f;
                    alphas[j - StartY, i - StartX, WaterChannel] = 0f;
                    alphas[j - StartY, i - StartX, RockChannel] = 1f;

                    Debug.Log($"({i}, {j}) new height {targetHeight}");
                }
            }
        }

        // Apply the modified heights and alpha maps back to the terrain
        CityTerrain.terrainData.SetHeights(StartX, StartY, heights);
        CityTerrain.terrainData.SetAlphamaps(StartX, StartY, alphas);
    }

    /// <summary>
    /// Clears the terrain texture map and applies a new texture based on the terrain height.
    /// </summary>
    void ClearMap()
    {
        // Retrieve current alpha maps of terrain
        float[,,] alphas = CityTerrain.terrainData.GetAlphamaps(0, 0, hmw - 1, hmh - 1);

        var terrainData = CityTerrain.terrainData;
        var alphaMapWidth = terrainData.alphamapWidth;
        var alphaMapHeight = terrainData.alphamapHeight;

        for (int i = 0; i < alphaMapWidth; ++i)
        {
            for (int j = 0; j < alphaMapHeight; ++j)
            {
                // Calculate sand texture value based on terrain height and random factor
                float sand =
                    UnityEngine.Random.Range(0.5f, 2f)
                    * (
                        0.2f
                        + 0.8f
                            * Mathf.Abs(
                                2
                                    * (
                                        CityTerrain.SampleHeight(new Vector3(j, 0, i))
                                        - (maxHeight / 2)
                                    )
                                    / maxHeight
                            )
                    );
                if (sand > 1)
                {
                    sand = 1;
                }

                // Get interpolated normal for terrain surface at this point
                Vector3 n = terrainData.GetInterpolatedNormal(j / (hmh - 1.0f), i / (hmw - 1.0f));

                // Calculate rock texture value based on surface slope
                float rock =
                    UnityEngine.Random.Range(0.5f, 2f) * (1.2f - 2 * Vector3.Dot(Vector3.up, n));
                if (rock > 1)
                {
                    rock = 1;
                }

                // Apply texture blkending based on calculated values for sand and rock
                if (rock > 0.5f)
                {
                    // Rock sand combination
                    alphas[i, j, 0] = sand * (1 - rock);
                    alphas[i, j, 1] = (1 - sand) * (1 - rock);
                    alphas[i, j, 2] = 0;
                    alphas[i, j, 3] = rock;
                }
                else
                {
                    // Sand and grass combination
                    alphas[i, j, 0] = sand;
                    alphas[i, j, 1] = 1 - sand;
                    alphas[i, j, 2] = 0;
                    alphas[i, j, 3] = 0;
                }
            }
        }

        // Set the modified alpha map back to the terrain
        CityTerrain.terrainData.SetAlphamaps(0, 0, alphas);
    }

    /// <summary>
    /// Checks if a given position is out of bounds based on the current grid dimensions.
    /// </summary>
    /// <param name="pos">The position to check.</param>
    /// <returns>True if the position is outside the valid grid bounds, otherwise false.</returns>
    private bool IsOutOfBounds(Vector2 pos)
    {
        if (hmw <= 0 || hmh <= 0)
        {
            Debug.LogError("Invalid grid dimensions: hmw or hmh is less than or equal to 0.");

            // If dimensions are invalid, consider all positions out of bounds
            return true;
        }

        return pos.x < 0 || pos.x >= hmw || pos.y < 0 || pos.y >= hmh;
    }

    /// <summary>
    /// Returns a float representing the percentage of people accommodated.
    /// </summary>
    /// <returns>Float representing the percentage of people
    /// accommodated relative to the initial population.</returns>
    public float GetAccommodationRate()
    {
        if (InitialPopulation == 0)
        {
            Debug.LogWarning("Initial population is 0. Cannot calculate accommodation rate.");
            return 0f;
        }

        return (float)accommodated / InitialPopulation;
    }

    public Vector2 GetCityCore()
    {
        return cityCore;
    }
}
