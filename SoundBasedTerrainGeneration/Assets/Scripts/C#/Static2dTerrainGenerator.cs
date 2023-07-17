using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Static2dTerrainGenerator : TerrainGeneration
{
    [Range(0, 2)]
    [SerializeField] private float scale = 1;
    [SerializeField] private int width;
    [SerializeField] private int minStoneheight, maxStoneHeight;
    [SerializeField] private Tilemap dirtTilemap, grassTilemap, stoneTilemap;
    [SerializeField] private Tile dirt, grass, stone;
    [Range(0, 100)]
    [SerializeField] private float heightValue, smoothness;

    private int min, max;
    private float previousScale;
    private int[] waveformArray;

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
            stoneTilemap.ClearAllTiles();
            dirtTilemap.ClearAllTiles();
            grassTilemap.ClearAllTiles();
        }
    }

    private void OnValidate()
    {
        if (scale != previousScale)
        {
            if (waveformArray != null)
            {
                ClearTerrain();
                SpawnTiles();
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

    private void SpawnTiles()
    {
        int mean = min + ((max - min) / 2);

        for (int x = 0; x < waveformArray.Length; x++)//This will help spawn a tile on the x axis
        {
            int height = mean + (int)((waveformArray[x] - mean) * scale);

            int minStoneSpawnDistance = height - minStoneheight;
            int maxStoneSpawnDistance = height - maxStoneHeight;
            int totalStoneSpawnDistance = Random.Range(minStoneSpawnDistance, maxStoneSpawnDistance);
            //Perlin noise.
            for (int y = 0; y < height; y++)//This will help spawn a tile on the y axis
            {
                if (y < totalStoneSpawnDistance)
                {
                    //spawnObj(stone, x, y);
                    stoneTilemap.SetTile(new Vector3Int(x, y, 0), stone);
                }
                else
                {
                    // spawnObj(dirt, x, y);
                    dirtTilemap.SetTile(new Vector3Int(x, y, 0), dirt);
                }

            }
            if (totalStoneSpawnDistance == height)
            {
                // spawnObj(stone, x, height);
                stoneTilemap.SetTile(new Vector3Int(x, height, 0), stone);
            }
            else
            {
                //spawnObj(grass, x, height);
                grassTilemap.SetTile(new Vector3Int(x, height, 0), grass);
            }
        }
    }

    override protected void Generation()
    {
        base.Generation();

        string waveformImage = PathCombine(Application.dataPath, "GeneratedPlots/waveform.png"); // Assign the waveform image in the Inspector
        waveformArray = ConvertWaveformImageToArray(LoadPNG(waveformImage));
        SpawnTiles();
    }

    private int[] ConvertWaveformImageToArray(Texture2D image)
    {
        Color32[] pixels = image.GetPixels32();
        int width = image.width;
        int height = image.height;

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
}
