using UnityEngine;
using UnityEditor;

public class VoiceInteractionSetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Voice Client")]
    public static void SetupVoiceClient()
    {
        // 1. Find or create a manager object in the scene
        GameObject manager = GameObject.Find("VoiceInteractionManager");
        if (manager == null)
        {
            manager = new GameObject("VoiceInteractionManager");
        }

        // 2. Add AudioSource if it doesn't exist
        AudioSource audioSource = manager.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = manager.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 3. Add VoiceInteractionClient if it doesn't exist
        VoiceInteractionClient client = manager.GetComponent<VoiceInteractionClient>();
        if (client == null)
        {
            client = manager.AddComponent<VoiceInteractionClient>();
        }

        // 4. Link the AudioSource
        client.audioSource = audioSource;

        // Mark scene as dirty so it saves the changes
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        Debug.Log("Voice Interaction Setup Complete! The 'VoiceInteractionManager' GameObject has been created/updated with the necessary components.");
    }
}
