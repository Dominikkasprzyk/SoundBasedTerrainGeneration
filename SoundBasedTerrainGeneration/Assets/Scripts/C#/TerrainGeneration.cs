using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
abstract public class TerrainGeneration : MonoBehaviour
{
    [SerializeField] private string wavFilename;
    
    [Range(0, 1)]
    [SerializeField] protected float steepness;

    [Range(0, 10)]
    [SerializeField] protected int smoothingSteps;

    [Min(1)]
    [SerializeField] protected int skipDetail;

    protected int[,] vertexDataArray;
    protected MeshFilter meshFilter;
    protected Vector3[] vertices;
    protected int[] triangles;

    protected float previousSteepness;
    protected int previousSmoothingSteps;
    protected int previousSkipDetail;
    abstract protected string txtDataFilePath
    {
        get;
    }

    virtual protected void Start()
    {
        RunPython.RunWaveformAndSpectogramGenerator(wavFilename);
        vertexDataArray = ConvertTxtToArray(Path.Combine(Application.dataPath, txtDataFilePath));
        GenerateTerrainMesh(ExtractDetails(vertexDataArray));
    }

    private void OnValidate()
    {
        if (steepness != previousSteepness ||
            smoothingSteps != previousSmoothingSteps ||
            skipDetail != previousSkipDetail)
        {
            UpdateMesh();
            previousSteepness = steepness;
            previousSmoothingSteps = smoothingSteps;
            previousSkipDetail = skipDetail;
        }
    }

    abstract protected int[,] ConvertTxtToArray(string txtFilePath);

    abstract protected void GenerateTerrainMesh(int[,] dataArray);

    abstract protected void UpdateMesh();

    abstract protected void AdjustVertices();

    abstract protected int[,] ExtractDetails(int[,] original);
}
