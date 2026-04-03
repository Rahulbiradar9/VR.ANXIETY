using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A modular script that syncs typewriter text animation with audio playback,
/// while automatically and smoothly scrolling a ScrollRect to the bottom.
/// </summary>
public class AutoScrollingTypewriter : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The TextMeshProUGUI component where the text will be displayed.")]
    public TextMeshProUGUI textDisplay;
    
    [Tooltip("The ScrollRect containing the text. Ensure a ContentSizeFitter is on the Text element.")]
    public ScrollRect scrollRect;

    [Header("Audio Synchronization")]
    [Tooltip("The AudioSource playing the voice. Required if Sync With Audio is enabled.")]
    public AudioSource audioSource;
    
    [Tooltip("If true, typing speed dynamically adjusts to match exactly when the audio finishes.")]
    public bool syncWithAudio = true;

    [Header("Typewriter Settings")]
    [Tooltip("Characters per second when not syncing with audio or no audio is playing.")]
    public float defaultCharactersPerSecond = 30f;
    
    [Tooltip("Time taken for the scroll to catch up to the bottom smoothly.")]
    public float scrollSmoothTime = 0.1f;

    // --- State Properties ---
    public bool IsTyping { get; private set; }
    public bool IsPaused { get; private set; }

    // --- Internal State ---
    private string fullTargetText = "";
    private int currentlyTypedLength = 0;
    
    private float currentScrollVelocity = 0f;
    private Coroutine typingCoroutine;

    /// <summary>
    /// Starts typing a completely new text block from scratch. 
    /// This clears existing text.
    /// </summary>
    /// <param name="newText">The text string to type out.</param>
    public void TypeText(string newText)
    {
        fullTargetText = newText;
        currentlyTypedLength = 0;
        
        if (textDisplay != null) textDisplay.text = "";
        
        StartTyping();
    }

    /// <summary>
    /// Appends text to the existing typewriter queue.
    /// Typing will seamlessly resume if it had completed.
    /// </summary>
    /// <param name="appendedText">The new text to add to the end.</param>
    public void AppendText(string appendedText)
    {
        fullTargetText += appendedText;
        
        if (!IsTyping && typingCoroutine == null)
        {
            StartTyping();
        }
    }

    /// <summary>
    /// Pauses the typing effect.
    /// </summary>
    public void PauseTyping()
    {
        IsPaused = true;
    }

    /// <summary>
    /// Resumes the typing effect.
    /// </summary>
    public void ResumeTyping()
    {
        IsPaused = false;
    }

    /// <summary>
    /// Instantly displays all queued text and snaps the scrollbar to the bottom.
    /// </summary>
    public void CompleteInstantly()
    {
        if (textDisplay == null) return;
        
        currentlyTypedLength = fullTargetText.Length;
        textDisplay.text = fullTargetText;
        
        // Force layout update so the Content Rect expands immediately
        Canvas.ForceUpdateCanvases();
        
        if (scrollRect != null)
        {
            // 0 = bottom, 1 = top
            scrollRect.verticalNormalizedPosition = 0f;
        }

        IsTyping = false;
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    // --- Internal Implementation ---

    private void Update()
    {
        // Continuously dampen scrollbar down to the bottom while typing is active.
        if (IsTyping && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = Mathf.SmoothDamp(
                scrollRect.verticalNormalizedPosition, 
                0f, 
                ref currentScrollVelocity, 
                scrollSmoothTime
            );
        }
    }

    private void StartTyping()
    {
        if (textDisplay == null)
        {
            Debug.LogWarning("AutoScrollingTypewriter: TextMeshProUGUI Display is missing!");
            return;
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        IsPaused = false;
        typingCoroutine = StartCoroutine(TypingRoutine());
    }

    private IEnumerator TypingRoutine()
    {
        IsTyping = true;
        float timeAccumulator = 0f;

        while (currentlyTypedLength < fullTargetText.Length)
        {
            if (IsPaused)
            {
                yield return null;
                continue;
            }

            float currentSpeed = CalculateTypingSpeed();
            timeAccumulator += Time.deltaTime;
            
            float timePerChar = 1f / Mathf.Max(currentSpeed, 1f);
            int charsToTypeThisFrame = 0;
            
            while (timeAccumulator >= timePerChar && currentlyTypedLength < fullTargetText.Length)
            {
                timeAccumulator -= timePerChar;
                charsToTypeThisFrame++;
                currentlyTypedLength++;
            }

            if (charsToTypeThisFrame > 0)
            {
                textDisplay.text = fullTargetText.Substring(0, currentlyTypedLength);
                
                // Forces the layout to rebuild instantly so the ScrollRect knows its new height
                // This enables the scrolling update to correctly seek the true bottom
                Canvas.ForceUpdateCanvases();
            }

            yield return null;
        }

        IsTyping = false;
        typingCoroutine = null;
    }

    /// <summary>
    /// Calculates the dynamic speed required to finish tying the remaining 
    /// characters before the audio finishes playing.
    /// </summary>
    private float CalculateTypingSpeed()
    {
        if (syncWithAudio && audioSource != null && audioSource.isPlaying && audioSource.clip != null)
        {
            float remainingAudioTime = audioSource.clip.length - audioSource.time;
            int charsRemaining = fullTargetText.Length - currentlyTypedLength;
            
            // Prevent division by zero and extreme zooming at the literal tail end of the audio clip
            if (remainingAudioTime > 0.05f && charsRemaining > 0)
            {
                return (float)charsRemaining / remainingAudioTime;
            }
        }
        
        // Fallback to inspector default speed
        return defaultCharactersPerSecond;
    }
}
