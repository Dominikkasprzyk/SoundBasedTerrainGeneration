using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Static3dTerrainGenerator : TerrainGeneration
{
    [Range(0, 1)]
    [SerializeField] private float smoothness;

    [Range(0, 10)]
    [SerializeField] private int smoothingSteps;

    private int min, max;
    private float previousSmoothness, previoussmoothingSteps;
    private int[,] spectrogramArray;
    private int numRows, numCols;
    private MeshFilter meshFilter;
    private Vector3[] vertices;
    private int[] triangles;

    void Start()
    {
        Generation();
    }

    private void OnValidate()
    {
        if (smoothness != previousSmoothness || smoothingSteps != previoussmoothingSteps)
        {
            UpdateMesh();
            previousSmoothness = smoothness;
            previoussmoothingSteps = smoothingSteps;
        }
    }

    override protected void Generation()
    {
        base.Generation();

        string spectrogramTxt = PathCombine(Application.dataPath, "spectrogram.txt"); // Assign the spectrogram image in the Inspector
        spectrogramArray = ConvertSpectrogramTxtToArray(spectrogramTxt);

        //int[,] test = new int[3, 3];
        //numRows = 3;
        //numCols = 3;

        GenerateTerrainMesh(spectrogramArray);

    }

    private int[,] ConvertSpectrogramTxtToArray(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        numRows = lines.Length;
        string[] firstRowValues = lines[0].Split(' ');
        numCols = firstRowValues.Length;

        // Create a 2D int array to store the data
        int[,] dataArray = new int[numRows, numCols];

        for (int i = 0; i < numRows; i++)
        {
            string[] values = lines[i].Split(' ');
            for (int j = 0; j < numCols; j++)
            {
                int value = int.Parse(values[j]);
                dataArray[i, j] = value;
            }
        }

        return dataArray;
    }

    private string PathCombine(string path1, string path2)
    {
        return Path.Combine(path1, path2);
    }

    void GenerateTerrainMesh(int[,] graphData)
    {
        if (graphData == null || graphData.Length == 0)
        {
            Debug.LogError("Graph data is empty!");
            return;
        }

        // Generate the vertices and triangles for the mesh
        vertices = new Vector3[numRows * numCols];
        triangles = new int[(numRows - 1) * (numCols - 1) * 6];

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numCols; j++)
            {
                float x = j;
                float y = graphData[i, j];
                float z = i;

                vertices[vertexIndex] = new Vector3(x, y, z);

                if (i < numRows - 1 && j < numCols - 1)
                {
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + numCols;
                    triangles[triangleIndex + 2] = vertexIndex + numCols + 1;

                    triangles[triangleIndex + 3] = vertexIndex;
                    triangles[triangleIndex + 4] = vertexIndex + numCols + 1;
                    triangles[triangleIndex + 5] = vertexIndex + 1;

                    triangleIndex += 6;
                }

                vertexIndex++;
            }
        }

        // Create the terrain mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;


        // Get the existing MeshFilter component
        meshFilter = GetComponent<MeshFilter>();

        // Create the mesh
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh.MarkDynamic();
        meshFilter.mesh.SetVertices(vertices);
        meshFilter.mesh.SetTriangles(triangles, 0);
        // Calculate normals (important for lighting)
        meshFilter.mesh.RecalculateNormals();
    }

    private void UpdateMesh()
    {
        if (!meshFilter)
            return;
        if (spectrogramArray == null)
            return;

        List<Vector3> newVertices = new List<Vector3>(vertices);
        for (int v = 0; v < newVertices.Count; v ++)
        {
            int newY = (int)Mathf.Lerp(vertices[v].y, 0, smoothness);
            newVertices[v] = new Vector3(vertices[v].x, newY, vertices[v].z);
        }

        if (smoothingSteps > 0)
        {
            List<Vector3> interpolatedVertices = SmoothTerrainMesh(newVertices, smoothingSteps);
            newVertices = interpolatedVertices;
        }

        meshFilter.mesh.SetVertices(newVertices);
        meshFilter.mesh.RecalculateNormals();
    }

    private List<Vector3> InterpolateList(int smoothingSteps, List<Vector3> original)
    {
        //Debug.Log(original.Count);
        // Initialize a new list to store the interpolated values
        List<Vector3> interpolatedList = new List<Vector3>();

        // Get the number of elements in the original list
        int originalCount = original.Count;

        // Loop through the original list, skipping elements based on the smoothingSteps
        for (int i = 0; i < originalCount - 1; i += smoothingSteps * 2)
        {
            // Add the first element of the segment to the interpolated list
            interpolatedList.Add(original[i]);

            // Determine the number of steps to interpolate between current and next element
            int steps = Math.Min(smoothingSteps * 2, originalCount - i - 1);

            // Calculate the difference between the current and next element
            int diff = (int)((original[i + steps].y - original[i].y)/steps);
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

    List <Vector3> SmoothTerrainMesh(List<Vector3> verts, int smoothIterations)
    {
        List<Vector3> vertices = verts;

        for (int iteration = 0; iteration < smoothIterations; iteration++)
        {
            List<Vector3> smoothedVertices = vertices.ToArray().ToList<Vector3>();

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    int vertexIndex = i * numCols + j;
                    Vector3 averageHeight = Vector3.zero;
                    int neighborCount = 0;

                    for (int ni = Mathf.Max(0, i - 1); ni <= Mathf.Min(numRows - 1, i + 1); ni++)
                    {
                        for (int nj = Mathf.Max(0, j - 1); nj <= Mathf.Min(numCols - 1, j + 1); nj++)
                        {
                            int neighborIndex = ni * numCols + nj;
                            averageHeight += vertices[neighborIndex];
                            neighborCount++;
                        }
                    }

                    smoothedVertices[vertexIndex] = averageHeight / neighborCount;
                }
            }

            vertices = smoothedVertices;
        }
        return vertices;
    }
}
