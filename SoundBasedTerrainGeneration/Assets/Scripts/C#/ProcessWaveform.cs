using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ProcessWaveform : MonoBehaviour
{
    void Start()
    {
        string waveformImage = PathCombine(Application.dataPath, "GeneratedPlots/waveform.png"); // Assign the waveform image in the Inspector
        int[] waveformArray = ConvertWaveformImageToArray(LoadPNG(waveformImage));
        SaveArrayToFile(waveformArray, "waveform.txt");
    }

    private string PathCombine(string path1, string path2)
    {
        return System.IO.Path.Combine(path1, path2);
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
        }

        return waveformList.ToArray();
    }

    private void SaveArrayToFile(int[] array, string fileName)
    {
        string filePath = Path.Combine(Application.dataPath, fileName);
        string arrayData = string.Join(",", array);
        File.WriteAllText(filePath, arrayData);
        Debug.Log("Array saved to: " + filePath);
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
