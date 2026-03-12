using UnityEngine;
using System;
using System.IO;
using System.Text;

public static class WavUtility
{
    const int HEADER_SIZE = 44;

    public static byte[] FromAudioClip(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();

        int samples = clip.samples * clip.channels;
        float[] data = new float[samples];
        clip.GetData(data, 0);

        short[] intData = new short[samples];
        byte[] bytesData = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            intData[i] = (short)(data[i] * short.MaxValue);
            BitConverter.GetBytes(intData[i]).CopyTo(bytesData, i * 2);
        }

        WriteHeader(stream, clip);

        stream.Write(bytesData, 0, bytesData.Length);

        return stream.ToArray();
    }

    static void WriteHeader(Stream stream, AudioClip clip)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        int byteRate = hz * channels * 2;

        stream.Seek(0, SeekOrigin.Begin);

        byte[] riff = Encoding.UTF8.GetBytes("RIFF");
        stream.Write(riff, 0, 4);

        stream.Write(BitConverter.GetBytes(36 + samples * channels * 2), 0, 4);

        byte[] wave = Encoding.UTF8.GetBytes("WAVE");
        stream.Write(wave, 0, 4);

        byte[] fmt = Encoding.UTF8.GetBytes("fmt ");
        stream.Write(fmt, 0, 4);

        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);
        stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(hz), 0, 4);
        stream.Write(BitConverter.GetBytes(byteRate), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2);
        stream.Write(BitConverter.GetBytes((ushort)16), 0, 2);

        byte[] dataString = Encoding.UTF8.GetBytes("data");
        stream.Write(dataString, 0, 4);
        stream.Write(BitConverter.GetBytes(samples * channels * 2), 0, 4);
    }
}