// ============================================================================
// InputHandler.cs
// Tube Tank AR — Blue Aquarium Project
// Aquaria KLCC × Asia Pacific University (APU)
// Author: Christian Nathaniel Tjandra
//
// Single entry point for all tap input on the kiosk. Runs a UI hit-test
// first so canvas buttons never fall through to the world-space targets
// behind them, then resolves against 2D fish-sprite colliders and 3D
// temperature-layer colliders as appropriate. Built on Unity 6's
// EnhancedTouchSupport / Touch.activeTouches rather than the legacy
// Input.GetTouch(), which is unreliable under Unity 6's GameActivity
// backend on Android.
//
// Shared for portfolio and educational reference — see repository README
// for full context, architecture notes, and license terms.
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// InputHandler — Global touch and mouse input handler
/// Detects taps on:
///   - World Space UI buttons (EventSystem)
///   - 2D fish sprites with FishTapHandler (Physics2D)
///   - 3D temperature layer cylinders via TemperatureLayerManager (Physics3D)
/// </summary>
public class InputHandler : MonoBehaviour
{
    [Header("--- CAMERA ---")]
    public Camera arCamera;

    [Header("--- TEMPERATURE LAYER MANAGER ---")]
    [Tooltip("Drag the GameObject that has TemperatureLayerManager on it")]
    public TemperatureLayerManager tempLayerManager;

    [Header("--- LAYER OBJECTS --- So we know which index was hit")]
    [Tooltip("Drag WARM layer cylinder here (index 0)")]
    public GameObject layer_Warm;
    [Tooltip("Drag MIDDLE layer cylinder here (index 1)")]
    public GameObject layer_Middle;
    [Tooltip("Drag COOL layer cylinder here (index 2)")]
    public GameObject layer_Cool;

    [Header("--- DEBUG ---")]
    public bool showDebugLogs = true;

    void OnEnable()  { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Update()
    {
        bool    tapped = false;
        Vector2 tapPos = Vector2.zero;

        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                tapped = true;
                tapPos = touch.screenPosition;
            }
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            tapped = true;
            tapPos = Mouse.current.position.ReadValue();
        }

        if (!tapped || arCamera == null) return;

        // Check UI first
        if (IsTapOnUI(tapPos)) return;

        Ray ray = arCamera.ScreenPointToRay(tapPos);

        if (showDebugLogs)
            Debug.Log("[InputHandler] Ray: " + ray.origin + " → " + ray.direction);

        // ── Physics2D — fish sprites ──────────────────────────────────────────
        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
        if (hit2D.collider != null)
        {
            GameObject hitObj = hit2D.collider.gameObject;
            if (showDebugLogs) Debug.Log("[InputHandler] 2D hit: " + hitObj.name);

            FishTapHandler fish = hitObj.GetComponent<FishTapHandler>()
                               ?? hitObj.GetComponentInParent<FishTapHandler>();
            if (fish != null) { fish.OnFishTapped(); return; }
            return;
        }

        // ── Physics3D — temperature layer cylinders ───────────────────────────
        if (Physics.Raycast(ray, out RaycastHit hit3D))
        {
            GameObject hitObj = hit3D.collider.gameObject;
            if (showDebugLogs) Debug.Log("[InputHandler] 3D hit: " + hitObj.name);

            // Check if hit object is one of the temperature layers
            int layerIndex = GetLayerIndex(hitObj);
            if (layerIndex >= 0 && tempLayerManager != null)
            {
                if (showDebugLogs) Debug.Log("[InputHandler] Layer tapped: index " + layerIndex);
                tempLayerManager.OnLayerTapped(layerIndex);
                return;
            }

            // Fallback — check for FishTapHandler
            FishTapHandler fish = hitObj.GetComponent<FishTapHandler>()
                               ?? hitObj.GetComponentInParent<FishTapHandler>();
            if (fish != null) { fish.OnFishTapped(); return; }

            if (showDebugLogs) Debug.Log("[InputHandler] No handler on: " + hitObj.name);
        }
    }

    // ── Determine which layer index was hit ───────────────────────────────────

    private int GetLayerIndex(GameObject hitObj)
    {
        // Check the hit object and its parents
        Transform t = hitObj.transform;
        while (t != null)
        {
            if (layer_Warm   != null && t.gameObject == layer_Warm)   return 0;
            if (layer_Middle != null && t.gameObject == layer_Middle) return 1;
            if (layer_Cool   != null && t.gameObject == layer_Cool)   return 2;
            t = t.parent;
        }
        return -1;
    }

    // ── UI check ─────────────────────────────────────────────────────────────

    private bool IsTapOnUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        { position = screenPos };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0 && showDebugLogs)
            Debug.Log("[InputHandler] UI hit: " + results[0].gameObject.name);

        return results.Count > 0;
    }
}
