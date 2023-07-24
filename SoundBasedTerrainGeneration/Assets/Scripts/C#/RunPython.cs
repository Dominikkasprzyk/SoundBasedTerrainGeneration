using UnityEditor.Scripting.Python;
using UnityEditor;
using UnityEngine;

public class RunPython
{
    public static void RunWaveformAndSpectogramGenerator(string filename)
    {
        PythonRunner.RunString($@"

            import UnityEngine;
            import wave
            import matplotlib.pyplot as plt
            import os
            import numpy as np
            import sys

            
            # Get the current working directory and specify the path for the audio file
            path = os.getcwd() + '/Assets/' + '{filename}.wav'
            # Open the audio file using the wave module
            wav_obj = wave.open(path)

            # Get the sample frequency (number of samples per second)
            sample_freq = wav_obj.getframerate()
            # Get the total number of audio samples in the file
            n_samples = wav_obj.getnframes()
            # Calculate the duration of the audio in seconds
            t_audio = n_samples / sample_freq

            # Get the number of audio channels (1 for mono, 2 for stereo)
            n_channels = wav_obj.getnchannels()

            # Read all the audio frames from the file
            signal_wave = wav_obj.readframes(n_samples)
            # Convert the binary audio data to a NumPy array of 16-bit integers
            signal_array = np.frombuffer(signal_wave, dtype=np.int16)

            # For stereo audio, extract the left and right channels separately
            l_channel = signal_array[0::n_channels]
            r_channel = signal_array[1::n_channels]

            # Generate an array of time values corresponding to each audio sample
            times = np.linspace(0, t_audio, num=n_samples)

            # Set the figure size for the waveform plot to be proportional to the audio duration
            plt.rcParams['figure.figsize'] = [t_audio*4000, 10]
            # Create a new figure for the waveform plot
            plt.figure(frameon=False)
            # Plot the left channel waveform against time
            plt.plot(times, l_channel)
            # Set the x-axis limits to show the entire audio duration
            plt.xlim(0, t_audio)
            # Turn off axis to remove unnecessary visual elements
            plt.axis('off')
            # Save the waveform plot to a file
            plt.savefig(os.getcwd() + '/Assets/GeneratedPlots/waveform.png', bbox_inches='tight', pad_inches=0)

            # Set the figure size for the spectrogram plot to be proportional to the audio duration
            plt.rcParams['figure.figsize'] = [t_audio * 3, 6]
            # Create a new figure for the spectrogram plot
            plt.figure(frameon=False)
            # Plot the spectrogram of the left channel
            plt.specgram(l_channel, Fs=sample_freq, vmin=-20, vmax=50)
            # Save the spectrogram plot to a file
            plt.savefig(os.getcwd() + '/Assets/GeneratedPlots/frequencySpectrum.png', bbox_inches='tight', pad_inches=0)
        
        ");
    }
}
