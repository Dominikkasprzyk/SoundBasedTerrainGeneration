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

        PythonRunner.RunString($@"

            import os
            import numpy as np
            import matplotlib.pyplot as plt
            import matplotlib.cm as cm
            from scipy.io import wavfile
            from scipy.signal import spectrogram
            import librosa.display

            # Load the .wav file
            wav_filename = os.getcwd() + '/Assets/' + '{filename}.wav'
            data, sample_rate = librosa.load(wav_filename, sr=None)

            # Calculate the spectrogram
            spectrogram = np.abs(librosa.stft(data))

            # Convert to dB scale
            log_spectrogram = librosa.amplitude_to_db(spectrogram, ref=np.max)

            # Calculate the dynamic range of the spectrogram
            spectrogram_range = np.max(log_spectrogram) - np.min(log_spectrogram)

            # Define the percentage of dynamic range to use for visualization
            percentile = 100  # Adjust this value to control the percentage of dynamic range

            # Calculate adjusted min and max values based on percentile
            min_color = np.percentile(log_spectrogram, 100 - percentile)
            max_color = np.percentile(log_spectrogram, percentile)

            # Map the spectrogram data to the color range
            normalized_spectrogram = (log_spectrogram - min_color) / (max_color - min_color)
            normalized_spectrogram = np.clip(normalized_spectrogram, 0, 1)  # Ensure values are in the range [0, 1]

            # Convert the normalized spectrogram to integer values
            integer_spectrogram = (normalized_spectrogram * 255).astype(int)
                
            # Reverse the order of rows
            reversed_spectrogram = np.flipud(integer_spectrogram)

            # Save the integer spectrogram data as a text file
            txt_filename = os.getcwd() + '/Assets/' + 'spectrogram.txt'
            np.savetxt(txt_filename, reversed_spectrogram, fmt='%d')

            # Display the grayscale spectrogram
            plt.figure(figsize=(10, 6))
            librosa.display.specshow(integer_spectrogram, cmap='gray', sr=sample_rate, x_axis='time', y_axis='log')
            plt.colorbar(format='%+2.0f dB')
            plt.title('Grayscale Spectrogram')
            plt.show()
        ");
    }
}
