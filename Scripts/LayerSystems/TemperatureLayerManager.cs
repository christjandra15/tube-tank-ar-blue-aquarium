// ============================================================================
// TemperatureLayerManager.cs
// Tube Tank AR — Blue Aquarium Project
// Aquaria KLCC × Asia Pacific University (APU)
// Author: Christian Nathaniel Tjandra
//
// Handles selection of the three lake temperature layers (warm / transition /
// cool). Highlighting is done via a soft UI Image glow overlaid on each
// layer rather than a 3D mesh glow effect — far more reliable against the
// non-uniformly scaled cylinder meshes than trying to drive emissive
// material properties directly.
//
// Shared for portfolio and educational reference — see repository README
// for full context, architecture notes, and license terms.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TemperatureLayerManager — Layer selection with UI-based glow highlight
/// Uses a UI Image overlay per layer instead of 3D mesh glow
/// Much more reliable with non-uniform scaled meshes
///
/// GLOW SETUP:
/// Create a UI Image inside TempraturePanel for each layer
/// Use a soft circular/oval gradient sprite
/// Position it to overlap each cylinder layer visually
/// The script fades the Image alpha in/out
/// </summary>
public class TemperatureLayerManager : MonoBehaviour
{
    [Header("--- LAYER GAMEOBJECTS ---")]
    public GameObject layer_Warm;
    public GameObject layer_Middle;
    public GameObject layer_Cool;

    [Header("--- INFO PANELS ---")]
    public GameObject panel_Warm;
    public GameObject panel_Middle;
    public GameObject panel_Cool;

    [Header("--- ALL ARROWS (shared) ---")]
    public GameObject[] allArrowParents;

    [Header("--- AUDIO ---")]
    public AudioSource audio_Warm;
    public AudioSource audio_Middle;
    public AudioSource audio_Cool;

    [Header("--- UI GLOW HIGHLIGHTS ---")]
    [Tooltip("UI Image that glows over the warm/red layer when selected")]
    public Image glowHighlight_Warm;
    [Tooltip("UI Image that glows over the middle/yellow layer when selected")]
    public Image glowHighlight_Middle;
    [Tooltip("UI Image that glows over the cool/blue layer when selected")]
    public Image glowHighlight_Cool;

    [Header("--- GLOW SETTINGS ---")]
    [Range(0f, 1f)]
    public float glowAlphaTarget   = 0.6f;
    public float glowFadeDuration  = 0.4f;

    [Header("--- DIM SETTINGS ---")]
    [Range(0f, 1f)]
    public float dimmedAlpha       = 0.08f;
    public float panelFadeDuration = 0.3f;

    // Cached layer material instances + original alphas
    private List<Material> _mats_Warm   = new List<Material>();
    private List<Material> _mats_Middle = new List<Material>();
    private List<Material> _mats_Cool   = new List<Material>();

    private List<float> _origAlpha_Warm   = new List<float>();
    private List<float> _origAlpha_Middle = new List<float>();
    private List<float> _origAlpha_Cool   = new List<float>();

    private int  _selectedIndex = -1;
    private bool _initialized   = false;

    void Start()    { InitializeMaterials(); }
    void OnEnable() { if (!_initialized) InitializeMaterials(); else RestoreAllOriginalAlphas(); }

    // ── Initialize ────────────────────────────────────────────────────────────

    private void InitializeMaterials()
    {
        _mats_Warm.Clear();   _origAlpha_Warm.Clear();
        _mats_Middle.Clear(); _origAlpha_Middle.Clear();
        _mats_Cool.Clear();   _origAlpha_Cool.Clear();

        CacheLayer(layer_Warm,   _mats_Warm,   _origAlpha_Warm,   "WARM");
        CacheLayer(layer_Middle, _mats_Middle, _origAlpha_Middle, "MIDDLE");
        CacheLayer(layer_Cool,   _mats_Cool,   _origAlpha_Cool,   "COOL");

        // All glow highlights start invisible
        SetHighlightAlpha(glowHighlight_Warm,   0f);
        SetHighlightAlpha(glowHighlight_Middle, 0f);
        SetHighlightAlpha(glowHighlight_Cool,   0f);

        _initialized   = true;
        _selectedIndex = -1;

        Debug.Log("[TempLayerMgr] Initialized OK");
    }

    private void CacheLayer(GameObject layer, List<Material> mats,
                             List<float> alphas, string name)
    {
        if (layer == null) { Debug.LogWarning("[TempLayerMgr] " + name + " not wired!"); return; }

        Renderer[] renderers = layer.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // Skip glow mesh renderers — only cache actual layer materials
            if (r.gameObject.name.StartsWith("GlowMesh")) continue;

            foreach (var mat in r.materials)
            {
                if (!mat.HasProperty("_BaseColor") && !mat.HasProperty("_Color")) continue;
                mats.Add(mat);
                float a = mat.HasProperty("_BaseColor")
                    ? mat.GetColor("_BaseColor").a
                    : mat.GetColor("_Color").a;
                alphas.Add(a);
            }
        }
        Debug.Log("[TempLayerMgr] " + name + " cached " + mats.Count + " mats");
    }

    // ── Called by InputHandler ────────────────────────────────────────────────

    public void OnLayerTapped(int layerIndex)
    {
        if (ButtonClickSound.Instance != null)
            ButtonClickSound.Instance.PlayClickSound();

        if (_selectedIndex == layerIndex) { ResetAll(); return; }
        SelectLayer(layerIndex);
    }

    private void SelectLayer(int index)
    {
        _selectedIndex = index;

        for (int i = 0; i < 3; i++)
        {
            bool isSelected = (i == index);

            if (isSelected)
            {
                RestoreLayerAlpha(GetMats(i), GetOrigAlphas(i));
                StartCoroutine(FadeHighlight(GetHighlight(i), glowAlphaTarget));
                ShowPanel(GetPanel(i));
            }
            else
            {
                SetListAlpha(GetMats(i), dimmedAlpha);
                StartCoroutine(FadeHighlight(GetHighlight(i), 0f));
                HidePanel(GetPanel(i), GetAudio(i));
            }
        }

        SetAllArrows(false);
        Debug.Log("[TempLayerMgr] Selected: " + index);
    }

    public void ResetAll()
    {
        _selectedIndex = -1;
        RestoreAllOriginalAlphas();

        StartCoroutine(FadeHighlight(glowHighlight_Warm,   0f));
        StartCoroutine(FadeHighlight(glowHighlight_Middle, 0f));
        StartCoroutine(FadeHighlight(glowHighlight_Cool,   0f));

        for (int i = 0; i < 3; i++)
            HidePanel(GetPanel(i), GetAudio(i));

        SetAllArrows(true);
        Debug.Log("[TempLayerMgr] Reset all");
    }

    // ── UI Glow highlight ─────────────────────────────────────────────────────

    private IEnumerator FadeHighlight(Image img, float target)
    {
        if (img == null) yield break;

        float start   = img.color.a;
        float elapsed = 0f;

        while (elapsed < glowFadeDuration)
        {
            elapsed += Time.deltaTime;
            SetHighlightAlpha(img, Mathf.Lerp(start, target,
                Mathf.Clamp01(elapsed / glowFadeDuration)));
            yield return null;
        }
        SetHighlightAlpha(img, target);
    }

    private void SetHighlightAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a     = alpha;
        img.color = c;
    }

    // ── Alpha helpers ─────────────────────────────────────────────────────────

    private void RestoreAllOriginalAlphas()
    {
        RestoreLayerAlpha(_mats_Warm,   _origAlpha_Warm);
        RestoreLayerAlpha(_mats_Middle, _origAlpha_Middle);
        RestoreLayerAlpha(_mats_Cool,   _origAlpha_Cool);
    }

    private void RestoreLayerAlpha(List<Material> mats, List<float> alphas)
    {
        for (int i = 0; i < mats.Count; i++)
        {
            if (mats[i] == null) continue;
            SetMatAlpha(mats[i], (i < alphas.Count) ? alphas[i] : 1f);
        }
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

    // ── Arrows ────────────────────────────────────────────────────────────────

    private void SetAllArrows(bool active)
    {
        if (allArrowParents == null) return;
        foreach (var a in allArrowParents)
            if (a != null) a.SetActive(active);
    }

    // ── Panel show/hide ───────────────────────────────────────────────────────

    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(true);
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null) StartCoroutine(FadeCG(cg, 0f, 1f, panelFadeDuration));
    }

    private void HidePanel(GameObject panel, AudioSource audio)
    {
        if (panel == null || !panel.activeSelf) return;
        if (audio != null && audio.isPlaying) audio.Stop();
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null) StartCoroutine(FadeAndHide(cg, panel, panelFadeDuration));
        else panel.SetActive(false);
    }

    private IEnumerator FadeCG(CanvasGroup cg, float from, float to, float dur)
    {
        cg.alpha = from; float e = 0f;
        while (e < dur) { e += Time.deltaTime; cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(e / dur)); yield return null; }
        cg.alpha = to;
    }

    private IEnumerator FadeAndHide(CanvasGroup cg, GameObject panel, float dur)
    {
        float e = 0f, s = cg.alpha;
        while (e < dur) { e += Time.deltaTime; cg.alpha = Mathf.Lerp(s, 0f, Mathf.Clamp01(e / dur)); yield return null; }
        cg.alpha = 0f; panel.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<Material> GetMats(int i)      { switch(i){ case 0: return _mats_Warm;     case 1: return _mats_Middle;     default: return _mats_Cool;     } }
    private List<float>    GetOrigAlphas(int i){ switch(i){ case 0: return _origAlpha_Warm; case 1: return _origAlpha_Middle; default: return _origAlpha_Cool; } }
    private GameObject     GetPanel(int i)     { switch(i){ case 0: return panel_Warm;      case 1: return panel_Middle;      default: return panel_Cool;      } }
    private AudioSource    GetAudio(int i)     { switch(i){ case 0: return audio_Warm;      case 1: return audio_Middle;      default: return audio_Cool;      } }
    private Image          GetHighlight(int i) { switch(i){ case 0: return glowHighlight_Warm; case 1: return glowHighlight_Middle; default: return glowHighlight_Cool; } }

    void OnDisable()
    {
        StopAllCoroutines();
        SetHighlightAlpha(glowHighlight_Warm,   0f);
        SetHighlightAlpha(glowHighlight_Middle, 0f);
        SetHighlightAlpha(glowHighlight_Cool,   0f);
        RestoreAllOriginalAlphas();
        SetAllArrows(true);
        _selectedIndex = -1;
    }
}
