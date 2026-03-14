using UnityEngine;
using System.IO;

public class MicrophoneRecorder : MonoBehaviour
{
    AudioClip clip;
    string device;
    public SendAudioAPI sendAudioAPI;

    System.Collections.IEnumerator Start()
    {
        if (Microphone.devices.Length > 0)
        {
            device = Microphone.devices[0];
            Debug.Log("Microphone: " + device);
        }
        else
        {
            Debug.LogError("No microphone detected!");
        }

        if (sendAudioAPI == null)
        {
            sendAudioAPI = FindObjectOfType<SendAudioAPI>();
        }

        // Fetch and play the initial AI greeting FIRST
        if (sendAudioAPI != null)
        {
            Debug.Log("Fetching initial AI greeting...");
            yield return StartCoroutine(sendAudioAPI.FetchInitialGreeting());
        }

        // Automatically start the continuous recording loop AFTER the greeting
        Debug.Log("Greeting finished, starting continuous recording loop.");
        ToggleContinuousRecording();
    }

    public void StartRecording()
    {
        if (device != null)
        {
            clip = Microphone.Start(device, false, 10, 44100);
            Debug.Log("Recording Started");
        }
    }

    public void StopRecording()
    {
        if (Microphone.IsRecording(device))
        {
            Microphone.End(device);

            byte[] wavData = WavUtility.FromAudioClip(clip);

            string path = Application.dataPath + "/recorded.wav";

            File.WriteAllBytes(path, wavData);

            Debug.Log("Saved: " + path);

            if (sendAudioAPI != null)
            {
                Debug.Log("Sending audio to server...");
                sendAudioAPI.SendAudio();
            }
        }
    }

    public float threshold = 0.02f;
    public float silenceDelay = 1.5f;
    
    private bool isContinuousRecording = false;
    private bool isVoiceDetected = false;
    private float timeSinceLastVoice = 0f;

    public void ToggleContinuousRecording() 
    {
        isContinuousRecording = !isContinuousRecording;
        if (isContinuousRecording) 
        {
            Debug.Log("Starting continuous recording loop...");
            StartCoroutine(ContinuousRecordingLoop());
        } 
        else 
        {
            Debug.Log("Stopped continuous recording loop.");
            StopAllCoroutines();
            if (Microphone.IsRecording(device)) Microphone.End(device);
        }
    }

    System.Collections.IEnumerator ContinuousRecordingLoop()
    {
        while (isContinuousRecording)
        {
            if (device == null) yield break;

            // Wait until the AI has finished speaking before listening for user input
            if (sendAudioAPI != null && sendAudioAPI.isPlayingAudio)
            {
                Debug.Log("AI is speaking, waiting before listening...");
                while (sendAudioAPI.isPlayingAudio)
                    yield return null;
                Debug.Log("AI finished speaking, now listening for user.");
            }

            clip = Microphone.Start(device, false, 60, 44100);
            Debug.Log("Listening for voice...");
            
            isVoiceDetected = false;
            timeSinceLastVoice = 0f;
            int lastPosition = 0;
            
            while (!(Microphone.GetPosition(device) > 0)) { yield return null; }
            
            while (Microphone.IsRecording(device) && isContinuousRecording)
            {
                int currentPosition = Microphone.GetPosition(device);
                if (currentPosition < 0 || lastPosition == currentPosition) 
                {
                    yield return null;
                    continue;
                }

                if (currentPosition > 0)
                {
                    float[] samples = new float[128];
                    int readPos = currentPosition - 128;
                    if (readPos < 0) readPos = 0;
                    
                    clip.GetData(samples, readPos);
                    
                    float maxVolume = 0f;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        if (Mathf.Abs(samples[i]) > maxVolume)
                            maxVolume = Mathf.Abs(samples[i]);
                    }

                    if (maxVolume > threshold)
                    {
                        if (!isVoiceDetected)
                        {
                            Debug.Log("Voice detected! Recording phrase...");
                            isVoiceDetected = true;
                        }
                        timeSinceLastVoice = 0f;
                    }
                    else if (isVoiceDetected)
                    {
                        timeSinceLastVoice += Time.deltaTime;
                        if (timeSinceLastVoice >= silenceDelay)
                        {
                            Debug.Log("Silence detected, finalizing recording...");
                            break;
                        }
                    }
                }
                
                lastPosition = currentPosition;
                yield return null;
            }
            
            if (isVoiceDetected)
            {
                int recordPosition = Microphone.GetPosition(device);
                Microphone.End(device);

                if (recordPosition > 0)
                {
                    AudioClip trimmedClip = AudioClip.Create("TrimmedClip", recordPosition, clip.channels, clip.frequency, false);
                    float[] data = new float[recordPosition * clip.channels];
                    clip.GetData(data, 0);
                    trimmedClip.SetData(data, 0);

                    byte[] wavData = WavUtility.FromAudioClip(trimmedClip);
                    string path = Application.dataPath + "/recorded.wav";
                    File.WriteAllBytes(path, wavData);
                    Debug.Log("Saved trimmed audio: " + path);

                    if (sendAudioAPI != null)
                    {
                        Debug.Log("Sending audio to server...");
                        yield return StartCoroutine(sendAudioAPI.UploadAudioContinuous());
                    }
                }
            }
            else
            {
                Microphone.End(device);
            }
            
            yield return null;
        }
    }
}