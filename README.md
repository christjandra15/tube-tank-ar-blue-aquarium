# Tube Tank AR — Lake Ecosystem Exhibit

**Christian Nathaniel Tjandra** · Senior Developer, Blue Aquarium Project
([GitHub](https://github.com/christjandra15))

An interactive AR-schematic exhibit built for the **Blue Aquarium Project**, a
STEM Lab initiative developed in collaboration between **Aquaria KLCC** and
**Asia Pacific University (APU)**. Deployed on tablet-based kiosks
(Samsung Galaxy Tab S8 Ultra / S10 Plus, Android) mounted beside a live tube
tank representing a freshwater lake ecosystem, the app overlays a 4K video
schematic on top of the real tank, letting visitors tap through the tank's
water-flow system, thermal stratification (epilimnion / metalimnion /
hypolimnion), and the fish species native to each layer.

**About the source project:** this is a production Unity project built for a
real, permanently installed museum exhibit. The full project, scene files,
and installable build are not published here per the client agreement — for
photos and video of the finished exhibit, [see the case study on my
portfolio site](#) *(link here)*. What's in this repo are standalone,
cleaned copies of the runtime scripts I personally authored for the
exhibit, shared to demonstrate the underlying systems and engineering
decisions.

---

## What's here

### Video Playback
- **`SeamlessVideoLoop.cs`** — drives the tank's 4K background video using a
  dual-`RenderTexture` crossfade (two `VideoPlayer` outputs swapped near the
  clip's end) so the loop has no visible seam or frame drop. Recomputes the
  display quad's scale at runtime from the live camera aspect and screen
  dimensions, so the same build fills correctly across different tablet
  aspect ratios (Tab S8 Ultra vs. S10 Plus) without a hardcoded resolution.

### App / Content Flow
- **`AppManager.cs`** — top-level state controller; owns which content
  block (intro / Water Flow / Temperature / Fishes) is active and drives
  transitions between them.
- **`ContentAnimator.cs`** — shared fade system across `CanvasGroup`s and
  instanced materials. Exposes an `excludedRenderers` list so a specific
  subsystem (e.g. per-layer highlighting) can own its own alpha without
  fighting the top-level content fade.

### Interaction
- **`InputHandler.cs`** — single entry point for all tap input. Runs a
  UI-hit check first (so canvas buttons don't fall through to the 3D
  targets behind them), then resolves against 2D fish sprite colliders and
  3D temperature-layer colliders depending on which content block is
  active. Built on Unity 6's `EnhancedTouchSupport` / `Touch.activeTouches`
  rather than the legacy `Input.GetTouch()`, which is unreliable under
  Unity 6's GameActivity backend.
- **`FishTapHandler.cs`** — per-fish tap target; resolves its highlighter
  reference via a static `Instance` with an inspector-slot fallback, so it
  degrades gracefully if a reference isn't wired in the scene.

### Layer Systems
- **`TemperatureLayerManager.cs`** — owns the three temperature-layer
  cylinders (warm / transition / cool), their individual alpha state, and
  the content mapping (per-layer description text) shown on tap.
- **`FishLayerHighlighter.cs`** — highlights the fish species associated
  with whichever temperature layer is currently selected; exposes a static
  `Instance` consumed by `FishTapHandler`.

---

## Technical highlights

A few decisions worth calling out for anyone reading the code:

- **Fixed-perspective "simulated AR" instead of live tracking.** The
  tablet is physically dock-mounted at a fixed angle to the tank rather
  than handheld, so instead of running live SLAM/AR Foundation tracking,
  the illusion is produced by matching a pre-rendered 3D overlay video to
  that exact fixed camera perspective. For an always-on kiosk this
  sidesteps tracking drift and re-localization failure entirely — there's
  nothing to lose tracking of.
- **Aspect-adaptive video framing.** Rather than shipping per-device
  builds, `SeamlessVideoLoop` reads the actual screen/camera aspect at
  launch and rescales the video quad accordingly, so one APK targets both
  the S8 Ultra and S10 Plus correctly.
- **UI-first hit resolution.** `InputHandler` checks
  `EventSystem.IsPointerOverGameObject` before doing any 2D/3D raycast
  work, which sounds obvious but is an easy thing to get wrong once a
  scene mixes screen-space UI, 2D sprite colliders, and 3D mesh colliders
  all responding to the same tap.
- **Alpha ownership without conflicts.** Multiple systems (top-level
  content fades, per-layer highlighting) all touch renderer alpha.
  `ContentAnimator`'s `excludedRenderers` mechanism means a more specific
  system can claim a renderer without the general-purpose fader stomping
  it on the next transition.
- **UI-driven highlighting instead of material glow.** Rather than trying
  to drive an emissive/glow effect through material properties on
  non-uniformly scaled cylinder meshes, `TemperatureLayerManager`
  overlays a soft UI `Image` on top of each layer and fades its alpha —
  visually equivalent, far more predictable across mesh scales.
- **Fish that span multiple layers.** `FishLayerHighlighter.HighlightLayers`
  takes an `int[]` rather than a single index, so a fish that ranges across
  two temperature zones correctly highlights both rather than forcing a
  single-layer simplification.

---

## Setup

Each script is a standalone `MonoBehaviour` written against **Unity
6000.3.x**, **URP 17.3.0**, and the **Input System package (new)**. Drop
the relevant scripts into a Unity 6 project with URP and the Input System
installed, attach the components as described in each file's header
comment, and wire up the inspector references (video clip, render
textures, layer GameObjects) to match your own scene. No scene files are
included — these are meant to show how the systems are built, not to run
out of the box.

**Note on dependencies:** a couple of scripts reference two small helper
types (`ContentFader`, `ButtonClickSound`) that aren't included in this
excerpt — they're a minor field reference and a UI click-sound singleton
respectively, and not central to the systems being demonstrated here. To
compile these files as-is, stub them out or remove the references.

## License

**All rights reserved.** Shared here for portfolio and educational reference
only. This code was developed for the Blue Aquarium Project, a collaboration
between Aquaria KLCC and Asia Pacific University (APU); it is not licensed
for reuse, redistribution, or commercial use without permission from Aquaria
KLCC and APU.

*(This notice is a placeholder pending formal confirmation from APU/Aquaria
on IP terms — update before publishing if their agreement specifies
different language.)*

---

**Christian Nathaniel Tjandra** — Senior Developer, Blue Aquarium Project.
Developed for Aquaria KLCC in collaboration with Asia Pacific University
(APU).
