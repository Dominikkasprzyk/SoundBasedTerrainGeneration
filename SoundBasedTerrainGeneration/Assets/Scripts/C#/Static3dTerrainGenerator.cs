using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Static3dTerrainGenerator : TerrainGeneration
{
    private int numRows, numCols;

    override protected string txtDataFilePath
    {
        get
        {
            return "spectrogram.txt";
        }
    }

    void Start()
    {
        Generation();
    }

    override protected void Generation()
    {
        base.Generation();
        vertexDataArray = ConvertTxtToArray(Path.Combine(Application.dataPath, txtDataFilePath));
        GenerateTerrainMesh(ExtractDetails(vertexDataArray));
    }

    override protected int[,] ConvertTxtToArray(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        numRows = lines.Length;
        string[] firstRowValues = lines[0].Split(' ');
        numCols = firstRowValues.Length;

        Debug.Log(numRows + "; " + numCols);

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
            Debug.LogError("Graph data is empty!");
            return;
        }

        numRows = dataArray.GetLength(0);
        numCols = dataArray.GetLength(1);

        vertices = new Vector3[numRows * numCols];
        triangles = new int[(numRows - 1) * (numCols - 1) * 6];

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < numRows; i ++)
        {
            for (int j = 0; j < numCols; j ++)
            {
                float x = j * skipDetail;
                float y = dataArray[i, j];
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
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh.MarkDynamic();
        meshFilter.mesh.SetVertices(vertices);
        meshFilter.mesh.SetTriangles(triangles, 0);
        meshFilter.mesh.RecalculateNormals();
    }

    override protected void UpdateMesh()
    {
        if (!meshFilter)
            return;
        if (vertexDataArray == null)
            return;

        List<Vector3> newVertices = new List<Vector3>(vertices);
        for (int v = 0; v < newVertices.Count; v++)
        {
            int newY = (int)Mathf.Lerp(0, vertices[v].y, steepness);
            newVertices[v] = new Vector3(vertices[v].x, newY, vertices[v].z);
        }

        if (smoothingSteps > 0)
        {
            List<Vector3> smoothedVertices = SmoothTerrainMesh(newVertices, smoothingSteps);
            newVertices = smoothedVertices;
        }

        if (skipDetail > 0)
        {
            //List<Vector3> undetailedVertices = SkipDetail(newVertices, smoothingSteps);
            //newVertices = undetailedVertices;
        }

        meshFilter.mesh.SetVertices(newVertices);
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

    private int[,] ExtractDetails(int[,] original)
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
                result[newRow,newCol] = original[i,j];
                newCol++;
            }
            result[newRow,newColCount - 1] = original[i,oldColCount - 1];
            newRow++;
        }
        int newColLast = 0;
        for (int j = 0; j < oldColCount; j += skipDetail)
        {
            result[newRowCount - 1,newColLast] = original[oldRowCount - 1,j];
            newColLast++;
        }
        result[newRowCount - 1,newColCount - 1] = original[oldRowCount - 1,oldColCount - 1];

        return result;
    }
}
