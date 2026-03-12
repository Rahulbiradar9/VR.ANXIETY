using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using TMPro;

[System.Serializable]
public class QAData
{
    public string user_text;
    public string ai_question;
    public string audio_base64;
}

public class SendAudioAPI : MonoBehaviour
{

    string apiURL = "http://127.0.0.1:8000/upload-audio";

    public AudioSource audioSource;
    public TextMeshProUGUI captionText;

    public void SendAudio()
    {
        StartCoroutine(UploadAudioContinuous());
    }

    public void ResetConversation()
    {
        StartCoroutine(ResetConversationRoutine());
    }

    IEnumerator ResetConversationRoutine()
    {
        string resetURL = "http://127.0.0.1:8000/reset-conversation";
        UnityWebRequest request = UnityWebRequest.Post(resetURL, new WWWForm());
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Conversation Reset Successfully.");
        }
        else
        {
            Debug.Log("Error resetting conversation: " + request.error);
        }
    }

    public IEnumerator FetchInitialGreeting()
    {
        string greetingURL = "http://127.0.0.1:8000/get-greeting";
        UnityWebRequest request = UnityWebRequest.Get(greetingURL);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("Greeting Raw Response: " + jsonResponse);
            
            QAData data = JsonUtility.FromJson<QAData>(jsonResponse);
            if(data != null)
            {
                if (captionText != null)
                {
                    captionText.text = data.ai_question;
                }

                if (!string.IsNullOrEmpty(data.audio_base64))
                {
                    byte[] audioBytes = System.Convert.FromBase64String(data.audio_base64);
                    string tempPath = Application.persistentDataPath + "/temp_response.wav";
                    File.WriteAllBytes(tempPath, audioBytes);
                    
                    string fileUrl = "file://" + tempPath;
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.WAV);
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        if (audioSource != null && clip != null)
                        {
                            audioSource.clip = clip;
                            audioSource.Play();
                            
                            // Wait for the audio to finish playing
                            yield return new WaitForSeconds(clip.length);
                        }
                    }
                    else
                    {
                        Debug.Log("Error loading audio clip: " + www.error);
                    }
                    www.Dispose();
                }
            }
        }
        else
        {
            Debug.Log("Error fetching greeting: " + request.error);
        }
    }

    public IEnumerator UploadAudioContinuous()
    {
        string path = Application.dataPath + "/recorded.wav";

        byte[] audioData = File.ReadAllBytes(path);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, "recorded.wav", "audio/wav");

        UnityWebRequest request = UnityWebRequest.Post(apiURL, form);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("Server Raw Response: " + jsonResponse);
            
            QAData data = JsonUtility.FromJson<QAData>(jsonResponse);
            if(data != null)
            {
                Debug.Log("User Text parsed: " + data.user_text);
                Debug.Log("AI Question parsed: " + data.ai_question);

                if (captionText != null)
                {
                    captionText.text = data.ai_question;
                }

                if (!string.IsNullOrEmpty(data.audio_base64))
                {
                    byte[] audioBytes = System.Convert.FromBase64String(data.audio_base64);
                    string tempPath = Application.persistentDataPath + "/temp_response.wav";
                    File.WriteAllBytes(tempPath, audioBytes);
                    
                    string fileUrl = "file://" + tempPath;
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.WAV);
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        if (audioSource != null && clip != null)
                        {
                            audioSource.clip = clip;
                            audioSource.Play();
                        }
                    }
                    else
                    {
                        Debug.Log("Error loading audio clip: " + www.error);
                    }
                    www.Dispose();
                }
            }
        }
        else
        {
            Debug.Log("Error: " + request.error);
        }
    }
}