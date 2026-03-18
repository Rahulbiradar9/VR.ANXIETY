using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket; // Requires NativeWebSocket package from Unity Asset Store or GitHub (https://github.com/endel/NativeWebSocket)

public class VoiceInteractionClient : MonoBehaviour
{
    [Header("Network Settings")]
    public string websocketUrl = "ws://localhost:8000/ws/audio-stream";
    private WebSocket websocket;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public int recordFreq = 16000;
    public int maxRecordDurationSeconds = 10;
    
    private AudioClip recordingClip;
    private string sessionId;
    
    [Header("State")]
    public bool isRecording = false;

    // Buffer for TTS playback
    private Queue<byte[]> audioQueue = new Queue<byte[]>();
    private bool isPlayingTTS = false;
    private MemoryStream incomingAudioStream;

    void Start()
    {
        sessionId = Guid.NewGuid().ToString();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        // For continuous real-time, you could make the user hold to talk or use VAD on client side.
        // Here we demonstrate a push-to-talk style for simplicity in the demo.
    }

    void Update()
    {
        if (websocket != null)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
#endif
        }

        // Check for Input System Spacebar
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StartRecordingAndConnect();
            }
            
            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasReleasedThisFrame && isRecording)
            {
                StopRecordingAndSend();
            }
        }

        // Check TTS Playback queue
        if (!isPlayingTTS && audioQueue.Count > 0)
        {
            StartCoroutine(PlayNextAudioChunk());
        }
    }

    private async void StartRecordingAndConnect()
    {
        isRecording = true;
        Debug.Log("Recording started...");

        // Start Microphone
        recordingClip = Microphone.Start(null, false, maxRecordDurationSeconds, recordFreq);

        // Connect WebSocket if not connected
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            websocket = new WebSocket(websocketUrl);

            websocket.OnOpen += () =>
            {
                Debug.Log("WebSocket Connection open!");
                // Send metadata
                string metadata = $"{{\"session_id\": \"{sessionId}\"}}";
                websocket.SendText(metadata);
            };

            websocket.OnError += (e) =>
            {
                Debug.Log("WebSocket Error: " + e);
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("WebSocket Connection closed!");
            };

            websocket.OnMessage += (bytes) =>
            {
                // Simple protocol: We distinguish JSON messages and Binary audio.
                // In a robust app, use a proper framing format (header + payload size).
                string message = Encoding.UTF8.GetString(bytes);
                
                if (message.StartsWith("{"))
                {
                    // It's JSON metadata
                    Debug.Log("Received JSON: " + message);
                    if (message.Contains("audio_start"))
                    {
                        incomingAudioStream = new MemoryStream();
                    }
                    else if (message.Contains("audio_end"))
                    {
                        audioQueue.Enqueue(incomingAudioStream.ToArray());
                        incomingAudioStream.Dispose();
                        incomingAudioStream = null;
                    }
                }
                else
                {
                    // It's binary audio data
                    if (incomingAudioStream != null)
                    {
                        incomingAudioStream.Write(bytes, 0, bytes.Length);
                    }
                }
            };

            try
            {
                await websocket.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebSocket Connect Exception: {ex.Message}");
            }
        }
    }

    private async void StopRecordingAndSend()
    {
        isRecording = false;
        
        // Capture the exact position of the recording *before* we end the microphone
        int lastPos = Microphone.GetPosition(null);
        if (lastPos == 0) lastPos = recordingClip.samples;
        
        Microphone.End(null);
        Debug.Log("Recording stopped. Sending to server...");

        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            // Convert AudioClip to byte array (WAV format), trimming silence
            byte[] wavData = ConvertClipToWav(recordingClip, lastPos);
            await websocket.Send(wavData);
            Debug.Log($"Sent {wavData.Length} bytes of audio data.");
        }
        else
        {
            Debug.LogWarning("Cannot send. WebSocket is not open.");
        }
    }

    private IEnumerator PlayNextAudioChunk()
    {
        isPlayingTTS = true;
        byte[] mp3Data = audioQueue.Dequeue();
        
        // For demonstration, server sends raw PCM (16-bit, 16kHz, mono) without RIFF headers
        
        float[] floatArray = new float[mp3Data.Length / 2];
        for (int i = 0; i < floatArray.Length; i++)
        {
            short bit16 = BitConverter.ToInt16(mp3Data, i * 2);
            floatArray[i] = bit16 / 32768.0f;
        }

        AudioClip clip = AudioClip.Create("TTS", floatArray.Length, 1, 16000, false);
        clip.SetData(floatArray, 0);

        audioSource.clip = clip;
        audioSource.Play();

        yield return new WaitForSeconds(clip.length);
        
        isPlayingTTS = false;
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

    // Helper: Convert Unity AudioClip to standard WAV byte array
    private byte[] ConvertClipToWav(AudioClip clip, int endPosition)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        // Use the recorded endPosition to trim science
        float[] samples = new float[endPosition * clip.channels];
        clip.GetData(samples, 0);

        int hz = clip.frequency;
        int channels = clip.channels;
        int samplesCount = samples.Length;

        // WAV Header
        writer.Write(Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(36 + samplesCount * 2);
        writer.Write(Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(hz);
        writer.Write(hz * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(Encoding.UTF8.GetBytes("data"));
        writer.Write(samplesCount * 2);

        // Convert floats to 16-bit PCM back into the stream
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(samples[i] * 32767.0f);
            writer.Write(sample);
        }

        byte[] wavData = stream.ToArray();
        writer.Close();
        stream.Close();

        return wavData;
    }
}
