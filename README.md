# Cell Divider — Biological Incremental Game

A Cookie Clicker-style idle game with a biological theme, built in Unity 2D.

## Concept

Click to divide cells. Unlock upgrades. Watch your cell colony grow autonomously.

The tech tree is a **Fruchterman-Reingold force-directed graph** — nodes repel each other and edges attract, producing an organic branching layout. Connections are **springy ropes** (manual spring-mass simulation, no Rigidbody) that jiggle when nodes are unlocked.

---

## Requirements

- Unity 2022 LTS or newer (2023, Unity 6 also fine)
- TextMeshPro package (install via Package Manager if not already included)

---

## Project Structure

```
Assets/
  Scripts/
    Core/
      GameManager.cs       — singleton; cell count, CPC, CPS, auto-divide, save/load
      EventBus.cs          — static events for decoupled system communication
      SaveSystem.cs        — PlayerPrefs JSON save/load (double-safe)
    Click/
      ClickManager.cs      — main divide button handler
      ClickFeedback.cs     — squish/glow animation on click
      FloatingTextSpawner.cs — pooled "+N cells" text factory
      FloatingText.cs      — single floating text behaviour
    TechTree/
      TechNodeData.cs      — ScriptableObject: node definition
      TechTreeManager.cs   — FR layout, graph instantiation, SpringLines
      TechNode.cs          — per-node runtime state + click to unlock
      SpringLine.cs        — spring-simulated LineRenderer connection
    Shop/
      ShopItemData.cs      — ScriptableObject: item definition
      ShopManager.cs       — purchase logic, cost scaling
      ShopItemUI.cs        — per-item display row
    UI/
      HUDController.cs     — cell count / CPS / CPC labels
      PanelToggle.cs       — tab switching (shop / tech tree)
      GameUtils.cs         — number formatting (K, M, B, T...)
```

---

## Scene Setup

### 1. GameManager
- Create empty GO `GameManager`
- Add `GameManager.cs`
- Add `TechTreeManager.cs`, `ShopManager.cs` as siblings or children

### 2. Canvas (Screen Space - Overlay)

```
Canvas
  HUD                   ← HUDController.cs
    CellCountLabel      ← TMP label
    CPSLabel
    CPCLabel

  CellButton            ← Button + ClickManager.cs + ClickFeedback.cs
    CellSprite          ← Image (your cell graphic)
    ClickParticles      ← ParticleSystem
  FloatingTextPool      ← FloatingTextSpawner.cs

  TabBar
    ShopTabButton       ← PanelToggle.cs
    TechTreeTabButton   ← PanelToggle.cs

  ShopPanel             ← ScrollView
    ShopContent         ← Vertical Layout Group (ShopManager adds rows here)

  TechTreePanel         ← ScrollView
    TechContent         ← TechTreeManager adds nodes + lines here
```

### 3. Prefabs needed
- `TechNodePrefab` — RectTransform + `TechNode.cs` + child Image + TMP labels + Button
- `SpringLinePrefab` — GameObject + `SpringLine.cs` + `LineRenderer`
- `ShopItemPrefab` — RectTransform + `ShopItemUI.cs` + child Images + TMP labels + Button
- `FloatingTextPrefab` — RectTransform + `FloatingText.cs` + TMP label + CanvasGroup

### 4. ScriptableObjects

Create tech nodes: `Assets > Create > CellGame > TechNode`

Example starter tree:
| id | Name | Cost | Effect | Prerequisites |
|----|------|------|--------|--------------|
| mitosis_boost | Mitosis Boost | 10 | +0.5 CPC | — |
| membrane | Membrane Strength | 50 | +1.0 CPC | mitosis_boost |
| atp | ATP Synthesis | 50 | +0.5 CPS | mitosis_boost |
| nuclear | Nuclear Division | 200 | x2.0 CPC | membrane |
| mito1 | Mitochondria I | 200 | +2.0 CPS | atp |
| colony | Colony Formation | 1000 | +10.0 CPS | mito1 |

Create shop items: `Assets > Create > CellGame > ShopItem`

Example items:
| Name | Base Cost | Scaling | Effect |
|------|-----------|---------|--------|
| Ribosome | 15 | 1.15 | +0.1 CPS |
| Cell Wall | 100 | 1.15 | +0.5 CPC |
| Nucleus | 500 | 1.15 | +2.0 CPS |
| Stem Cell Factory | 5000 | 1.15 | +20.0 CPS |

---

## Tech Tree — Fruchterman-Reingold

`TechTreeManager` runs the FR force-directed algorithm at startup to position nodes:

- *Repulsive force* between all node pairs — keeps nodes spread out
- *Attractive force* along each prerequisite edge — pulls connected nodes together
- Temperature cools each iteration — layout converges to stable positions

Parameters (tune in inspector on `TechTreeManager`):
- `graphWidth / graphHeight` — bounding box for the layout
- `frIterations` — more = more refined layout, but slower startup (150 is fine)
- `frTemperature` — initial max displacement (200 is a good start)
- `frCooling` — temperature multiplier per iteration (0.95 = 5% cooldown)

---

## Spring Lines — How it works

`SpringLine.cs` runs a manual spring-mass simulation each frame:

1. Pin endpoints to their nodes' world positions
2. Compute rest positions (straight line between endpoints)
3. For each intermediate mass: apply spring force toward rest + damping
4. Integrate velocity + position
5. Update `LineRenderer`

Calling `Excite()` on unlock adds a perpendicular impulse → the line wobbles, then settles.

Tune `stiffness` (80) and `damping` (5) in the inspector for different feels.

---

## Save System

Saves to `PlayerPrefs` as JSON every 30 seconds and on quit.
To reset: call `SaveSystem.DeleteAll()` from the inspector or add a debug button.

> Note: `double` values are stored as strings in JSON (JsonUtility doesn't support double). This is handled transparently in `SaveSystem.cs`.
