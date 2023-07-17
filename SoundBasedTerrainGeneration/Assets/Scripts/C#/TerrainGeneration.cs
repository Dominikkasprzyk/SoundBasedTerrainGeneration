using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    [SerializeField] protected string filename;

    protected virtual void Generation()
    {
        RunPython.RunWaveformAndSpectogramGenerator(filename);
    }
}
