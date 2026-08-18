// ============================================================================
// ContentAnimator.cs
// Tube Tank AR — Blue Aquarium Project
// Aquaria KLCC × Asia Pacific University (APU)
// Author: Christian Nathaniel Tjandra
//
// Generalized fade-in/fade-out for any mix of content under a GameObject —
// 3D mesh materials, sprite renderers, CanvasGroups, and UI Graphics — driven
// from one call. The excludedRenderers list lets a more specific system (e.g.
// TemperatureLayerManager) own a renderer's alpha independently, so it isn't
// overwritten by the next general content transition.
//
// Shared for portfolio and educational reference — see repository README
// for full context, architecture notes, and license terms.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContentAnimator : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInDuration  = 0.5f;
    public float fadeOutDuration = 0.4f;

    [Header("--- EXCLUDED RENDERERS ---")]
    [Tooltip("Renderers in this list will NOT be touched by fade in/out. Use for temperature layers so TemperatureLayerManager can control them independently.")]
    public Renderer[] excludedRenderers;

    // Collected targets
    private List<Material>       _materials    = new List<Material>();
    private List<SpriteRenderer> _sprites      = new List<SpriteRenderer>();
    private List<CanvasGroup>    _canvasGroups = new List<CanvasGroup>();
    private List<Graphic>        _graphics     = new List<Graphic>();

    private Coroutine _currentFade;

    // ── Public API ────────────────────────────────────────────────────────────

    public void FadeIn()
    {
        StopFade();
        CollectAll();
        gameObject.SetActive(true);
        SetAlphaAll(0f);
        _currentFade = StartCoroutine(FadeCoroutine(0f, 1f, fadeInDuration, null));
    }

    public void FadeOut(System.Action onComplete = null)
    {
        if (!gameObject.activeSelf) { onComplete?.Invoke(); return; }
        StopFade();
        CollectAll();
        SetAlphaAll(1f);
        _currentFade = StartCoroutine(FadeOutCoroutine(fadeOutDuration, onComplete));
    }

    public void ShowImmediate()
    {
        StopFade();
        CollectAll();
        gameObject.SetActive(true);
        SetAlphaAll(1f);
    }

    public void HideImmediate()
    {
        StopFade();
        CollectAll();
        SetAlphaAll(0f);
        gameObject.SetActive(false);
    }

    // ── Collect all fadeable targets — skipping excluded renderers ────────────

    private void CollectAll()
    {
        _materials.Clear();
        _sprites.Clear();
        _canvasGroups.Clear();
        _graphics.Clear();

        // Build a HashSet of excluded renderers for fast lookup
        HashSet<Renderer> excluded = new HashSet<Renderer>();
        if (excludedRenderers != null)
            foreach (var r in excludedRenderers)
                if (r != null) excluded.Add(r);

        // 3D Renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // Skip excluded renderers
            if (excluded.Contains(r)) continue;

            if (r is SpriteRenderer) continue;

            foreach (var mat in r.materials)
                if (mat.HasProperty("_BaseColor") || mat.HasProperty("_Color"))
                    _materials.Add(mat);
        }

        // 2D Sprite Renderers
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in sprites)
            _sprites.Add(s);

        // UI CanvasGroups
        CanvasGroup rootCG = GetComponent<CanvasGroup>();
        if (rootCG != null)
        {
            _canvasGroups.Add(rootCG);
        }
        else
        {
            foreach (Transform child in transform)
            {
                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg != null)
                    _canvasGroups.Add(cg);
            }
        }

        // UI Graphics
        if (_canvasGroups.Count == 0)
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
                _graphics.Add(g);
        }
    }

    // ── Set alpha on all collected targets ────────────────────────────────────

    private void SetAlphaAll(float alpha)
    {
        foreach (var mat in _materials)
        {
            if (mat == null) continue;
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = alpha;
                mat.SetColor("_Color", c);
            }
        }

        foreach (var s in _sprites)
        {
            if (s == null) continue;
            Color c = s.color;
            c.a = alpha;
            s.color = c;
        }

        foreach (var cg in _canvasGroups)
        {
            if (cg == null) continue;
            cg.alpha = alpha;
        }

        foreach (var g in _graphics)
        {
            if (g == null) continue;
            Color c = g.color;
            c.a = alpha;
            g.color = c;
        }
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator FadeCoroutine(float from, float to, float duration, System.Action onComplete)
    {
        SetAlphaAll(from);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed    += Time.deltaTime;
            SetAlphaAll(Mathf.Clamp01(Mathf.Lerp(from, to, elapsed / duration)));
            yield return null;
        }
        SetAlphaAll(to);
        _currentFade = null;
        onComplete?.Invoke();
    }

    private IEnumerator FadeOutCoroutine(float duration, System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed    += Time.deltaTime;
            SetAlphaAll(Mathf.Clamp01(Mathf.Lerp(1f, 0f, elapsed / duration)));
            yield return null;
        }
        SetAlphaAll(0f);
        gameObject.SetActive(false);
        _currentFade = null;
        onComplete?.Invoke();
    }

    private void StopFade()
    {
        if (_currentFade != null)
        {
            StopCoroutine(_currentFade);
            _currentFade = null;
        }
    }
}
