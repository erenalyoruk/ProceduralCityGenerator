using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Globalization;

public class GenerateFromRealMap : MonoBehaviour
{
    public float CenterLatitude;
    public float CenterLongitude;
    public float ScaleFactor = 8;
    public int ElevationResolution = 20;
    public bool UpdateTerrainHeights = false;
    private Vector2 centerXY, minXY, maxXY, minGPS, maxGPS;
    private int hmw, hmh;
    // Start is called before the first frame update
    void Start()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
        //determine corner coordinates
        hmw = this.gameObject.GetComponent<Terrain>().terrainData.heightmapResolution;
        hmh = this.gameObject.GetComponent<Terrain>().terrainData.heightmapResolution;

        centerXY = new Vector2((float)(MercatorProjection.lonToX(CenterLongitude)), (float)(MercatorProjection.latToY(CenterLatitude)));
        minXY = centerXY - (new Vector2(hmw, hmh)) * ScaleFactor * 0.5f;
        maxXY = centerXY + (new Vector2(hmw, hmh)) * ScaleFactor * 0.5f;
        Debug.Log(minXY);

        minGPS = new Vector2((float)(MercatorProjection.xToLon(minXY.x)), (float)(MercatorProjection.yToLat(minXY.y)));
        maxGPS = new Vector2((float)(MercatorProjection.xToLon(maxXY.x)), (float)(MercatorProjection.yToLat(maxXY.y)));

        //make a list of coordinates
        Vector2[,] geoCoordinates = new Vector2[ElevationResolution,ElevationResolution];
        float[,] elevations = new float[ElevationResolution, ElevationResolution];
        for (int i = 0; i < ElevationResolution; i++)
        {
            for (int j = 0; j < ElevationResolution; j++)
            {
                geoCoordinates[i, j] = minGPS + new Vector2((maxGPS.x - minGPS.x) * i / (ElevationResolution - 1), (maxGPS.y - minGPS.y) * j / (ElevationResolution - 1));
            }
        }

        //if the file doesn't exist
        string elevationsFilename = "Assets/Elevations/" + CenterLongitude + CenterLatitude + ScaleFactor + ElevationResolution;
        //check if the file downloaded
        if (!System.IO.File.Exists(elevationsFilename + "0.json"))
        {
            //download elevations from google maps
            GetElevation(geoCoordinates, elevationsFilename);
        }

        float minElevation = 10000;
        //read file and fill elevation values
        for (int i = 0; i < ElevationResolution; i++)
        {
            string dataAsJson = File.ReadAllText(elevationsFilename + i + ".json");
            JSONTileElevationData elevationData = JsonUtility.FromJson<JSONTileElevationData>(dataAsJson);
            for (int j = 0; j < ElevationResolution; j++)
            {
                elevations[j, i] = (float)(elevationData.results[j].elevation);
                if (elevations[j, i] < minElevation)
                    minElevation = elevations[j, i];
            }
        }
        for (int i = 0; i < ElevationResolution; i++)
        {
            for (int j = 0; j < ElevationResolution; j++)
            {
                elevations[i, j] -= minElevation;
            }
        }

        //update terrains height values
        float[,] heights = this.gameObject.GetComponent<Terrain>().terrainData.GetHeights(0, 0, hmw, hmh);
        
        //find horizontal elevations
        float stepSize = 1f * (hmw-1) / (ElevationResolution-1);
        Vector3 p1, p2, p3, p4;
        for (int j = 0; j < ElevationResolution; j++)
        {
            for (int i = 0; i < hmw; i++)
            {
                int ip2 = Mathf.FloorToInt(i / stepSize);
                int ip3 = Mathf.CeilToInt(i / stepSize);
                ip3 = ip3 <= ElevationResolution - 1 ? ip3 : ip3 - 1;
                int ip1 = ip2 > 0 ? ip2 - 1 : ip2;
                int ip4 = ip3 < ElevationResolution - 1 ? ip3 + 1 : ip3;

                p1 = new Vector3(ip1*ElevationResolution, elevations[ip1, j], j * stepSize);
                p2 = new Vector3(ip2 * ElevationResolution, elevations[ip2, j], j * stepSize);
                p3 = new Vector3(ip3 * ElevationResolution, elevations[ip3, j], j * stepSize);
                p4 = new Vector3(ip4 * ElevationResolution, elevations[ip4, j], j * stepSize);

                Vector3 crp = GetCatmullRomPosition((i / stepSize - ip2), p1, p2, p3, p4);
                heights[i, (int)(j*stepSize)] = crp.y / (600f * ScaleFactor);
            }
        }

        for (int i = 0; i < hmw; i++)
        {
            for (int j = 0; j < hmh; j++)
            {
                int jp2 = Mathf.FloorToInt(j / stepSize);
                int jp3 = Mathf.CeilToInt(j / stepSize);
                jp3 = jp3 <= ElevationResolution - 1 ? jp3 : jp3 - 1;
                int jp1 = jp2 > 0 ? jp2 - 1 : jp2;
                int jp4 = jp3 < ElevationResolution - 1 ? jp3 + 1 : jp3;

                p1 = new Vector3(i, heights[i, (int)(jp1 * stepSize)], (int)(jp1 * stepSize));
                p2 = new Vector3(i, heights[i, (int)(jp2 * stepSize)], (int)(jp2 * stepSize));
                p3 = new Vector3(i, heights[i, (int)(jp3 * stepSize)], (int)(jp3 * stepSize));
                p4 = new Vector3(i, heights[i, (int)(jp4 * stepSize)], (int)(jp4 * stepSize));

                p1 = p2 - (p2 - p1) * 0.5f;
                p4 = p3 + (p4 - p3) * 0.5f;

                Vector3 crp = GetCatmullRomPosition(j/stepSize - jp2, p1, p2, p3, p4);
                heights[i, j] = crp.y;
            }
        }
        //find vertical elevations

        if(UpdateTerrainHeights)
            gameObject.GetComponent<Terrain>().terrainData.SetHeights(0, 0, heights);

        BuildGroundTiles();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void GetElevation(Vector2[,] coords, string outputFile)
    {
        if (coords.Length == 0)
            return;
        for (int i = 0; i < ElevationResolution; i++)
        {
            //check if the file exists
            if (System.IO.File.Exists(outputFile + i + ".json"))
                continue;
                

            //https://maps.googleapis.com/maps/api/elevation/json?locations=40.714728,-73.998672|-34.397,150.644&key=AIzaSyDy2lR8XOqHynPA4ayoqfXdrgL2Ec3a0b0
            string url = "https://maps.googleapis.com/maps/api/elevation/json?locations=";
            for (int j = 0; j < ElevationResolution; j++)
            {
                if((j) == 0)
                    url += coords[i, j].y + "," + coords[i, j].x;
                else
                    url += "%7C" + coords[i, j].y + "," + coords[i, j].x;
            }
            url += "^&key=AIzaSyDy2lR8XOqHynPA4ayoqfXdrgL2Ec3a0b0";

            string dl_cmd = "/c C:\\Users\\AYBU\\Downloads\\curl -l " + url + " > " + outputFile + i + ".json";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = dl_cmd;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
        
    }

    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        //The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        //The cubic polynomial: a + b * t + c * t^2 + d * t^3
        Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));

        return pos;
    }

    public void BuildGroundTiles()
    {

        int zoomLevel = 17;
        int imDownloadSize = 512;


        float scaleY = 512 * (0.001F) / (245.0F / Mathf.Pow(2, 18 - zoomLevel)); //latitude difference across an image
        Vector2 difGPS = maxGPS - minGPS;
        Vector2 maxPx = new Vector2((float)(MercatorProjection.lonToX(maxGPS[0])), (float)(MercatorProjection.latToY(maxGPS[1])));
        Vector2 minPx = new Vector2((float)(MercatorProjection.lonToX(minGPS[0])), (float)(MercatorProjection.latToY(minGPS[1])));
        Vector2 difXY = maxXY - minXY;

        float scaleX = scaleY * 111.22F / 84.44F; // (difXY[1]/difGPS[1]) * (difGPS[0] / difXY[0]);

        //determine the necessary images
        float latInterval = scaleY * 0.9F;//to get rid of the bottom string in the images
        float lngInterval = scaleX;
        Vector2 centerGPS = (minGPS + maxGPS) * 0.5f;
        int minx = Mathf.FloorToInt((float)(0.5F + (minGPS[0] - centerGPS[0]) / lngInterval));
        int maxx = Mathf.FloorToInt((float)(0.5F + (maxGPS[0] - centerGPS[0]) / lngInterval));
        int miny = Mathf.FloorToInt((float)(0.5F + (minGPS[1] - centerGPS[1]) / latInterval));
        int maxy = Mathf.FloorToInt((float)(0.5F + (maxGPS[1] - centerGPS[1]) / latInterval));

        //initialize elevations
        int tileGridSize = 4;//grid size

        GameObject tiles = new GameObject("Ground tiles");
        for (int i = minx; i <= maxx; i++)
        {//left to right
            for (int j = miny; j <= maxy; j++)
            {//top to bottom

                float lng = lngInterval * i + centerGPS[0];
                float lat = latInterval * j + centerGPS[1];
                //generate filename
                string filename = "Assets/Resources/Ground/" + lng + lat + zoomLevel + ".png";
                //download the file
                if (!System.IO.File.Exists(filename))
                {
                    string url = generateUrlForSatellite(lat, lng, zoomLevel, imDownloadSize);
                    //Debug.Log( url + " " + filename );
                    ImageFromURL(url, filename);
                }

                //corners of the tile
                Vector2 gps0 = new Vector2(centerGPS[0] + lngInterval * (i - 0.5F), centerGPS[1] + latInterval * (j - 0.45F));
                Vector2 v0 = FromGPS(new Vector2(centerGPS[0] + lngInterval * (i - 0.5F), centerGPS[1] + latInterval * (j - 0.45F)));
                Vector2 v1 = FromGPS(new Vector2(centerGPS[0] + lngInterval * (i + 0.5F), centerGPS[1] + latInterval * (j - 0.45F)));
                Vector2 v2 = FromGPS(new Vector2(centerGPS[0] + lngInterval * (i + 0.5F), centerGPS[1] + latInterval * (j + 0.55F)));
                Vector2 v3 = FromGPS(new Vector2(centerGPS[0] + lngInterval * (i - 0.5F), centerGPS[1] + latInterval * (j + 0.55F)));
                Vector2 vc = (v0 + v2) / 2.0F;

                //generate the tile object
                GameObject tile = new GameObject("tile" + (i - minx) + "_" + (j - miny));
                tile.transform.SetParent(tiles.transform, false);
                // Add the mesh filter and renderer components to the object
                MeshFilter mf = tile.AddComponent<MeshFilter>();
                MeshRenderer mr = tile.AddComponent<MeshRenderer>();

                // Create the collections for the object's vertices, indices, UVs etc.
                List<Vector3> vectors = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<int> indices = new List<int>();

                tile.transform.position = new Vector3(vc[0], -10, vc[1]);

                //Generate tile vertices
                for (int gi = 0; gi <= tileGridSize; gi++)
                {
                    float gridalpha = gi * 1.0F / tileGridSize;
                    for (int gj = 0; gj <= tileGridSize; gj++)
                    {
                        float gridbeta = gj * 1.0F / tileGridSize;
                        float lgx = v0[0] - vc[0] + gridalpha * (v2[0] - v0[0]);
                        float lgy = v0[1] - vc[1] + gridbeta * (v2[1] - v0[1]);
                        float gx = lgx + vc[0];
                        float gy = lgy + vc[1];
                        gx = Mathf.Max(gx,0F);
                        gx = Mathf.Min(gx, hmw-1f);
                        gy = Mathf.Max(gy,0f);
                        gy = Mathf.Min(gy, hmh-1f);
                        float elv = 1.0F * gameObject.GetComponent<Terrain>().terrainData.GetInterpolatedHeight(gx / (hmw - 1.0f), gy / (hmh - 1.0f));
                        vectors.Add(new Vector3(lgx, elv, lgy));
                        Vector3 nrml = 1.0F * gameObject.GetComponent<Terrain>().terrainData.GetInterpolatedNormal(gx / (hmw - 1.0f), gy / (hmh - 1.0f)); 
                        normals.Add(nrml);
                        uvs.Add(new Vector2(gridalpha, 0.1F + 0.9F * gridbeta));
                        
                    }
                }
                //Generate indices for tiles
                for (int gi = 0; gi < tileGridSize; gi++)
                {
                    for (int gj = 0; gj < tileGridSize; gj++)
                    {
                        indices.Add(gi * (tileGridSize + 1) + gj);
                        indices.Add((gi + 1) * (tileGridSize + 1) + (gj + 1));
                        indices.Add((gi + 1) * (tileGridSize + 1) + (gj));
                        indices.Add(gi * (tileGridSize + 1) + gj);
                        indices.Add((gi) * (tileGridSize + 1) + (gj + 1));
                        indices.Add((gi + 1) * (tileGridSize + 1) + (gj + 1));
                    }
                }

                Texture tex2D = (Texture)Resources.Load("Ground/" + lng + lat + zoomLevel);
                Material material = new Material(Shader.Find("Unlit/Texture"));
                material.SetTexture("_MainTex", tex2D);
                mr.material = material;

                //mr.material.SetTexture("_MainTex", tex2D);
                
                // Apply the data to the mesh
                mf.mesh.vertices = vectors.ToArray();
                mf.mesh.normals = normals.ToArray();
                mf.mesh.triangles = indices.ToArray();
                mf.mesh.uv = uvs.ToArray();
                
            }
        }
    }

    private Vector2 FromGPS(Vector2 gpsPos)
    {
        //find game coordinates
        Vector2 pXY = new Vector2((float)(MercatorProjection.lonToX(gpsPos.x)), (float)(MercatorProjection.latToY(gpsPos.y)));
        return (pXY - minXY)/ScaleFactor;
    }

    string generateUrlForSatellite(double latitude, double longitude, int zoomLevel, int size)
    {
        string result;
        result = "https://maps.googleapis.com/maps/api/staticmap?center=" + latitude + "," + longitude
            + "^&zoom=" + zoomLevel
            + "^&size=" + size + "x" + size
            + "^&maptype=satellite^&key=AIzaSyDy2lR8XOqHynPA4ayoqfXdrgL2Ec3a0b0";
        return result;
    }
    

    int ImageFromURL(string url, string outputFile)
    {

        //https://maps.googleapis.com/maps/api/elevation/json?locations=40.714728,-73.998672|-34.397,150.644&key=AIzaSyDy2lR8XOqHynPA4ayoqfXdrgL2Ec3a0b0
        string dl_cmd = "/c C:\\Users\\AYBU\\Downloads\\curl -l " + url + " > " + outputFile;
        Debug.Log(url);
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = dl_cmd;
        process.StartInfo = startInfo;
        process.Start();

        return 1;
    }
}
