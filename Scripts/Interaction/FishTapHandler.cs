// ============================================================================
// FishTapHandler.cs
// Tube Tank AR — Blue Aquarium Project
// Aquaria KLCC × Asia Pacific University (APU)
// Author: Christian Nathaniel Tjandra
//
// Per-fish tap target. Shows that fish's info panel and highlights the
// temperature layer(s) it inhabits. Resolves its FishLayerHighlighter
// reference via an inspector slot with a static-instance fallback, so it
// degrades gracefully even if a scene reference isn't wired.
//
// Shared for portfolio and educational reference — see repository README
// for full context, architecture notes, and license terms.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FishTapHandler : MonoBehaviour
{
    [Header("--- FISH IDENTITY ---")]
    public string fishName = "Fish Name";

    [Header("--- TEMPERATURE LAYER ---")]
    [Tooltip("0=Warmest 1=Warm 2=Cool")]
    public int[] inhabitedLayers = new int[]{ 0 };

    [Tooltip("Drag CONTENT_C GameObject that has FishLayerHighlighter")]
    public FishLayerHighlighter layerHighlighter;

    [Header("--- THIS FISH PANEL ---")]
    [Tooltip("Drag the fish info panel — content inside it stays as designed in hierarchy")]
    public GameObject fishInfoPanel;
    public GameObject closeButton;

    [Header("--- OTHER FISH PANELS ---")]
    public GameObject otherPanel_A;
    public GameObject otherPanel_B;

    [Header("--- FADE SETTINGS ---")]
    public float fadeDuration = 0.4f;

    // ── Called when fish sprite is tapped ─────────────────────────────────────

    public void OnFishTapped()
    {
        if (ButtonClickSound.Instance != null)
            ButtonClickSound.Instance.PlayClickSound();

        // Hide other panels first
        HideOtherPanels();

        // Show close button
        if (closeButton != null) closeButton.SetActive(true);

        // Show THIS panel — content stays exactly as designed in hierarchy
        // NO content overwriting — text, images, descriptions are set in the panel itself
        if (fishInfoPanel != null)
        {
            fishInfoPanel.SetActive(true);
            StartCoroutine(FadeIn(fishInfoPanel));
        }

        // Highlight matching temperature layer
        FishLayerHighlighter highlighter = layerHighlighter ?? FishLayerHighlighter.Instance;
        if (highlighter != null)
            highlighter.HighlightLayers(inhabitedLayers);
        else
            Debug.LogWarning("[FishTapHandler] No FishLayerHighlighter found: " + fishName);

        Debug.Log("[FishTapHandler] Tapped: " + fishName);
    }

    // ── Called by close button ────────────────────────────────────────────────

    public void OnClosePanel()
    {
        if (ButtonClickSound.Instance != null)
            ButtonClickSound.Instance.PlayClickSound();

        if (fishInfoPanel != null)
            fishInfoPanel.SetActive(false);

        FishLayerHighlighter highlighter = layerHighlighter ?? FishLayerHighlighter.Instance;
        if (highlighter != null)
            highlighter.ResetAll();

        Debug.Log("[FishTapHandler] Closed: " + fishName);
    }

    private void HideOtherPanels()
    {
        if (otherPanel_A != null) otherPanel_A.SetActive(false);
        if (otherPanel_B != null) otherPanel_B.SetActive(false);
    }

    private IEnumerator FadeIn(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) yield break;
        cg.alpha  = 0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }
}
