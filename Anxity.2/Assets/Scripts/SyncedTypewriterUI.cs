using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Synchronizes AI-generated text, audio playback (TTS), and UI scrolling.
/// Production-ready and Modular system.
/// </summary>
public class SyncedTypewriterUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The TextMeshPro target to display text. Must have a ContentSizeFitter attached on the same object.")]
    public TextMeshProUGUI textDisplay;
    
    [Tooltip("The ScrollRect that contains the text. Used for smooth bottom auto-scrolling.")]
    public ScrollRect scrollRect;

    [Header("Audio Settings")]
    [Tooltip("AudioSource used to play the AI TTS.")]
    public AudioSource audioSource;

    [Header("Typing Timing")]
    [Tooltip("Variance applied to each character typed to create a more 'human' randomness.")]
    [Range(0f, 0.05f)] public float randomDelayVariance = 0.02f;
    [Tooltip("Fallback speed (secrets per character) when no Audio Source is provided.")]
    public float fallbackTypingSpeed = 0.04f;

    [Header("Scrolling Setup")]
    [Tooltip("Smooth time applied to the vertical dampening of the ScrollRect.")]
    public float scrollSmoothTime = 0.1f;

    // --- State Accessors ---
    public bool IsTyping { get; private set; }
    public bool IsPaused { get; private set; }

    // --- Internal Trackers ---
    private float scrollVelocity = 0f;
    private Coroutine activeSequence;
    private string fullTargetText = "";
    
    /// <summary>
    /// Initiates the synchronized display and playback of an AI response.
    /// Wait until the audio clip is fully loaded, start it, and sync text to it.
    /// </summary>
    /// <param name="response">The AI's generated text.</param>
    /// <param name="clip">The generated TTS audio clip.</param>
    public void StartInteraction(string response, AudioClip clip)
    {
        if (activeSequence != null) StopCoroutine(activeSequence);
        
        fullTargetText = response;
        activeSequence = StartCoroutine(SyncSequence(response, clip));
    }

    // ============================================
    // CORE BEHAVIOR
    // ============================================

    private IEnumerator SyncSequence(string text, AudioClip clip)
    {
        IsTyping = true;
        IsPaused = false;
        textDisplay.text = "";

        if (clip != null)
        {
            // Wait until the audio clip is fully loaded and ready
            yield return StartCoroutine(LoadAudio(clip));

            // Start audio playback first
            PlayAudio(clip);

            // Wait for engine audio state to ensure synchronization
            yield return new WaitUntil(() => audioSource.isPlaying);

            // Begin a typewriter effect ONLY after audio starts playing
            yield return StartCoroutine(TypeText(text, clip));
        }
        else
        {
            // Execute fallback standalone sequence
            yield return StartCoroutine(TypeTextStandalone(text));
        }

        IsTyping = false;
        activeSequence = null;
    }

    private IEnumerator LoadAudio(AudioClip clip)
    {
        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        while (clip.loadState == AudioDataLoadState.Loading)
        {
            yield return null;
        }

        if (clip.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogError("SyncedTypewriterUI: Failed to load AudioData from TTS Clip.");
        }
    }

    private void PlayAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    private IEnumerator TypeText(string text, AudioClip clip)
    {
        int totalCharacters = text.Length;
        int charsTyped = 0;

        // typingSpeed must dynamically match the audio duration: audioClip.length / totalCharacters
        float baseTypingSpeed = totalCharacters > 0 ? clip.length / totalCharacters : 0f;

        while (charsTyped < totalCharacters)
        {
            // Check for early Audio End
            // If the audio legitimately stopped and didn't just pause, we should break and complete.
            if (!audioSource.isPlaying && !IsPaused)
            {
                if (audioSource.time == 0f && charsTyped > 0)
                {
                    break;
                }
                
                // If it's buffering or engine lag, we hold execution.
                yield return null;
                continue;
            }

            // Sync Handling: If audio pauses or buffers, typing should also pause.
            if (IsPaused)
            {
                yield return null;
                continue;
            }

            // Advance organically
            charsTyped++;
            
            // Substring handles ContentSizeFitter updates efficiently so the ScrollRect organically grows.
            textDisplay.text = text.Substring(0, charsTyped);
            Canvas.ForceUpdateCanvases(); 

            // Add slight random delay variation for natural typing
            float variance = Random.Range(-randomDelayVariance, randomDelayVariance);
            float currentDelay = Mathf.Max(0.01f, baseTypingSpeed + variance);

            yield return new WaitForSeconds(currentDelay);

            // Catch-up sync mechanism: 
            // Ensures if framerates drop, the text snaps directly to where the audio position expects it to be
            float expectedProgress = audioSource.time / clip.length;
            int expectedChars = Mathf.Clamp((int)(expectedProgress * totalCharacters), 0, totalCharacters);

            if (charsTyped < expectedChars)
            {
                charsTyped = expectedChars;
                textDisplay.text = text.Substring(0, charsTyped);
                Canvas.ForceUpdateCanvases();
            }
        }

        // If audio ends early, text should complete immediately.
        if (charsTyped < totalCharacters)
        {
            textDisplay.text = text;
            Canvas.ForceUpdateCanvases();
        }
    }

    private IEnumerator TypeTextStandalone(string text)
    {
        int totalCharacters = text.Length;
        int charsTyped = 0;

        while (charsTyped < totalCharacters)
        {
            if (IsPaused)
            {
                yield return null;
                continue;
            }

            charsTyped++;
            textDisplay.text = text.Substring(0, charsTyped);
            Canvas.ForceUpdateCanvases();

            float variance = Random.Range(-randomDelayVariance, randomDelayVariance);
            yield return new WaitForSeconds(Mathf.Max(0.01f, fallbackTypingSpeed + variance));
        }
        
        textDisplay.text = text;
    }

    // ============================================
    // SCROLL HANDLING
    // ============================================

    private void Update()
    {
        HandleScroll();
    }

    private void HandleScroll()
    {
        // While text is being typed, smoothly stay at the bottom
        // Stops dampening when typing concludes to restore user scroll authority
        if (IsTyping && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = Mathf.SmoothDamp(
                scrollRect.verticalNormalizedPosition, 
                0f, 
                ref scrollVelocity, 
                scrollSmoothTime
            );
        }
    }

    // ============================================
    // CONTROL API (BONUS)
    // ============================================

    /// <summary>
    /// Pauses printing and TTS audio playback simultaneously.
    /// </summary>
    public void Pause()
    {
        IsPaused = true;
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    /// <summary>
    /// Resumes typing operation and TTS audio.
    /// </summary>
    public void Resume()
    {
        IsPaused = false;
        if (audioSource != null)
        {
            audioSource.UnPause();
        }
    }

    /// <summary>
    /// Skips/fast-forwards typing and stops audio on user input.
    /// </summary>
    public void Skip()
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }

        IsTyping = false;
        IsPaused = false;
        textDisplay.text = fullTargetText;
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
