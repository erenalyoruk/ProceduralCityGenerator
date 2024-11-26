using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.IntegerTime;
using UnityEngine;

public class PCWalls : PCBuilding
{
    Vector2Int CityBoundaryMin;
    private float colorDarkening = 0.6f;
    private float houseWallHeight = 8f / ProceduralCityGenerator.ScaleFactor;
    private float houseWallThickness = 2f / ProceduralCityGenerator.ScaleFactor;
    private float doorWidth = 4f / ProceduralCityGenerator.ScaleFactor;
    private float windowWidth = 0.5f / ProceduralCityGenerator.ScaleFactor;

    public PCWalls(Vector2Int cmin, Vector2Int cmax, Texture t)
        : base("City Walls")
    {
        CityBoundaryMin = cmin;
        texture = t;
        GenerateModel();
    }

    /// <summary>
    /// Generates the model for the city walls by processing the occupied city grid and building walls around the occupied areas.
    /// </summary>
    private void GenerateModel()
    {
        ProceduralCityGenerator pcg = (ProceduralCityGenerator)
            GameObject.Find("CityBuilder").GetComponent(typeof(ProceduralCityGenerator));
        List<Vector3> points = new List<Vector3>();

        // Loop through city grid to find occupied areas and sample terrain heights at the neighborhood points
        for (int i = 0; i < pcg.cw; i++)
        {
            for (int j = 0; j < pcg.ch; j++)
            {
                if (pcg.CityOccupation[i, j] == ProceduralCityGenerator.Occupation.Occupied)
                {
                    // Generate 4 neighborhood points
                    Vector3[] neighbors =
                    {
                        new Vector3(i + CityBoundaryMin.x - 1f, 0f, j + CityBoundaryMin.y - 1f),
                        new Vector3(i + CityBoundaryMin.x - 1f, 0f, j + CityBoundaryMin.y + 1f),
                        new Vector3(i + CityBoundaryMin.x + 1f, 0f, j + CityBoundaryMin.y - 1f),
                        new Vector3(i + CityBoundaryMin.x + 1f, 0f, j + CityBoundaryMin.y + 1f),
                    };

                    // Sample terrain height for each point
                    foreach (Vector3 neighbor in neighbors)
                    {
                        Vector3 modifiedNeighbor = neighbor;
                        modifiedNeighbor.y = pcg.CityTerrain.SampleHeight(neighbor); // Assign terrain height
                        points.Add(modifiedNeighbor); // Add the point to the list
                    }
                }
            }
        }

        // If we have more than two points, proceed with sorting and wall generation
        if (points.Count > 2)
        {
            // Sort points based on their z-coordinate to arrange them in the correct order
            points.Sort((a, b) => a.z.CompareTo(b.z));

            List<Vector3> wallPoints = new List<Vector3>
            {
                points[0], // Add first two points for wall construction
                points[1],
            };

            // Build the right-side walls by scanning from bottom to top
            BuildWalls(points, wallPoints, isRightWall: true);

            // Build the left-side walls by scanning from top to bottom
            BuildWalls(points, wallPoints, isRightWall: false);

            // Create a GameObject to hold the walls and add each wall segment
            model = new GameObject("CityWalls");
            for (int i = 0; i < wallPoints.Count - 1; i++)
            {
                HouseWall(wallPoints[i], wallPoints[i + 1], false, false); // Generate wall between consecutive points
            }
        }
    }

    /// <summary>
    /// Helper method to build walls either to the right or left based on the direction.
    /// </summary>
    /// <param name="points">List of points defining the city's boundary.</param>
    /// <param name="wallPoints">List to store the points defining the walls.</param>
    /// <param name="isRightWall">Indicates whether to build the right wall (true) or left wall (false).</param>
    private void BuildWalls(List<Vector3> points, List<Vector3> wallPoints, bool isRightWall)
    {
        // Define scanning range based on wall direction (bottom-to-top or top-to-bottom)
        int startIndex = isRightWall ? 2 : points.Count - 2;
        int endIndex = isRightWall ? points.Count : -1;
        int step = isRightWall ? 1 : -1;

        for (int i = startIndex; i != endIndex; i += step)
        {
            // Check the direction of the new segment relative to the last two wall points
            Vector3 newSegment = points[i] - wallPoints[wallPoints.Count - 1];
            Vector3 lastSegment =
                wallPoints[wallPoints.Count - 1] - wallPoints[wallPoints.Count - 2];

            // Roll back until a left turn is found (when cross product is negative)
            while (
                wallPoints.Count > 2
                && newSegment.x * lastSegment.z - newSegment.z * lastSegment.x >= 0
            )
            {
                wallPoints.RemoveAt(wallPoints.Count - 1); // Remove last point if not a valid turn
                newSegment = points[i] - wallPoints[wallPoints.Count - 1]; // Recalculate segment
                lastSegment = wallPoints[wallPoints.Count - 1] - wallPoints[wallPoints.Count - 2]; // Recalculate last segment
            }

            wallPoints.Add(points[i]); // Add the new point for wall construction
        }
    }

    /// <summary>
    /// Creates a wall segment for the house with optional doors and windows.
    /// It recursively divides the wall if necessary for creating door/window sections.
    /// </summary>
    /// <param name="from">Starting point of the wall segment</param>
    /// <param name="to">Ending point of the wall segment</param>
    /// <param name="door">Indicates if a door should be included in the wall</param>
    /// <param name="windows">Indicates if windows should be included in the wall</param>
    private void HouseWall(Vector3 from, Vector3 to, bool door = false, bool windows = false)
    {
        Vector3 dir = to - from;

        // Recursively divide wall into smaller segments if the wall is tall
        if (Mathf.Abs(dir[1]) > houseWallHeight / 4)
        {
            // Divide into three segments
            HouseWall(from, from + dir / 3, false, windows);
            HouseWall(from + dir / 3, from + 2 * dir / 3, door, windows);
            HouseWall(from + 2 * dir / 3, to, false, windows);
            return;
        }

        dir.Normalize();
        Vector3 towardsIn = Vector3.Cross(dir, Vector3.up);

        // Create the wall based on the presence of a door or windows
        if (!door && !windows)
        {
            CreateHouseWall(from, to, dir, towardsIn);
        }
        else if (door)
        {
            HandleDoorPlacement(from, to, dir, windows);
        }
        else if (windows)
        {
            HandleWindowPlacement(from, to, dir);
        }
    }

    /// <summary>
    /// Creates a basic wall section without any door or window.
    /// </summary>
    /// <param name="from">Starting point of the wall</param>
    /// <param name="to">Ending point of the wall</param>
    /// <param name="dir">Direction vector of the wall</param>
    /// <param name="towardsIn">Perpendicular direction to align the wall correctly</param>
    private void CreateHouseWall(Vector3 from, Vector3 to, Vector3 dir, Vector3 towardsIn)
    {
        GameObject houseWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer houseWallRenderer = houseWall.GetComponent<Renderer>();

        // Apply texture and material
        houseWallRenderer.material.SetTexture("_MainTex", texture);
        houseWallRenderer.material.SetTextureScale(
            "_MainTex",
            new Vector2((to - from).magnitude / 4f, 1)
        );
        houseWallRenderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        houseWallRenderer.material.SetColor("_Color", Color.white * colorDarkening);

        // Set wall position and rotation
        houseWall.transform.SetParent(model.transform, false);
        houseWall.transform.localScale = new Vector3(
            (to - from).magnitude,
            houseWallHeight,
            houseWallThickness
        );
        houseWall.transform.localPosition =
            (from + to + towardsIn * houseWallThickness + Vector3.up * houseWallHeight / 2f) / 2f;
        houseWall.transform.Rotate(Vector3.up, Vector3.SignedAngle(Vector3.right, dir, Vector3.up));
    }

    /// <summary>
    /// Handles the placement of a door in the wall. If the space is insufficient, it logs a message.
    /// </summary>
    /// <param name="from">Starting point of the wall</param>
    /// <param name="to">Ending point of the wall</param>
    /// <param name="dir">Direction vector of the wall</param>
    /// <param name="windows">Indicates if windows should be placed around the door</param>
    private void HandleDoorPlacement(Vector3 from, Vector3 to, Vector3 dir, bool windows)
    {
        if ((to - from).magnitude <= doorWidth)
        {
            HouseWall(from, to, false, windows); // Recurse without a door if there's not enough space
            Debug.Log("Not enough room for a door!");
        }
        else
        {
            // Make a centered door
            Door((from + to - dir * doorWidth) / 2, (from + to + dir * doorWidth) / 2);
            HouseWall(from, (from + to - dir * doorWidth) / 2, false, windows);
            HouseWall((from + to + dir * doorWidth) / 2, to, false, windows);
        }
    }

    /// <summary>
    /// Handles the placement of windows in the wall, evenly distributing them based on the space available.
    /// </summary>
    /// <param name="from">Starting point of the wall</param>
    /// <param name="to">Ending point of the wall</param>
    /// <param name="dir">Direction vector of the wall</param>
    private void HandleWindowPlacement(Vector3 from, Vector3 to, Vector3 dir)
    {
        float sideLength = (to - from).magnitude;
        int windowsCount = (int)(
            (sideLength - houseWallThickness * 2)
            / (windowWidth * ProceduralCityGenerator.GoldenRatio)
        );

        // Calculate the spacing between windows
        float intervalLength = (sideLength - windowsCount * windowWidth) / (windowsCount + 1f);
        HouseWall(from, from + dir * intervalLength); // Left segment before first window

        Vector3 windowFrom = from + dir * intervalLength;
        for (int i = 0; i < windowsCount; i++)
        {
            Window(windowFrom, windowFrom + dir * windowWidth); // Create window
            windowFrom += dir * (windowWidth + intervalLength); // Move to next window position
            HouseWall(windowFrom - dir * intervalLength, windowFrom); // Wall segment between windows
        }
    }

    /// <summary>
    /// Creates a window on a building wall by generating the necessary window elements
    /// (under window, over window, and inner window structure) and adjusting their textures.
    /// </summary>
    /// <param name="from">Starting point of the window (left side)</param>
    /// <param name="to">Ending point of the window (right side)</param>
    private void Window(Vector3 from, Vector3 to)
    {
        // Calculate the direction and the cross direction for proper placement
        Vector3 dir = (to - from).normalized;
        float windowLength = (to - from).magnitude;

        // Create and position the window elements
        CreateWindowElement(
            from,
            to,
            houseWallHeight / 3,
            houseWallThickness,
            windowLength,
            Vector3.up * houseWallHeight / 3f
        );
        CreateWindowElement(
            from,
            to,
            houseWallHeight / 6,
            houseWallThickness,
            windowLength,
            Vector3.up * (houseWallHeight / 12f) + Vector3.up * houseWallHeight / 2f
        );

        // Define window corners for the inner frame
        Vector3 upperLeft = from + Vector3.up * houseWallHeight / 3f;
        Vector3 lowerLeft = from;
        Vector3 upperRight = to + Vector3.up * houseWallHeight / 3f;
        Vector3 lowerRight = to;

        // Create the private region for the window's internal area
        AddPrivateRegion(upperLeft, lowerLeft, upperRight, lowerRight);

        // Create the frame and inner window bars (eliminate redundant code by calling MakeElongatedCube)
        CreateWindowFrame(upperLeft, lowerLeft, upperRight, lowerRight);
    }

    /// <summary>
    /// Creates a window element (either under or over the window) with the given parameters.
    /// </summary>
    /// <param name="from">Starting point of the window</param>
    /// <param name="to">Ending point of the window</param>
    /// <param name="height">Height of the window element</param>
    /// <param name="thickness">Thickness of the wall</param>
    /// <param name="length">Length of the window element</param>
    /// <param name="offset">Offset to adjust the position of the window element</param>
    private void CreateWindowElement(
        Vector3 from,
        Vector3 to,
        float height,
        float thickness,
        float length,
        Vector3 offset
    )
    {
        // Create the window element (cube)
        GameObject windowElement = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowElement.transform.SetParent(model.transform, false);
        windowElement.transform.localScale = new Vector3(length, height, thickness);

        // Set the texture and material properties
        ApplyWindowMaterial(windowElement.GetComponent<Renderer>(), length);

        // Position and rotate the window element
        Vector3 direction = (to - from).normalized;
        Vector3 towardsIn = Vector3.Cross(direction, Vector3.up);
        windowElement.transform.localPosition = (from + to + towardsIn * thickness + offset) / 2f;
        windowElement.transform.Rotate(
            Vector3.up,
            Vector3.SignedAngle(direction, Vector3.right, Vector3.up)
        );
    }

    /// <summary>
    /// Applies the material and texture to a window element's renderer.
    /// </summary>
    /// <param name="renderer">Renderer of the window element</param>
    /// <param name="length">Length of the window element (used for texture scaling)</param>
    private void ApplyWindowMaterial(Renderer renderer, float length)
    {
        renderer.material.SetTexture("_MainTex", texture);
        renderer.material.SetTextureScale("_MainTex", new Vector2(length / 4f, 0.3f));
        renderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        renderer.material.SetColor("_Color", Color.white * colorDarkening);
    }

    /// <summary>
    /// Creates the frame for the window by adding elongated cubes for the sides and top/bottom borders.
    /// </summary>
    /// <param name="upperLeft">The upper-left corner of the window</param>
    /// <param name="lowerLeft">The lower-left corner of the window</param>
    /// <param name="upperRight">The upper-right corner of the window</param>
    /// <param name="lowerRight">The lower-right corner of the window</param>
    private void CreateWindowFrame(
        Vector3 upperLeft,
        Vector3 lowerLeft,
        Vector3 upperRight,
        Vector3 lowerRight
    )
    {
        // Create the top, bottom, left, and right frame bars
        MakeElongatedCube(lowerLeft, lowerRight, 0.15f); // Bottom bar
        MakeElongatedCube(upperLeft, upperRight, 0.15f); // Top bar
        MakeElongatedCube(upperLeft, lowerLeft, 0.15f); // Left bar
        MakeElongatedCube(upperRight, lowerRight, 0.15f); // Right bar

        // Create inner window bars
        MakeElongatedCube((upperLeft + upperRight) / 2, (lowerLeft + lowerRight) / 2, 0.05f); // Vertical middle bar
        MakeElongatedCube(
            (upperLeft * 2f + upperRight) / 3,
            (lowerLeft * 2f + lowerRight) / 3,
            0.05f
        ); // Left inner bar
        MakeElongatedCube(
            (upperLeft + upperRight * 2f) / 3,
            (lowerLeft + lowerRight * 2f) / 3,
            0.05f
        ); // Right inner bar
    }

    /// <summary>
    /// Creates a cube with a specified width that connects two points.
    /// </summary>
    /// <param name="from">The start position of the cube.</param>
    /// <param name="to">The end position of the cube.</param>
    /// <param name="width">Width of the cube.</param>
    private void MakeElongatedCube(Vector3 from, Vector3 to, float width)
    {
        // Create the cube
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Set material color
        Renderer houseWallRenderer = cube.GetComponent<Renderer>();
        houseWallRenderer.material.SetColor("_Color", Color.white * 0.8f * colorDarkening);

        // Calculate the direction and magnitude
        var dir = to - from;
        var length = dir.magnitude;
        dir.Normalize();

        // Set parent and scale
        cube.transform.SetParent(model.transform, false);
        cube.transform.localScale = new Vector3(length + width, width, width);

        // Set position at the midpoint between from and to
        cube.transform.localPosition = (from + to) / 2f;

        // Calculate the rotation axis and angle
        var crossDir = Vector3.Cross(Vector3.right, dir);
        var angle = Vector3.SignedAngle(Vector3.right, dir, crossDir);

        // Apply the rotation
        cube.transform.Rotate(crossDir, angle);
    }

    /// <summary>
    /// Generates a door with a frame at the specified positions in a wall.
    /// This method creates an over-door element, the door object itself, and a frame around the door.
    /// </summary>
    /// <param name="from">The start position of the door.</param>
    /// <param name="to">The end position of the door.</param>
    private void Door(Vector3 from, Vector3 to)
    {
        var dir = (to - from).normalized;
        var towardsIn = Vector3.Cross(dir, Vector3.up);
        var doorLength = (to - from).magnitude;

        var overDoor = CreateDoorElement(
            doorLength,
            houseWallHeight / 6,
            houseWallThickness,
            from,
            to,
            towardsIn,
            22f,
            1f
        );
        SetDoorMaterial(overDoor.GetComponent<Renderer>(), doorLength);

        var doorObject = CreateDoorElement(
            doorLength,
            5f * houseWallHeight / 6,
            houseWallThickness * 0.25f,
            from,
            to,
            towardsIn,
            10f,
            1f
        );
        SetDoorMaterial(
            doorObject.GetComponent<Renderer>(),
            doorLength,
            new Color(0.3f, 0.22f, 0.05f) * UnityEngine.Random.value
        );

        CreateDoorFrame(from, to, towardsIn, houseWallThickness);
    }

    /// <summary>
    /// Creates a cube representing either a door or an over-door element.
    /// The cube is positioned and scaled based on the provided parameters.
    /// </summary>
    /// <param name="length">The length of the door or over-door.</param>
    /// <param name="height">The height of the door or over-door.</param>
    /// <param name="thickness">The thickness of the door or over-door.</param>
    /// <param name="from">The start position of the door.</param>
    /// <param name="to">The end position of the door.</param>
    /// <param name="towardsIn">The direction vector perpendicular to the wall, used for adjusting the door's position.</param>
    /// <param name="offsetY">The offset for vertical positioning of the door.</param>
    /// <param name="scaleMultiplier">Multiplier for scaling the door size.</param>
    /// <returns>A GameObject representing the door element.</returns>
    private GameObject CreateDoorElement(
        float length,
        float height,
        float thickness,
        Vector3 from,
        Vector3 to,
        Vector3 towardsIn,
        float offsetY,
        float scaleMultiplier
    )
    {
        GameObject doorElement = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorElement.transform.SetParent(model.transform, false);
        doorElement.transform.localScale = new Vector3(length, height, thickness);
        doorElement.transform.localPosition =
            (from + to + towardsIn * thickness + offsetY * Vector3.up * houseWallHeight / 12f) / 2f;
        doorElement.transform.Rotate(
            Vector3.up,
            Vector3.SignedAngle(to - from, Vector3.right, Vector3.up)
        );

        return doorElement;
    }

    /// <summary>
    /// Applies material properties to the door, including texture and color adjustments.
    /// </summary>
    /// <param name="renderer">The Renderer component of the door element.</param>
    /// <param name="doorLength">The length of the door used to scale the texture.</param>
    /// <param name="color">The optional color for the door. If null, default color is applied.</param>
    private void SetDoorMaterial(Renderer renderer, float doorLength, Color? color = null)
    {
        renderer.material.SetTexture("_MainTex", texture);
        renderer.material.SetTextureScale("_MainTex", new Vector2(doorLength / 4f, 0.3f));
        renderer.material.SetTextureOffset(
            "_MainTex",
            new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)
        );
        renderer.material.SetColor("_Color", color ?? Color.white * colorDarkening);
    }

    /// <summary>
    /// Creates a door frame around the door using elongated cubes to represent the frame's top, bottom, and sides.
    /// </summary>
    /// <param name="from">The start position of the door.</param>
    /// <param name="to">The end position of the door.</param>
    /// <param name="towardsIn">The direction perpendicular to the wall used for adjusting the frame's position.</param>
    /// <param name="thickness">The thickness of the wall, which also determines the frame's size.</param>

    private void CreateDoorFrame(Vector3 from, Vector3 to, Vector3 towardsIn, float thickness)
    {
        // Frame top and bottom
        MakeElongatedCube(
            from + towardsIn * thickness * 0.25f,
            to + towardsIn * thickness * 0.25f,
            thickness * 0.4f
        );
        MakeElongatedCube(
            from + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * thickness * 0.25f,
            to + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * thickness * 0.25f,
            thickness * 0.4f
        );

        // Frame sides
        MakeElongatedCube(
            from + towardsIn * thickness * 0.25f,
            from + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * thickness * 0.25f,
            thickness * 0.4f
        );
        MakeElongatedCube(
            to + towardsIn * thickness * 0.25f,
            to + Vector3.up * 5f * houseWallHeight / 6 + towardsIn * thickness * 0.25f,
            thickness * 0.4f
        );
    }
}
