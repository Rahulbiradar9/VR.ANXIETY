using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-Space dialog panel that floats above / beside a character and
/// shows only the AI's spoken lines.  Attach to the same GameObject as
/// (or as a child of) your character, or set FollowTarget at runtime.
/// </summary>
public class DialogUIManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static DialogUIManager Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Follow Target")]
    [Tooltip("The character Transform the canvas should float above. " +
             "If left empty, uses this GameObject's transform.")]
    public Transform followTarget;

    [Tooltip("Offset from the character in LOCAL world space (e.g. 0,2,0 = 2 m above head)")]
    public Vector3 positionOffset = new Vector3(-0.6f, 2.0f, 0f);

    [Tooltip("Rotate to always face the main camera (billboard)")]
    public bool billboardMode = true;

    [Header("UI References")]
    [Tooltip("The ScrollRect component (assign in Inspector after Canvas setup)")]
    public ScrollRect scrollRect;

    [Tooltip("The Content RectTransform inside ScrollRect → Viewport")]
    public RectTransform contentParent;

    [Header("Appearance")]
    [Tooltip("Max messages kept visible before old ones are removed")]
    public int maxMessages = 6;

    [Tooltip("Panel background color")]
    public Color panelColor = new Color(0.05f, 0.05f, 0.05f, 0.82f);

    [Tooltip("AI bubble color")]
    public Color aiBubbleColor = new Color(0.18f, 0.52f, 0.90f, 1f);

    [Tooltip("Font size for messages")]
    public float fontSize = 14f;

    // ── Privates ─────────────────────────────────────────────────────────────
    private Canvas _canvas;
    private readonly List<GameObject> _messages = new List<GameObject>();
    private Camera _cam;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Always build the canvas hierarchy at runtime so contentParent is
        // always valid, regardless of any Canvas objects in the scene.
        BuildCanvasAtRuntime();

        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        // Only reposition if a DIFFERENT target is explicitly assigned.
        // If followTarget is null, let Unity's parent-child hierarchy handle it.
        if (followTarget != null && followTarget != transform)
        {
            transform.position = followTarget.position + positionOffset;
        }

        // Billboard — always face the main camera
        if (billboardMode && _cam != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - _cam.transform.position);
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Show a line of AI dialog in the panel.</summary>
    public void ShowAIMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        SpawnBubble(text);
    }

    /// <summary>
    /// Legacy stub kept so AICharacterController compiles.
    /// User messages are intentionally NOT shown in this panel.
    /// </summary>
    public void ShowUserMessage(string text) { /* intentionally silent */ }

    /// <summary>Remove all messages.</summary>
    public void ClearDialog()
    {
        foreach (var m in _messages)
            if (m) Destroy(m);
        _messages.Clear();
    }

    // ── Internals ────────────────────────────────────────────────────────────

    private void SpawnBubble(string text)
    {
        if (contentParent == null)
        {
            Debug.LogError("[DialogUIManager] contentParent is null — canvas was not built correctly.");
            return;
        }

        // Trim old messages
        while (_messages.Count >= maxMessages)
        {
            if (_messages[0]) Destroy(_messages[0]);
            _messages.RemoveAt(0);
        }

        // Container bubble
        GameObject bubble = new GameObject("AI_Line");
        bubble.transform.SetParent(contentParent, false);

        Image bg = bubble.AddComponent<Image>();
        bg.color = aiBubbleColor;

        HorizontalLayoutGroup hlg = bubble.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 6, 6);
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter csf = bubble.AddComponent<ContentSizeFitter>();
        csf.horizontalFit   = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit     = ContentSizeFitter.FitMode.PreferredSize;

        // Text
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(bubble.transform, false);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text              = text;
        tmp.fontSize          = fontSize;
        tmp.color             = Color.white;
        tmp.textWrappingMode  = TMPro.TextWrappingModes.Normal;
        tmp.alignment         = TextAlignmentOptions.Left;

        _messages.Add(bubble);
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;
    }

    // ── Runtime Canvas Builder ────────────────────────────────────────────────
    // Called only if you have NOT set up a Canvas manually in the Editor.

    private void BuildCanvasAtRuntime()
    {
        // Root canvas GO
        GameObject canvasGo = new GameObject("DialogCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode     = RenderMode.WorldSpace;
        _canvas.worldCamera    = Camera.main;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // Size the canvas rect (width × height in world units before scaling)
        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(300f, 160f);

        // ── Saved transform from manual positioning ───────────────────────
        canvasGo.transform.localPosition = new Vector3(1.068005f, 1.54f, 0f);
        canvasGo.transform.localRotation = Quaternion.Euler(0f, -12.817f, 0f);
        canvasGo.transform.localScale    = new Vector3(0.006733f, 0.005f, 0.005f);
        // ─────────────────────────────────────────────────────────────────

        // Panel background
        GameObject panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        Image panelImg = panelGo.AddComponent<Image>();
        panelImg.color = panelColor;
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Scroll View
        GameObject scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(panelGo.transform, false);
        scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(6, 6);
        scrollRt.offsetMax = new Vector2(-6, -6);

        // Viewport
        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        Image vpImg = viewportGo.AddComponent<Image>();
        vpImg.color = Color.clear;
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform vpRt = viewportGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        scrollRect.viewport = vpRt;

        // Content
        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        contentParent = contentGo.AddComponent<RectTransform>();
        contentParent.anchorMin = new Vector2(0, 1);
        contentParent.anchorMax = new Vector2(1, 1);
        contentParent.pivot     = new Vector2(0.5f, 1f);
        contentParent.offsetMin = Vector2.zero;
        contentParent.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding           = new RectOffset(4, 4, 4, 4);
        vlg.spacing           = 5f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;

        ContentSizeFitter contentCSF = contentGo.AddComponent<ContentSizeFitter>();
        contentCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentCSF.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentParent;
    }
}
