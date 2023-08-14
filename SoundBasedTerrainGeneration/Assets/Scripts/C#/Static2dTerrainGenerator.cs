using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Static2dTerrainGenerator : TerrainGeneration
{
    override protected string txtDataFilePath
    {
        get
        {
            return "waveform.txt";
        }
    }

    [Range(0, 1)]
    [SerializeField] private float smoothness;

    [Min(1)]
    [SerializeField] private int detailLevel;

    private int min, max;
    private float previousSmoothness, previousDetailLevel;
    private int[,] vertexDataArray;
    private int width, height, middlePoint;
    private MeshFilter meshFilter;
    private Vector3[] vertices;
    private int[] triangles;

    void Start()
    {
        Generation();
    }

    private void OnValidate()
    {
        if (smoothness != previousSmoothness || detailLevel != previousDetailLevel)
        {
            UpdateMesh();
            previousSmoothness = smoothness;
            previousDetailLevel = detailLevel;
        }
    }
    protected void Generation()
    {

        string waveformTxt = PathCombine(Application.dataPath, "waveform.txt"); // Assign the waveform image in the Inspector
        vertexDataArray = ConvertTxtToArray(waveformTxt);

        //int[,] test = new int[5, 1];
        //test[0, 0] = 5;
        //test[1, 0] = 10;
        //test[2, 0] = 15;
        //test[3, 0] = 5;
        //test[4, 0] = 5;
        //middlePoint = 0;

        GenerateGraphMesh(vertexDataArray);
    }

    override protected int[,] ConvertTxtToArray(string filePath)
    {
        middlePoint = 0;

        string[] lines = File.ReadAllLines(filePath);
        int numSamples = lines.Length;
        int numChannels = lines[0].Split(' ').Length;

        int[,] audioData = new int[numSamples, numChannels];

        for (int i = 0; i < numSamples; i++)
        {
            string[] samples = lines[i].Split(' ');
            for (int j = 0; j < numChannels; j++)
            {
                if (int.TryParse(samples[j], out int value))
                {
                    audioData[i, j] = value;
                    if (value < middlePoint)
                    {
                        middlePoint = value;
                    }
                }
                else
                {
                    // Handle parsing errors here if needed
                    Debug.Log($"Error parsing value at ({i}, {j})");
                }
            }
        }

        return audioData;
    }

    override protected void GenerateTerrainMesh(int[,] dataArray)
    {

    }

    //private int[] ConvertWaveformImageToArray(Texture2D image)
    //{
    //    Color32[] pixels = image.GetPixels32();
    //    width = image.width;
    //    height = image.height;

    //    List<int> waveformList = new List<int>();

    //    for (int x = 0; x < width; x++)
    //    {
    //        bool hasWaveform = false;

    //        for (int y = 0; y < height; y++)
    //        {
    //            Color32 pixelColor = pixels[x + y * width];

    //            // Check if the pixel is not fully transparent
    //            if (pixelColor.a > 0)
    //            {
    //                waveformList.Add(y);
    //                hasWaveform = true;
    //                break;
    //            }
    //        }

    //        if (!hasWaveform)
    //        {
    //            waveformList.Add(0); // Use 0 as the default value when no waveform is present
    //        }

    //        if(waveformList.Count == 1)
    //        {
    //            min = max = waveformList.Last();
    //        } else if(waveformList.Last() < min)
    //        {
    //            min = waveformList.Last();
    //        } else if(waveformList.Last() > max)
    //        {
    //            max = waveformList.Last();
    //        }
    //    }

    //    //middlePoint = min + ((max - min) / 2);

    //    return waveformList.ToArray();
    //}

    private string PathCombine(string path1, string path2)
    {
        return System.IO.Path.Combine(path1, path2);
    }

    //private Texture2D LoadPNG(string filePath)
    //{

    //    Texture2D tex = null;
    //    byte[] fileData;

    //    if (File.Exists(filePath))
    //    {
    //        fileData = File.ReadAllBytes(filePath);
    //        tex = new Texture2D(2, 2);
    //        tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
    //    }
    //    return tex;
    //}

    void GenerateGraphMesh(int [,] graphData)
    {     
        if (graphData == null || graphData.Length == 0)
        {
            Debug.LogError("Graph data is empty!");
            return;
        }

        int numVertices = graphData.GetLength(0) * 2; // Two vertices per data point (y-axis and x-axis)
        vertices = new Vector3[numVertices];
        triangles = new int[(graphData.GetLength(0) - 1) * 6]; // Two triangles per data point

        for (int i = 0; i < graphData.GetLength(0); i++)
        {
            float x = i;
            float y = graphData[i,0];

            // Vertex on the y-axis (value of the function)
            vertices[i * 2] = new Vector3(x, y + (-1*middlePoint), 0);
            // Vertex on the x-axis
            vertices[i * 2 + 1] = new Vector3(x, 0, 0);

            // Create triangles for the current data point (except for the last one)
            if (i < graphData.GetLength(0) - 1)
            {
                int triangleIndex = i * 6;
                triangles[triangleIndex] = i * 2;
                triangles[triangleIndex + 2] = (i + 1) * 2 + 1; 
                triangles[triangleIndex + 1] = (i + 1) * 2;

                triangles[triangleIndex + 3] = i * 2;
                triangles[triangleIndex + 5] = i * 2 + 1; 
                triangles[triangleIndex + 4] = (i + 1) * 2 + 1;
            }
        }

        // Get the existing MeshFilter component
        meshFilter = GetComponent<MeshFilter>();

        // Create the mesh
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh.MarkDynamic();
        meshFilter.mesh.SetVertices(vertices);
        meshFilter.mesh.SetTriangles(triangles, 0);
    }

    override protected void UpdateMesh()
    {
        if (!meshFilter)
            return;
        if (vertexDataArray == null)
            return;

        List<Vector3> newVertices = new List<Vector3>(vertices);
        for (int v = 0; v < newVertices.Count; v+=2)
        {   
            int newY = (int)Mathf.Lerp(vertices[v].y, (-1 * middlePoint), smoothness);
            newVertices[v] = new Vector3(vertices[v].x, newY, vertices[v].z);
        }

        if(detailLevel > 1)
        {
            List<Vector3> interpolatedVertices = InterpolateList(detailLevel, newVertices);
            newVertices = interpolatedVertices;
        }

        meshFilter.mesh.SetVertices(newVertices);
    }

    private List<Vector3> InterpolateList(int detailLevel, List<Vector3> original)
    {
        //Debug.Log(original.Count);
        // Initialize a new list to store the interpolated values
        List<Vector3> interpolatedList = new List<Vector3>();

        // Get the number of elements in the original list
        int originalCount = original.Count;

        // Loop through the original list, skipping elements based on the detailLevel
        for (int i = 0; i < originalCount - 1; i += detailLevel * 2)
        {
            // Add the first element of the segment to the interpolated list
            interpolatedList.Add(original[i]);

            // Determine the number of steps to interpolate between current and next element
            int steps = Math.Min(detailLevel * 2, originalCount - i - 1);

            //Debug.Log("Prev: " + original[i].y + "; Next: " + original[i + steps].y);
            // Interpolate between the current and next element and add to the interpolated list
            for (int j = 1; j < steps; j++)
            {
                if (j % 2 == 0)
                {
                    float percent = (float)j / steps;
                    float interpolatedValueFloat = original[i].y +
                        ((original[i + steps].y - original[i].y) *
                        percent);
                    int interpolatedValue = (int)interpolatedValueFloat;
                    //Debug.Log("Diff: " + (original[i + steps].y - original[i].y) +
                    //    "; Original: " + original[i].y + "; Next: " + original[i + steps].y +
                    //    "; Interpolated" + interpolatedValue + "; Percent: " + percent);
                    Vector3 newValue = new Vector3((i + j) / 2, interpolatedValue, 0);
                    interpolatedList.Add(newValue);
                } else
                {
                    Vector3 newValue = new Vector3((i + j) / 2, 0, 0);
                    interpolatedList.Add(newValue);
                }
            }
            //Debug.Log("___");
        }
        // Add the last element from the original list to the interpolated list
        interpolatedList.Add(original[originalCount - 1]);

        //Debug.Log(interpolatedList.Count);

        // Return the final interpolated list
        return interpolatedList;
    }

    override protected int[,] ExtractDetails(int[,] original)
    {
        int oldRowCount = original.GetLength(0);
        int oldColCount = original.GetLength(1);

        int newRowCount = 2 + ((oldRowCount - 2) / skipDetail);
        int newColCount = 2 + ((oldColCount - 2) / skipDetail);

        int[,] result = new int[newRowCount, newColCount];

        int newRow = 0;
        for (int i = 0; i < oldRowCount; i += skipDetail)
        {
            int newCol = 0;
            for (int j = 0; j < oldColCount; j += skipDetail)
            {
                result[newRow, newCol] = original[i, j];
                newCol++;
            }
            result[newRow, newColCount - 1] = original[i, oldColCount - 1];
            newRow++;
        }
        int newColLast = 0;
        for (int j = 0; j < oldColCount; j += skipDetail)
        {
            result[newRowCount - 1, newColLast] = original[oldRowCount - 1, j];
            newColLast++;
        }
        result[newRowCount - 1, newColCount - 1] = original[oldRowCount - 1, oldColCount - 1];

        return result;
    }

    override protected void AdjustVertices()
    {

    }
}
