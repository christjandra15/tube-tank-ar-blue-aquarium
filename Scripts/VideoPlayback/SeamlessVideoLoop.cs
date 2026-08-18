// ============================================================================
// SeamlessVideoLoop.cs
// Tube Tank AR — Blue Aquarium Project
// Aquaria KLCC × Asia Pacific University (APU)
// Author: Christian Nathaniel Tjandra
//
// Dual-RenderTexture video crossfade with runtime aspect-adaptive quad
// scaling. Loops a 4K background video with no visible seam, and rescales
// the display quad to fill (or fit) the screen based on the live camera
// aspect ratio — so one build renders correctly across different tablet
// resolutions (Galaxy Tab S8 Ultra vs. S10 Plus) without per-device builds.
//
// Shared for portfolio and educational reference — see repository README
// for full context, architecture notes, and license terms.
// ============================================================================

using UnityEngine;
using UnityEngine.Video;

public class SeamlessVideoLoop : MonoBehaviour
{
    public VideoClip     clip;
    public RenderTexture rtA;
    public RenderTexture rtB;
    public Renderer      videoQuad;
    public float         crossfadeTime = 3f;
    public float         cueBeforeEnd  = 4f;

    [Header("Quad Scale")]
    [Tooltip("Auto scale to fill screen while preserving video aspect ratio")]
    public bool autoScaleToScreen = true;

    [Tooltip("Manual scale — only used if autoScaleToScreen is false")]
    public Vector3 lockedScale = new Vector3(38.4f, 21.6f, 1f);

    [Tooltip("Video aspect ratio — 16:9 = 1.777, 16:10 = 1.6")]
    public float videoAspectRatio = 1.777f;

    public enum ScaleMode { Fill, Fit }
    [Tooltip("Fill = cover full screen (may crop). Fit = show full video (may have bars)")]
    public ScaleMode scaleMode = ScaleMode.Fill;

    [Header("Camera Reference")]
    [Tooltip("Camera rendering the video quad")]
    public Camera renderCamera;

    VideoPlayer   _vpA, _vpB;
    VideoPlayer   _active, _standby;
    RenderTexture _activeRT, _standbyRT;
    bool          _fading;
    float         _fadeTimer;
    bool          _initialized;

    void Start()
    {
        foreach (var vp in GetComponents<VideoPlayer>())
            Destroy(vp);

        Invoke(nameof(InitPlayers), 0.1f);
    }

    void InitPlayers()
    {
        _vpA = CreatePlayer(rtA);
        _vpB = CreatePlayer(rtB);
        _active    = _vpA;  _activeRT  = rtA;
        _standby   = _vpB;  _standbyRT = rtB;
        _active.Play();
        videoQuad.material.SetTexture("_BaseMap", _activeRT);
        ApplyQuadScale();
        _initialized = true;
    }

    private void ApplyQuadScale()
    {
        if (!autoScaleToScreen)
        {
            transform.localScale = lockedScale;
            return;
        }

        Camera cam = renderCamera != null ? renderCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[SeamlessVideoLoop] No camera found — using locked scale");
            transform.localScale = lockedScale;
            return;
        }

        float dist        = Mathf.Abs(transform.position.z - cam.transform.position.z);
        float frustumH    = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumW    = frustumH * cam.aspect;
        float screenAspect = cam.aspect;

        float quadW, quadH;

        if (scaleMode == ScaleMode.Fill)
        {
            if (screenAspect > videoAspectRatio)
            {
                quadW = frustumW;
                quadH = quadW / videoAspectRatio;
            }
            else
            {
                quadH = frustumH;
                quadW = quadH * videoAspectRatio;
            }
        }
        else
        {
            if (screenAspect > videoAspectRatio)
            {
                quadH = frustumH;
                quadW = quadH * videoAspectRatio;
            }
            else
            {
                quadW = frustumW;
                quadH = quadW / videoAspectRatio;
            }
        }

        transform.localScale = new Vector3(quadW * 1.01f, quadH * 1.01f, 1f);

        Debug.Log("[SeamlessVideoLoop] Scale: " + transform.localScale
            + " | Screen: " + Screen.width + "x" + Screen.height
            + " | Screen aspect: " + screenAspect.ToString("F3")
            + " | Video aspect: " + videoAspectRatio.ToString("F3"));
    }

    VideoPlayer CreatePlayer(RenderTexture rt)
    {
        var vp             = gameObject.AddComponent<VideoPlayer>();
        vp.clip            = clip;
        vp.renderMode      = VideoRenderMode.RenderTexture;
        vp.targetTexture   = rt;
        vp.isLooping       = false;
        vp.skipOnDrop      = true;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.Prepare();
        return vp;
    }

    void Update()
    {
        if (!_initialized) return;
        if (_active == null || !_active.isPlaying) return;

        double remaining = _active.clip.length - _active.time;

        if (!_fading && remaining <= cueBeforeEnd)
        {
            _standby.time = 0;
            _standby.Play();
            _fading    = true;
            _fadeTimer = 0;
        }

        if (_fading)
        {
            _fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeTimer / crossfadeTime);

            videoQuad.material.SetTexture("_BaseMap",     _activeRT);
            videoQuad.material.SetTexture("_EmissionMap", _standbyRT);
            videoQuad.material.SetFloat("_EmissionScale", t);

            if (t >= 1f)
            {
                (_active,   _standby)   = (_standby,   _active);
                (_activeRT, _standbyRT) = (_standbyRT, _activeRT);
                videoQuad.material.SetTexture("_BaseMap", _activeRT);
                _standby.Stop();
                _fading    = false;
                _fadeTimer = 0;
            }
        }
    }

    void OnDisable()
    {
        if (_vpA != null) _vpA.Stop();
        if (_vpB != null) _vpB.Stop();
    }
}
