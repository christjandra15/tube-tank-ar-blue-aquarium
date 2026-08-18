// ============================================================================
// AppManager.cs
// Tube Tank AR — Blue Aquarium Project
// Aquaria KLCC × Asia Pacific University (APU)
// Author: Christian Nathaniel Tjandra
//
// Top-level state machine for the exhibit. Owns which content block
// (Main Menu / Water Flow / Temperature / Fish Info) is active, sequences
// panel and 3D-content fades between states via ContentAnimator, and
// coordinates the back-button flow so nothing pops or overlaps mid-transition.
//
// Shared for portfolio and educational reference — see repository README
// for full context, architecture notes, and license terms.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AppManager : MonoBehaviour
{
    public enum AppState { MainMenu, WaterFlow, Temperature, FishInfo }

    [Header("--- 3D CONTENT OBJECTS ---")]
    public GameObject contentA_3D;
    public GameObject contentB_3D;
    public GameObject contentC_3D;

    [Header("--- UI PANELS ---")]
    public GameObject mainMenuPanel;
    public GameObject waterFlowPanel;
    public GameObject temperaturePanel;
    public GameObject fishInfoPanel;

    [Header("--- FISH OVERLAY PANELS ---")]
    public GameObject fishPanel_01;
    public GameObject fishPanel_02;
    public GameObject fishPanel_03;

    [Header("--- TEMPERATURE LAYER PANELS ---")]
    public GameObject tempLayerPanel_01;
    public GameObject tempLayerPanel_02;
    public GameObject tempLayerPanel_03;

    [Header("--- SHARED UI ---")]
    public GameObject backButton;
    public ContentFader contentFader;

    [Header("--- FADE SETTINGS ---")]
    public float panelFadeInDuration  = 0.4f;
    public float panelFadeOutDuration = 0.3f;

    private AppState _currentState;
    private bool _isTransitioning = false;

    void Start()
    {
        ForceHideAll3D();
        ShowMainMenuImmediate();
    }

    // ── Main Menu Button Callbacks ───────────────────────────────────────────

    public void OnWaterFlowButton()
    {
        if (_isTransitioning) return;
        SetState(AppState.WaterFlow);
    }

    public void OnTemperatureButton()
    {
        if (_isTransitioning) return;
        SetState(AppState.Temperature);
    }

    public void OnFishInfoButton()
    {
        if (_isTransitioning) return;
        SetState(AppState.FishInfo);
    }

    // ── Back Button Callback ─────────────────────────────────────────────────

    public void OnBackButton()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        if (ButtonClickSound.Instance != null)
            ButtonClickSound.Instance.PlayClickSound();

        backButton.SetActive(false);

        // Fade out the currently active content panel first
        StartCoroutine(FadeOutCurrentStatePanel(() =>
        {
            // Then fade out 3D content
            FadeOutActiveContent(() =>
            {
                // Then show main menu
                _currentState = AppState.MainMenu;
                HideAllUIPanels();
                mainMenuPanel.SetActive(true);
                StartCoroutine(FadeInCG(
                    mainMenuPanel.GetComponent<CanvasGroup>(),
                    panelFadeInDuration,
                    () => _isTransitioning = false
                ));
            });
        }));
    }

    // ── Fades out whichever content panel is currently showing ───────────────

    private IEnumerator FadeOutCurrentStatePanel(System.Action onComplete)
    {
        GameObject activePanel = null;

        switch (_currentState)
        {
            case AppState.WaterFlow:    activePanel = waterFlowPanel;    break;
            case AppState.Temperature:  activePanel = temperaturePanel;  break;
            case AppState.FishInfo:     activePanel = fishInfoPanel;     break;
        }

        if (activePanel != null && activePanel.activeSelf)
        {
            CanvasGroup cg = activePanel.GetComponent<CanvasGroup>();
            if (cg != null)
                yield return StartCoroutine(FadeOutCG(cg, panelFadeOutDuration));

            activePanel.SetActive(false);
        }

        // Also hide any open layer/fish panels instantly
        ForceHideLayerPanels();
        if (fishPanel_01 != null) fishPanel_01.SetActive(false);
        if (fishPanel_02 != null) fishPanel_02.SetActive(false);
        if (fishPanel_03 != null) fishPanel_03.SetActive(false);

        onComplete?.Invoke();
    }

    // ── Internal State Machine ───────────────────────────────────────────────

    private void SetState(AppState newState)
    {
        _isTransitioning = true;

        // Fade out main menu panel
        StartCoroutine(FadeOutCG(mainMenuPanel.GetComponent<CanvasGroup>(), panelFadeOutDuration, () =>
        {
            mainMenuPanel.SetActive(false);
            HideAllUIPanels();
            backButton.SetActive(true);

            FadeOutActiveContent(() =>
            {
                _currentState = newState;

                switch (newState)
                {
                    case AppState.WaterFlow:
                        FadeInContent(contentA_3D);
                        waterFlowPanel.SetActive(true);
                        StartCoroutine(FadeInCG(
                            waterFlowPanel.GetComponent<CanvasGroup>(),
                            panelFadeInDuration, null
                        ));
                        break;

                    case AppState.Temperature:
                        ForceHideLayerPanels();
                        FadeInContent(contentB_3D);
                        temperaturePanel.SetActive(true);
                        ForceHideLayerPanels();
                        StartCoroutine(FadeInCG(
                            temperaturePanel.GetComponent<CanvasGroup>(),
                            panelFadeInDuration, null
                        ));
                        break;

                    case AppState.FishInfo:
                        FadeInContent(contentC_3D);
                        fishInfoPanel.SetActive(true);
                        StartCoroutine(FadeInCG(
                            fishInfoPanel.GetComponent<CanvasGroup>(),
                            panelFadeInDuration, null
                        ));
                        break;
                }

                _isTransitioning = false;
            });
        }));
    }

    // ── Show main menu on app start ───────────────────────────────────────────

    private void ShowMainMenuImmediate()
    {
        _currentState = AppState.MainMenu;
        HideAllUIPanels();
        backButton.SetActive(false);
        mainMenuPanel.SetActive(true);
        StartCoroutine(FadeInCG(
            mainMenuPanel.GetComponent<CanvasGroup>(),
            panelFadeInDuration, null
        ));
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator FadeInCG(CanvasGroup cg, float duration, System.Action onComplete)
    {
        if (cg == null) { onComplete?.Invoke(); yield break; }

        cg.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        cg.alpha = 1f;
        onComplete?.Invoke();
    }

    private IEnumerator FadeOutCG(CanvasGroup cg, float duration, System.Action onComplete = null)
    {
        if (cg == null) { onComplete?.Invoke(); yield break; }

        float elapsed    = 0f;
        float startAlpha = cg.alpha;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        cg.alpha = 0f;
        onComplete?.Invoke();
    }

    // ── 3D Content Helpers ────────────────────────────────────────────────────

    private void FadeInContent(GameObject content)
    {
        if (content == null) return;
        ContentAnimator anim = content.GetComponent<ContentAnimator>();
        if (anim != null) anim.FadeIn();
        else content.SetActive(true);
    }

    private void FadeOutActiveContent(System.Action onComplete)
    {
        GameObject active = GetActiveContent();
        if (active == null) { onComplete?.Invoke(); return; }

        ContentAnimator anim = active.GetComponent<ContentAnimator>();
        if (anim != null) anim.FadeOut(onComplete);
        else { active.SetActive(false); onComplete?.Invoke(); }
    }

    private GameObject GetActiveContent()
    {
        if (contentA_3D != null && contentA_3D.activeSelf) return contentA_3D;
        if (contentB_3D != null && contentB_3D.activeSelf) return contentB_3D;
        if (contentC_3D != null && contentC_3D.activeSelf) return contentC_3D;
        return null;
    }

    // ── UI Helpers ────────────────────────────────────────────────────────────

    private void ForceHideLayerPanels()
    {
        if (tempLayerPanel_01 != null) tempLayerPanel_01.SetActive(false);
        if (tempLayerPanel_02 != null) tempLayerPanel_02.SetActive(false);
        if (tempLayerPanel_03 != null) tempLayerPanel_03.SetActive(false);
    }

    private void ForceHideAll3D()
    {
        ForceHide(contentA_3D);
        ForceHide(contentB_3D);
        ForceHide(contentC_3D);
    }

    private void ForceHide(GameObject obj)
    {
        if (obj == null) return;
        ContentAnimator anim = obj.GetComponent<ContentAnimator>();
        if (anim != null) anim.HideImmediate();
        else obj.SetActive(false);
    }

    private void HideAllUIPanels()
    {
        mainMenuPanel.SetActive(false);
        waterFlowPanel.SetActive(false);
        temperaturePanel.SetActive(false);
        fishInfoPanel.SetActive(false);

        if (fishPanel_01 != null) fishPanel_01.SetActive(false);
        if (fishPanel_02 != null) fishPanel_02.SetActive(false);
        if (fishPanel_03 != null) fishPanel_03.SetActive(false);

        ForceHideLayerPanels();
    }
}
