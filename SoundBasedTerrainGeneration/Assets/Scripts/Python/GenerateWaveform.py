import UnityEngine;
import wave
import matplotlib.pyplot as plt
import os
import numpy as np

path = os.getcwd() + '/Assets/' + 'wave.wav'
wav_obj = wave.open(path)
sample_freq = wav_obj.getframerate()
n_samples = wav_obj.getnframes()
t_audio = n_samples/sample_freq
n_channels = wav_obj.getnchannels()
signal_wave = wav_obj.readframes(n_samples)
signal_array = np.frombuffer(signal_wave, dtype=np.int16)
l_channel = signal_array[0::2]
r_channel = signal_array[1::2]
times = np.linspace(0, n_samples/sample_freq, num=n_samples)

plt.figure(frameon=False)
plt.rcParams['figure.figsize'] = [100, 100]
plt.plot(times, l_channel)
plt.xlim(0, t_audio)
plt.axis('off')
plt.savefig(os.getcwd() + '/Assets/GeneratedPlots/waveform.png', bbox_inches='tight', pad_inches=0)

plt.figure(frameon=False)
plt.rcParams['figure.figsize'] = [20, 100]
plt.specgram(l_channel, Fs=sample_freq, vmin=-20, vmax=50)
plt.xlim(0, t_audio)
plt.axis('off')
plt.savefig(os.getcwd() + '/Assets/GeneratedPlots/frequencySpectrum.png', bbox_inches='tight', pad_inches=0)