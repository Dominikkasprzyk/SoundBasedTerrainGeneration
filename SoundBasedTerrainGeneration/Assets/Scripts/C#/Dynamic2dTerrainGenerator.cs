using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Dynamic2dTerrainGenerator : TerrainGeneration
{
    private int numVertices;
    private bool simulationRunning = false;
    private AudioSource audioSource;
    [SerializeField] private AudioClip waveFile;
    private int currentFrame = 0;


    override protected string txtDataFilePath
    {
        get
        {
            return "spectrogram.txt";
        }
    }
    override protected void Start()
    {
        RunPython.RunWaveformAndSpectogramGenerator(wavFilename);
        vertexDataArray = ConvertTxtToArray(Path.Combine(Application.dataPath, txtDataFilePath));

        audioSource = GetComponent<AudioSource>();
        audioSource.clip = waveFile;
    }

    private IEnumerator Simulation(float interval)
    {
        simulationRunning = true;
        audioSource.Play();

        for (int i = 0; i < vertexDataArray.GetLength(1); i++)
        {
            currentFrame = i;
            GenerateTerrainMesh(ExtractDetails(vertexDataArray));
            yield return new WaitForSeconds(interval);
        }
        simulationRunning = false;
        //meshFilter.mesh = null;
    }

    public void StartSimulation()
    {
        if(!simulationRunning)
        {
            float interval = waveFile.length / vertexDataArray.GetLength(1);
            StartCoroutine(Simulation(interval));
        }
    }

    override protected int[,] ConvertTxtToArray(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        int numRows = lines.Length;
        string[] firstRowValues = lines[0].Split(' ');
        int numCols = firstRowValues.Length;

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

    override protected void GenerateTerrainMesh(int[,] dataArray)
    {
        if (dataArray == null || dataArray.Length == 0)
        {
            Debug.LogError("Data is empty!");
            return;
        }

        numVertices = dataArray.GetLength(0); 

        vertices = new Vector3[numVertices * 2];
        triangles = new int[(numVertices - 1) * 6];

        for (int i = 0; i < numVertices; i++)
        {
            float x = i * skipDetail;
            float y = dataArray[i, currentFrame] + 10;

            if (x > vertexDataArray.GetLength(0))
            {
                x = vertexDataArray.GetLength(0) - 1;
            }

            vertices[i * 2] = new Vector3(x, y, 0);
            vertices[i * 2 + 1] = new Vector3(x, 0, 0);

            if (i < numVertices - 1)
            {
                int triangleIndex = i * 6;
                triangles[triangleIndex] = i * 2;
                triangles[triangleIndex + 1] = (i + 1) * 2;
                triangles[triangleIndex + 2] = (i + 1) * 2 + 1;

                triangles[triangleIndex + 3] = i * 2;
                triangles[triangleIndex + 4] = (i + 1) * 2 + 1;
                triangles[triangleIndex + 5] = i * 2 + 1;
            }
        }
        if (!meshFilter)
        {
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = new Mesh();
            meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.mesh.MarkDynamic();
        }
        AdjustVertices();
    }

    override protected void UpdateMesh()
    {
    }

    override protected void AdjustVertices()
    {
        List<Vector3> adjsutedVertices = new List<Vector3>(vertices);
        for (int v = 0; v < adjsutedVertices.Count; v += 2)
        {
            int newY = (int)Mathf.Lerp(10, vertices[v].y, steepness);
            adjsutedVertices[v] = new Vector3(vertices[v].x, newY, vertices[v].z);
        }

        if (smoothingSteps > 0)
        {
            List<Vector3> smoothedVertices = SmoothTerrainMesh(adjsutedVertices, smoothingSteps);
            adjsutedVertices = smoothedVertices;
        }

        meshFilter.mesh.triangles = null;
        meshFilter.mesh.SetVertices(adjsutedVertices);
        meshFilter.mesh.SetTriangles(triangles, 0);
        meshFilter.mesh.RecalculateNormals();
    }

    List<Vector3> SmoothTerrainMesh(List<Vector3> verts, int smoothIterations)
    {
        List<Vector3> vertices = verts;

        for (int iteration = 0; iteration < smoothIterations; iteration++)
        {
            List<Vector3> smoothedVertices = vertices.ToArray().ToList<Vector3>();

            for (int i = 0; i < vertices.Count; i+=2)
            {
                int averageHeight = 0;
                int neighborCount = 0;

                for (int ni = Mathf.Max(0, i - 2); ni <= Mathf.Min(vertices.Count - 1, i + 2); ni+=2)
                {
                    averageHeight += (int)vertices[ni].y;
                    neighborCount++;
                }
                smoothedVertices[i] = new Vector3(vertices[i].x, averageHeight / neighborCount, 0);
            }
            vertices = smoothedVertices;
        }
        return vertices;
    }

    override protected int[,] ExtractDetails(int[,] original)
    {
        int oldRowCount = original.GetLength(0);
        int oldColCount = original.GetLength(1);

        int newRowCount = 2 + ((oldRowCount - 2) / skipDetail);
        int newColCount = oldColCount;

        int[,] result = new int[newRowCount, newColCount];

        int newRow = 0;
        for (int i = 0; i < oldRowCount; i += skipDetail)
        {
            int newCol = 0;
            for (int j = 0; j < oldColCount; j += 1)
            {
                result[newRow, newCol] = original[i, j];
                newCol++;
            }
            result[newRow, newColCount - 1] = original[i, oldColCount - 1];
            newRow++;
        }
        int newColLast = 0;
        for (int j = 0; j < oldColCount; j += 1)
        {
            result[newRowCount - 1, newColLast] = original[oldRowCount - 1, j];
            newColLast++;
        }
        result[newRowCount - 1, newColCount - 1] = original[oldRowCount - 1, oldColCount - 1];

        return result;
    }
}
