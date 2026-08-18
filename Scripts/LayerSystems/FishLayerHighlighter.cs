// ============================================================================
// FishLayerHighlighter.cs
// Tube Tank AR — Blue Aquarium Project
// Aquaria KLCC × Asia Pacific University (APU)
// Author: Christian Nathaniel Tjandra
//
// Manages the temperature-layer background visuals inside the Fish Info
// content block. Supports fish that inhabit more than one layer — a fish
// tap can highlight multiple layers at once via an int[] rather than a
// single index. Exposes a static Instance so close buttons can reset state
// without needing a scene reference wired to every one of them.
//
// Shared for portfolio and educational reference — see repository README
// for full context, architecture notes, and license terms.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FishLayerHighlighter — Manages CONTENT_C temperature layer visual states
/// Attach to CONTENT_C GameObject
/// Has a static Instance so any close button can call ResetAll() directly
/// </summary>
public class FishLayerHighlighter : MonoBehaviour
{
    [Header("--- CONTENT C LAYER OBJECTS ---")]
    public GameObject layer_Warm;
    public GameObject layer_Middle;
    public GameObject layer_Cool;

    [Header("--- ALPHA SETTINGS ---")]
    [Range(0f, 1f)]
    [Tooltip("Alpha of all layers when no fish selected — decorative background")]
    public float backgroundAlpha = 0.12f;

    [Range(0f, 1f)]
    [Tooltip("Alpha of highlighted layer when matching fish is tapped")]
    public float highlightAlpha  = 0.55f;

    public float fadeDuration    = 0.4f;

    // Static instance so close buttons can call ResetAll() without needing a reference
    public static FishLayerHighlighter Instance;

    private List<Material> _mats_Warm   = new List<Material>();
    private List<Material> _mats_Middle = new List<Material>();
    private List<Material> _mats_Cool   = new List<Material>();

    private bool _initialized = false;

    void Awake()  { Instance = this; }
    void Start()  { Initialize(); }
    void OnEnable()
    {
        Instance = this;
        if (!_initialized) Initialize();
        else SetAllToBackground();
    }

    private void Initialize()
    {
        CacheLayer(layer_Warm,   _mats_Warm,   "WARM_FISH");
        CacheLayer(layer_Middle, _mats_Middle, "MIDDLE_FISH");
        CacheLayer(layer_Cool,   _mats_Cool,   "COOL_FISH");
        SetAllToBackground();
        _initialized = true;
        Debug.Log("[FishLayerHighlighter] Initialized OK");
    }

    private void CacheLayer(GameObject layer, List<Material> mats, string name)
    {
        mats.Clear();
        if (layer == null) { Debug.LogWarning("[FishLayerHighlighter] " + name + " not wired!"); return; }
        Renderer[] renderers = layer.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            foreach (var mat in r.materials)
                if (mat.HasProperty("_BaseColor") || mat.HasProperty("_Color"))
                    mats.Add(mat);
        Debug.Log("[FishLayerHighlighter] " + name + " cached " + mats.Count + " mats");
    }

    // ── Called by FishTapHandler when fish is tapped ──────────────────────────

    public void HighlightLayers(int[] layerIndices)
    {
        var toHighlight = new HashSet<int>(layerIndices);
        for (int i = 0; i < 3; i++)
        {
            float target = toHighlight.Contains(i) ? highlightAlpha : backgroundAlpha;
            StartCoroutine(FadeLayerAlpha(GetMats(i), target));
        }
        Debug.Log("[FishLayerHighlighter] Highlighting layers: " + string.Join(", ", layerIndices));
    }

    // ── Called by FishTapHandler OR close button directly ────────────────────
    // Close buttons can call FishLayerHighlighter.Instance.ResetAll() directly

    public void ResetAll()
    {
        StopAllCoroutines();
        SetAllToBackground();
        Debug.Log("[FishLayerHighlighter] Reset to background");
    }

    public void SetAllToBackground()
    {
        SetListAlpha(_mats_Warm,   backgroundAlpha);
        SetListAlpha(_mats_Middle, backgroundAlpha);
        SetListAlpha(_mats_Cool,   backgroundAlpha);
    }

    private IEnumerator FadeLayerAlpha(List<Material> mats, float target)
    {
        if (mats == null || mats.Count == 0) yield break;
        float current = GetMatAlpha(mats[0]);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetListAlpha(mats, Mathf.Lerp(current, target,
                Mathf.Clamp01(elapsed / fadeDuration)));
            yield return null;
        }
        SetListAlpha(mats, target);
    }

    private void SetListAlpha(List<Material> mats, float alpha)
    {
        foreach (var mat in mats)
            if (mat != null) SetMatAlpha(mat, alpha);
    }

    private void SetMatAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor"); c.a = alpha;
            mat.SetColor("_BaseColor", c);
        }
        else if (mat.HasProperty("_Color"))
        {
            Color c = mat.GetColor("_Color"); c.a = alpha;
            mat.SetColor("_Color", c);
        }
    }

    private float GetMatAlpha(Material mat)
    {
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor").a;
        if (mat.HasProperty("_Color"))     return mat.GetColor("_Color").a;
        return 1f;
    }

    private List<Material> GetMats(int i)
    {
        switch(i){ case 0: return _mats_Warm; case 1: return _mats_Middle; default: return _mats_Cool; }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        SetAllToBackground();
        if (Instance == this) Instance = null;
    }
}
