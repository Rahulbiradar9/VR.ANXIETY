using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

[Serializable]
public class ProcessAudioResponse
{
    public string session_id;
    public string text;
    public string audio_base64;
}

public class AICharacterController : MonoBehaviour
{
    [Header("Network Settings")]
    public string apiEndpoint = "http://localhost:8000/process-audio";
    private string sessionId;

    [Header("Character Components")]
    [Tooltip("Animator containing the 'isTalking' boolean parameter")]
    public Animator animator;
    [Tooltip("AudioSource attached to the character's head/mouth")]
    public AudioSource audioSource;
    public string talkingAnimParam = "isTalking";

    [Header("Recording Settings")]
    public int recordFreq = 16000;
    public int maxRecordDurationSeconds = 10;
    public float silenceThreshold = 0.02f;
    public float silenceDurationToStop = 1.5f;

    private AudioClip recordingClip;
    private bool isRecording = false;
    private float currentSilenceTime = 0f;
    private int startRecordingPos = 0;
    
    // State machine to prevent recording while AI is speaking
    private enum State { Listening, Processing, Speaking }
    private State currentState = State.Listening;

    void Start()
    {
        // Generate a unique session ID for history tracking
        sessionId = Guid.NewGuid().ToString();
        
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponent<Animator>();

#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif

        // Set up the scene loop
        StartCoroutine(ContinuousInteractionLoop());
    }

    private IEnumerator ContinuousInteractionLoop()
    {
        // Request Microphone access
        recordingClip = Microphone.Start(null, true, maxRecordDurationSeconds, recordFreq);
        while (!(Microphone.GetPosition(null) > 0)) { yield return null; } 

        float[] sampleData = new float[256];

        while (true)
        {
            yield return null;

            if (currentState != State.Listening) 
            {
                // Reset recording states while waiting for response or speaking
                if (isRecording) isRecording = false;
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
                    Debug.Log("User started speaking...");
                    isRecording = true;
                    // Buffer 0.5s of audio to not cut off the first word
                    startRecordingPos = Mathf.Max(0, micPosition - (recordFreq / 2)); 
                }
            }
            else
            {
                if (isRecording)
                {
                    currentSilenceTime += Time.deltaTime;
                    if (currentSilenceTime > silenceDurationToStop)
                    {
                        Debug.Log("Silence detected, processing user speech...");
                        isRecording = false;
                        int endRecordingPos = micPosition;
                        
                        currentState = State.Processing;
                        StartCoroutine(ProcessAndSendAudio(startRecordingPos, endRecordingPos));
                    }
                }
            }
        }
    }

    private float CalculateRMS(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
        return Mathf.Sqrt(sum / samples.Length);
    }

    private IEnumerator ProcessAndSendAudio(int startPos, int endPos)
    {
        // Convert AudioClip chunk to standard WAV byte array
        byte[] wavData = ConvertClipToWav(recordingClip, startPos, endPos);
        
        // Setup multipart/form-data
        WWWForm form = new WWWForm();
        form.AddField("session_id", sessionId);
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post(apiEndpoint, form))
        {
            Debug.Log("Sending to AI backend API...");
            
            // Wait for response without blocking main Unity thread
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"API Error: {www.error}");
                currentState = State.Listening; // Reset on error to allow user to try again
            }
            else
            {
                // Parse the JSON Payload
                string jsonResponse = www.downloadHandler.text;
                ProcessAudioResponse response = JsonUtility.FromJson<ProcessAudioResponse>(jsonResponse);
                
                Debug.Log($"AI Response Text: {response.text}");
                
                // Decode and Play the Audio bytes
                StartCoroutine(PlayResponseAudio(response.audio_base64));
            }
        }
    }

    private IEnumerator PlayResponseAudio(string base64Audio)
    {
        currentState = State.Speaking;
        
        // --- 5. Lip Sync / Talking Animation ---
        animator.SetBool(talkingAnimParam, true);

        // Decode from Base64
        byte[] pcmData = Convert.FromBase64String(base64Audio);
        
        // Convert 16-bit PCM bytes to float array for AudioClip playback
        float[] floatArray = new float[pcmData.Length / 2];
        for (int i = 0; i < floatArray.Length; i++)
        {
            // The TTS service returns 16-bit little endian PCM
            short bit16 = BitConverter.ToInt16(pcmData, i * 2);
            floatArray[i] = bit16 / 32768.0f;
        }

        // Create 1-channel, 16000Hz AudioClip
        AudioClip clip = AudioClip.Create("AI_Response", floatArray.Length, 1, 16000, false);
        clip.SetData(floatArray, 0);

        audioSource.clip = clip;
        audioSource.Play();

        // Wait asynchronously until the audio finishes playing
        yield return new WaitForSeconds(clip.length);

        // Stop the talking animation
        animator.SetBool(talkingAnimParam, false);
        
        // Resume listening
        currentState = State.Listening;
        currentSilenceTime = 0f;
        Debug.Log("AI finished speaking. Listening for user...");
    }

    private byte[] ConvertClipToWav(AudioClip clip, int startPos, int endPos)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int hz = clip.frequency;
        int channels = clip.channels;
        
        // Calculate total samples correctly addressing circular buffer wrap-around
        int samplesCount = endPos >= startPos ? (endPos - startPos) : ((clip.samples - startPos) + endPos);
        if (samplesCount <= 0) return new byte[0];
        
        float[] fullClipSamples = new float[clip.samples * channels];
        clip.GetData(fullClipSamples, 0);
        
        float[] extractSamples = new float[samplesCount * channels];
        for(int i = 0; i < samplesCount; i++)
        {
            int readPos = (startPos + i) % clip.samples;
            extractSamples[i] = fullClipSamples[readPos];
        }

        // WAV Header Specification
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

        // Raw 16-bit PCM data
        for (int i = 0; i < extractSamples.Length; i++)
        {
            short sample = (short)(Mathf.Clamp(extractSamples[i], -1f, 1f) * 32767.0f);
            writer.Write(sample);
        }

        byte[] wavData = stream.ToArray();
        writer.Close();
        stream.Close();

        return wavData;
    }
}
