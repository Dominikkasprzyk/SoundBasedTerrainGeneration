using UnityEditor.Scripting.Python;
using UnityEditor;
using UnityEngine;

public class RunPython
{
    [MenuItem("ProcessAudio/GenerateWavformAndSpectrogram")]
    static void RunEnsureNaming()
    {
        PythonRunner.RunFile($"{Application.dataPath}/Scripts/Python/GenerateWaveform.py");
    }
}
