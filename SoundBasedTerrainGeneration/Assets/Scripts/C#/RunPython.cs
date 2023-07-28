using UnityEditor.Scripting.Python;
using UnityEditor;
using UnityEngine;

public class RunPython
{
    public static void RunWaveformAndSpectogramGenerator(string filename)
    {
        PythonRunner.RunString(@"

            import UnityEngine;
            import wave
            import matplotlib.pyplot as plt
            import os
            import numpy as np
            import sys

            def convert_wave_to_int_array(wave_file_path):
                with wave.open(wave_file_path, 'rb') as audio_file:
                    num_frames = audio_file.getnframes()
                    num_channels = audio_file.getnchannels()
                    sample_width = audio_file.getsampwidth()
                    frame_rate = audio_file.getframerate()
        
                    # Read all audio frames as bytes
                    audio_data_bytes = audio_file.readframes(num_frames)

                    dtype_map = {1: np.int8, 2: np.int16, 4: np.int32}
                    dtype = dtype_map[sample_width]

                    audio_array = np.frombuffer(audio_data_bytes, dtype=dtype)

                    # Reshape the array to separate channels
                    audio_array = audio_array.reshape(-1, num_channels)

                    return audio_array / np.iinfo(audio_array.dtype).max

            def save_int_array_to_txt(int_array, txt_file_path):
                with open(txt_file_path, 'w') as txt_file:
                    txt_file.write(' '.join(str(val) for val in int_array))
            " + $@"

            wave_file_path = os.getcwd() + '/Assets/' + '{filename}.wav'
            waveform_int_array = convert_wave_to_int_array(wave_file_path)

            txt_file_path = os.getcwd() + '/Assets/' + 'waveform.txt'
            # save_int_array_to_txt(waveform_int_array, txt_file_path)
            audio_array_int = (waveform_int_array * np.iinfo(np.int16).max).astype(np.int16)
            np.savetxt(txt_file_path, audio_array_int, fmt='%d', delimiter=' ')
        
        ");
    }
}
