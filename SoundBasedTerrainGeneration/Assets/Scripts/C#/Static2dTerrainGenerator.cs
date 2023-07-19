using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Static2dTerrainGenerator : TerrainGeneration
{
    [Range(0, 2)]
    [SerializeField] private float scale = 1;
    [SerializeField] private int minStoneheight, maxStoneHeight;
    [SerializeField] private Tilemap dirtTilemap, grassTilemap, stoneTilemap;
    [SerializeField] private Tile dirt, grass, stone;
    [Range(0, 100)]
    [SerializeField] private float heightValue, smoothness;

    private int min, max;
    private float previousScale;
    private int[] waveformArray;
    private int width, height;

    void Start()
    {
        Generation();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Generation();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            //ClearTerrain();
        }
    }

    private void OnValidate()
    {
        if (scale != previousScale)
        {
            if (waveformArray != null)
            {
                UpdateTiles();
            }
            previousScale = scale;
        }
    }

    private void ClearTerrain()
    {
        stoneTilemap.ClearAllTiles();
        dirtTilemap.ClearAllTiles();
        grassTilemap.ClearAllTiles();
    }

    private void UpdateTiles()
    {

    }

    private void SpawnTiles()
    {
        Vector3Int[] positions = new Vector3Int[width * height];
        TileBase[] tileArray = new TileBase[positions.Length];

        Debug.Log(width/50);
        Debug.Log(height/50);

        for (int i = 0; i< width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //stoneTilemap.SetTile(new Vector3Int(i, j, 0), stone);
                positions[(i * height) + j] = new Vector3Int(i, j, 0);
                tileArray[(i * height) + j] = stone;
            }
        }

        stoneTilemap.SetTiles(positions, tileArray);

        //int mean = min + ((max - min) / 2);

        //for (int x = 0; x < waveformArray.Length; x++)//This will help spawn a tile on the x axis
        //{
        //    int height = mean + (int)((waveformArray[x] - mean) * scale);

        //    int minStoneSpawnDistance = height - minStoneheight;
        //    int maxStoneSpawnDistance = height - maxStoneHeight;
        //    int totalStoneSpawnDistance = Random.Range(minStoneSpawnDistance, maxStoneSpawnDistance);
        //    for (int y = 0; y < height; y++)//This will help spawn a tile on the y axis
        //    {
        //        if (y < totalStoneSpawnDistance)
        //        {
        //            //spawnObj(stone, x, y);
        //            stoneTilemap.SetTile(new Vector3Int(x, y, 0), stone);
        //        }
        //        else
        //        {
        //            // spawnObj(dirt, x, y);
        //            dirtTilemap.SetTile(new Vector3Int(x, y, 0), dirt);
        //        }

        //    }
        //    if (totalStoneSpawnDistance == height)
        //    {
        //        // spawnObj(stone, x, height);
        //        stoneTilemap.SetTile(new Vector3Int(x, height, 0), stone);
        //    }
        //    else
        //    {
        //        //spawnObj(grass, x, height);
        //        grassTilemap.SetTile(new Vector3Int(x, height, 0), grass);
        //    }
        //}
    }

    override protected void Generation()
    {
        base.Generation();

        string waveformImage = PathCombine(Application.dataPath, "GeneratedPlots/waveform.png"); // Assign the waveform image in the Inspector
        waveformArray = ConvertWaveformImageToArray(LoadPNG(waveformImage));
        //SpawnTiles();

        int[] test =
            {1,2,1,2,3,4,5,4,3,2,1};

        GenerateGraphMesh(waveformArray);
    }

    private int[] ConvertWaveformImageToArray(Texture2D image)
    {
        Color32[] pixels = image.GetPixels32();
        width = image.width;
        height = image.height;

        List<int> waveformList = new List<int>();

        for (int x = 0; x < width; x++)
        {
            bool hasWaveform = false;

            for (int y = 0; y < height; y++)
            {
                Color32 pixelColor = pixels[x + y * width];

                // Check if the pixel is not fully transparent
                if (pixelColor.a > 0)
                {
                    waveformList.Add(y);
                    hasWaveform = true;
                    break;
                }
            }

            if (!hasWaveform)
            {
                waveformList.Add(0); // Use 0 as the default value when no waveform is present
            }

            if(waveformList.Count == 1)
            {
                min = max = waveformList.Last();
            } else if(waveformList.Last() < min)
            {
                min = waveformList.Last();
            } else if(waveformList.Last() > max)
            {
                max = waveformList.Last();
            }
        }

        return waveformList.ToArray();
    }

    private string PathCombine(string path1, string path2)
    {
        return System.IO.Path.Combine(path1, path2);
    }

    private Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }

    void GenerateGraphMesh(int [] graphData)
    {
        if (graphData == null || graphData.Length == 0)
        {
            Debug.LogError("Graph data is empty!");
            return;
        }

        int numVertices = graphData.Length * 2; // Two vertices per data point (y-axis and x-axis)
        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[(graphData.Length - 1) * 6]; // Two triangles per data point

        for (int i = 0; i < graphData.Length; i++)
        {
            float x = i;
            float y = graphData[i];

            // Vertex on the y-axis (value of the function)
            vertices[i * 2] = new Vector3(x, y, 0);

            // Vertex on the x-axis
            vertices[i * 2 + 1] = new Vector3(x, 0, 0);

            // Create triangles for the current data point (except for the last one)
            if (i < graphData.Length - 1)
            {
                int triangleIndex = i * 6;
                triangles[triangleIndex] = i * 2;
                triangles[triangleIndex + 2] = (i + 1) * 2 + 1; // Reverse the winding order here
                triangles[triangleIndex + 1] = (i + 1) * 2;

                triangles[triangleIndex + 3] = i * 2;
                triangles[triangleIndex + 5] = i * 2 + 1; // Reverse the winding order here
                triangles[triangleIndex + 4] = (i + 1) * 2 + 1;
            }
        }

        // Get the existing MeshFilter component
        MeshFilter meshFilter = GetComponent<MeshFilter>();


        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Assign the generated mesh to the MeshFilter
        meshFilter.mesh = mesh;
    }
}
