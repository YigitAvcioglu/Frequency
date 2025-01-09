using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public class RealTimeFrequency : MonoBehaviour
{
    public Text frequencyText; // UI'deki frekans� g�sterecek Text objesi
    private AudioClip microphoneClip;
    private const int sampleRate = 44100; // Sample h�z� 
    private const int fftSize = 1024; // FFT i�in �rnek boyutu (2'nin kuvveti olmal�)
    private float[] audioData; // Ses verileri i�in buffer

    void Start()
    {
        // Mikrofonu ba�lat
        string microphoneName = Microphone.devices[0]; // �lk mikrofonu se�
        microphoneClip = Microphone.Start(microphoneName, true, 1, sampleRate);

        // Ses verisi i�in buffer olu�tur
        audioData = new float[fftSize];
    }

    void Update()
    {
        // Mikrofon verisini al
        int micPosition = Microphone.GetPosition(null);
        if (micPosition >= fftSize)
        {
            Debug.Log(1);
            microphoneClip.GetData(audioData, micPosition - fftSize);

            // FFT analizi yap
            float frequency = PerformFFTAndGetFrequency(audioData);

            // Frekans� ekrana yazd�r
            frequencyText.text = $"Frequency: {frequency:F2} Hz";
        }
    }

    private float PerformFFTAndGetFrequency(float[] audioBuffer)
    {
        // Hamming penceresi uygula
        for (int i = 0; i < fftSize; i++)
        {
            audioBuffer[i] *= 0.54f - 0.46f * Mathf.Cos(2 * Mathf.PI * i / (fftSize - 1));
        }

        // FFT i�in kompleks veri yap�s� olu�tur
        Complex[] fftComplex = new Complex[fftSize];
        for (int i = 0; i < fftSize; i++)
        {
            fftComplex[i] = new Complex(audioBuffer[i], 0);
        }

        // FFT i�lemini uygula
        FFT(fftComplex);

        // Magnit�d spektrumu hesapla
        float[] magnitude = new float[fftSize / 2];
        for (int i = 0; i < fftSize / 2; i++)
        {
            magnitude[i] = (float)Math.Sqrt(fftComplex[i].Real * fftComplex[i].Real +
                                            fftComplex[i].Imaginary * fftComplex[i].Imaginary);
        }

        // En g��l� frekans� bul
        float maxMagnitude = -1f;
        int maxIndex = -1;
        for (int i = 0; i < magnitude.Length; i++)
        {
            if (magnitude[i] > maxMagnitude)
            {
                maxMagnitude = magnitude[i];
                maxIndex = i;
            }
        }

        // Frekans� hesapla
        return maxIndex * (sampleRate / (float)fftSize);
    }

    private void FFT(Complex[] buffer)
    {
        int n = buffer.Length;
        int bits = (int)(Math.Log(n) / Math.Log(2)); // Log2 yerine ln kullan

        // Bit ters �evirme
        for (int i = 0; i < n; i++)
        {
            int j = ReverseBits(i, bits);
            if (j > i)
            {
                var temp = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = temp;
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = -2 * Math.PI / len;
            var wLen = new Complex(Math.Cos(angle), Math.Sin(angle));
            for (int i = 0; i < n; i += len)
            {
                var w = Complex.One;
                for (int j = 0; j < len / 2; j++)
                {
                    var u = buffer[i + j];
                    var v = buffer[i + j + len / 2] * w;
                    buffer[i + j] = u + v;
                    buffer[i + j + len / 2] = u - v;
                    w *= wLen;
                }
            }
        }
    }

    private int ReverseBits(int value, int bits)
    {
        int result = 0;
        for (int i = 0; i < bits; i++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }
        return result;
    }

    void OnDisable()
    {
        // Mikrofonu kapat
        Microphone.End(null);
    }
}
