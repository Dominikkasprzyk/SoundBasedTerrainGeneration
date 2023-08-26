using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Dynamic3dTerrainGenerator : TerrainGeneration
{
    private int numRows, numCols;
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
        //int [,] test = RotateDataArray(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0});

        // Print the resulting array
        //for (int i = 0; i < test.GetLength(0); i++)
        //{
        //    string output = "";
        //    for (int j = 0; j < test.GetLength(1); j++)
        //    {
        //        output += test[i, j] + " ";
        //    }
        //    Debug.Log(output);
        //}
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

    private int [,] RotateDataArray(int [] inputArray)
    {
        int size = inputArray.Length * 2 - 1;
        int[,] newArray = new int[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int minIndex = Math.Min(i, Math.Min(j, Math.Min(size - 1 - i, size - 1 - j)));
                newArray[i, j] = inputArray[minIndex];
            }
        }

        return newArray;
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
        numRows = lines.Length;
        string[] firstRowValues = lines[0].Split(' ');
        numCols = firstRowValues.Length;

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

        int[,] currentFrameDataArray = RotateDataArray(GetCol(dataArray, currentFrame));

        numRows = currentFrameDataArray.GetLength(0);
        numCols = currentFrameDataArray.GetLength(1);

        vertices = new Vector3[numRows * numCols];
        triangles = new int[(numRows - 1) * (numCols - 1) * 6];

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numCols; j++)
            {
                float x = j * skipDetail;
                float y = currentFrameDataArray[i, j];
                float z = i * skipDetail;

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
        if (!meshFilter)
        {
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = new Mesh();
            meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.mesh.MarkDynamic();
        }
        AdjustVertices();
    }

    private int[] GetCol(int[,] matrix, int col)
    {
        var colLength = matrix.GetLength(0);
        var colVector = new int[colLength];

        for (var i = 0; i < colLength; i++)
            colVector[i] = matrix[i, col];

        return colVector;
    }

    override protected void UpdateMesh()
    {
    }

    override protected void AdjustVertices()
    {
        List<Vector3> adjsutedVertices = new List<Vector3>(vertices);
        for (int v = 0; v < adjsutedVertices.Count; v++)
        {
            int newY = (int)Mathf.Lerp(0, vertices[v].y, steepness);
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
