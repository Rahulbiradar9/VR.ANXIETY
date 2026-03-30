using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket; // Requires NativeWebSocket package from Unity Asset Store or GitHub (https://github.com/endel/NativeWebSocket)
using TMPro; // Added for TextMeshPro UI support

[System.Serializable]
public class WSMessage
{
    public string type;   // e.g. "audio_start", "audio_end"
    public string text;   // AI response text when present
}

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

    [Header("UI Settings")]
    [Tooltip("Drag and drop your TextMeshPro UI element here")]
    public TextMeshProUGUI responseTextDisplay;

    [Header("VAD Settings")]
    public float silenceThreshold = 0.02f;
    public float silenceDurationToStop = 1.5f;
    private float currentSilenceTime = 0f;
    private int startRecordingPos = 0;
    
    // Buffer for TTS playback
    private Queue<byte[]> audioQueue = new Queue<byte[]>();
    private bool isPlayingTTS = false;
    private MemoryStream incomingAudioStream;

    void Start()
    {
        sessionId = Guid.NewGuid().ToString();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        // Start processing automatically
        StartCoroutine(ContinuousListeningLoop());
    }

    void Update()
    {
        if (websocket != null)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
#endif
        }

        // Check TTS Playback queue
        if (!isPlayingTTS && audioQueue.Count > 0)
        {
            StartCoroutine(PlayNextAudioChunk());
        }
    }

    private IEnumerator ContinuousListeningLoop()
    {
        // Start connecting WebSocket immediately
        ConnectWebSocket();

        // Start Microphone continuously looping
        recordingClip = Microphone.Start(null, true, maxRecordDurationSeconds, recordFreq);
        while (!(Microphone.GetPosition(null) > 0)) { yield return null; } // Wait for mic

        float[] sampleData = new float[256];
        
        while (true)
        {
            yield return null;

            if (isPlayingTTS)
            {
                // To avoid bot hearing itself, we could reset states.
                if (isRecording)
                {
                    isRecording = false;
                }
                currentSilenceTime = 0f;
                continue;
            }

            int micPosition = Microphone.GetPosition(null);
            if (micPosition < 256) continue;

            recordingClip.GetData(sampleData, micPosition - 256);
            float rms = CalculateRMS(sampleData);

            if (rms > silenceThreshold)
            {
                currentSilenceTime = 0f;
                if (!isRecording)
                {
                    Debug.Log("VAD: Speech started...");
                    isRecording = true;
                    startRecordingPos = micPosition; // Rough start
                    
                    // Only show in dialog UI or console
                    // Show listening indicator in dialog UI
                    DialogUIManager.Instance?.ShowUserMessage("Listening...");
                }
            }
            else
            {
                if (isRecording)
                {
                    currentSilenceTime += Time.deltaTime;
                    if (currentSilenceTime > silenceDurationToStop)
                    {
                        Debug.Log("VAD: Silence detected, sending audio...");
                        
                        
                        // Keeping it only in console logs as requested
                        
                        isRecording = false;
                        int endRecordingPos = micPosition;
                        SendAudioData(startRecordingPos, endRecordingPos);
                    }
                }
            }
        }
    }

    private float CalculateRMS(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        return Mathf.Sqrt(sum / samples.Length);
    }

    private async void ConnectWebSocket()
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            websocket = new WebSocket(websocketUrl);

            websocket.OnOpen += () =>
            {
                Debug.Log("WebSocket Connection open!");
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
                string message = Encoding.UTF8.GetString(bytes);
                
                if (message.StartsWith("{"))
                {
                    Debug.Log("Received JSON: " + message);

                    // Try to parse for a text field (AI transcript)
                    try
                    {
                        WSMessage parsed = JsonUtility.FromJson<WSMessage>(message);

                        // type == "text" means the server is sending the AI reply text.
                        // NativeWebSocket fires OnMessage on Unity main thread via
                        // DispatchMessageQueue(), so Unity API calls are safe here.
                        if (parsed?.type == "text" && !string.IsNullOrEmpty(parsed.text))
                        {
                            string aiText = parsed.text;
                            
                            if (responseTextDisplay != null) {
                                responseTextDisplay.text = aiText;
                            }
                            
                            DialogUIManager.Instance?.ShowAIMessage(aiText);
                        }
                        else if (parsed?.type == "audio_start")
                        {
                            incomingAudioStream = new MemoryStream();
                        }
                        else if (parsed?.type == "audio_end")
                        {
                            audioQueue.Enqueue(incomingAudioStream.ToArray());
                            incomingAudioStream.Dispose();
                            incomingAudioStream = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[VoiceClient] JSON parse error: " + ex.Message);
                        if (message.Contains("audio_start"))
                            incomingAudioStream = new MemoryStream();
                        else if (message.Contains("audio_end"))
                        {
                            audioQueue.Enqueue(incomingAudioStream.ToArray());
                            incomingAudioStream.Dispose();
                            incomingAudioStream = null;
                        }
                    }
                }
                else
                {
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

    private async void SendAudioData(int startPos, int endPos)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            byte[] wavData = ConvertClipToWav(recordingClip, startPos, endPos);
            await websocket.Send(wavData);
            Debug.Log($"Sent {wavData.Length} bytes of audio data.");
        }
    }

    private IEnumerator PlayNextAudioChunk()
    {
        isPlayingTTS = true;
        byte[] mp3Data = audioQueue.Dequeue();
        
        if (mp3Data == null || mp3Data.Length <= 1)
        {
            isPlayingTTS = false;
            yield break;
        }

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

    // Helper: Convert Unity AudioClip to standard WAV byte array, handling wrap-around
    private byte[] ConvertClipToWav(AudioClip clip, int startPos, int endPos)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int hz = clip.frequency;
        int channels = clip.channels;
        
        // Handling wrap around for circular buffer
        int samplesCount = endPos >= startPos ? (endPos - startPos) : ((clip.samples - startPos) + endPos);
        if (samplesCount <= 0) return new byte[0]; // Avoid issue if calculation is strange
        
        float[] fullClipSamples = new float[clip.samples * clip.channels];
        clip.GetData(fullClipSamples, 0);
        
        float[] extractSamples = new float[samplesCount * clip.channels];
        for(int i = 0; i < samplesCount; i++)
        {
            int readPos = (startPos + i) % clip.samples;
            // Assumes mono
            extractSamples[i] = fullClipSamples[readPos];
        }

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
        for (int i = 0; i < extractSamples.Length; i++)
        {
            short sample = (short)(extractSamples[i] * 32767.0f);
            writer.Write(sample);
        }

        byte[] wavData = stream.ToArray();
        writer.Close();
        stream.Close();

        return wavData;
    }
}
