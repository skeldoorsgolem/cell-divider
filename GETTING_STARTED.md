# Getting Started — Cell Divider

From zero to a running game in Unity. No art assets needed — everything is procedurally generated.

---

## 1. Clone the repo

```
git clone https://github.com/skeldoorsgolem/cell-divider.git
cd cell-divider
```

---

## 2. Open in Unity

- Open Unity Hub
- Click *Open > Add project from disk*
- Select the `cell-divider` folder
- Unity version: 2022 LTS or newer (2023 / Unity 6 are fine)
- Let it import — first open takes a minute

---

## 3. Install TextMeshPro

When Unity prompts "Import TMP Essentials" — click it. If it doesn't prompt:

*Window > TextMeshPro > Import TMP Essential Resources*

---

## 4. Create the Scene

*File > New Scene > Basic (Built-in)* then save as `Assets/Scenes/Main.unity`

### 4a. GameManager

- Create empty GameObject, name it `GameManager`
- Add components: `GameManager`, `EventBus`, `SaveSystem`
- Create a child GameObject `TechTreeManager`, add `TechTreeManager` component
- Create a child GameObject `ShopManager`, add `ShopManager` component

### 4b. Canvas

- *GameObject > UI > Canvas*
- Set Canvas Scaler: Scale With Screen Size, reference 1920×1080

Inside the Canvas, create this hierarchy (all UI GameObjects unless noted):

```
Canvas
├── HUD                          (empty RectTransform, stretch to full canvas)
│   ├── CellCountLabel           (TextMeshProUGUI)
│   ├── CPSLabel                 (TextMeshProUGUI)
│   └── CPCLabel                 (TextMeshProUGUI)
│
├── CellButton                   (Button)
│   └── (ClickManager.cs + SquishyCell.cs on this GO)
│
├── FloatingTextSpawner          (empty GO with FloatingTextSpawner.cs)
│
├── TabBar
│   ├── ShopTabButton            (Button)
│   └── TechTabButton            (Button)
│
├── ShopPanel                    (ScrollView)
│   └── Viewport > Content       (assign to ShopManager.contentParent)
│
└── TechTreePanel                (ScrollView)
    └── Viewport > Content       (assign to TechTreeManager.contentParent)
```

Add `HUDController.cs` to the HUD object and wire up the three labels.

Add `PanelToggle.cs` to each tab button and wire up ShopPanel / TechTreePanel.

### 4c. AudioManager

- Create empty GameObject `AudioManager`
- Add `AudioSource` component (leave clip empty)
- Add `AudioSynth.cs` and `AudioHooks.cs`
- Wire the AudioSource to AudioSynth in the inspector

---

## 5. Create Prefabs

In `Assets/Prefabs/` (create folder if needed):

### TechNodePrefab
- Empty RectTransform (200×200)
- Add `TechNode.cs`
- Add `ProceduralNodeSprite.cs`
- Child: Image (for the procedural sprite)
- Child: TMP label (node name)
- Child: TMP label (cost)
- Child: Button component on root

### SpringLinePrefab
- Empty GameObject (not a RectTransform — lives in world space)
- Add `LineRenderer` (set width 0.05, material Default-Line or Sprites-Default)
- Add `SpringLine.cs`

### ShopItemPrefab
- RectTransform (full width, height 80)
- Add `ShopItemUI.cs`
- Children: name label, cost label, count label, buy Button

### FloatingTextPrefab
- RectTransform
- Add `FloatingText.cs`
- Child: TMP label
- Child: CanvasGroup

---

## 6. Create ScriptableObjects

### Tech Nodes
Right-click in Project > *Create > CellGame > TechNode*

Create these 6 to start:

| Asset name | Display name | Cost | CPC bonus | CPS bonus | Prerequisites |
|---|---|---|---|---|---|
| `node_mitosis` | Mitosis Boost | 10 | +0.5 | — | — |
| `node_membrane` | Membrane | 50 | +1.0 | — | node_mitosis |
| `node_atp` | ATP Synthesis | 50 | — | +0.5 | node_mitosis |
| `node_nuclear` | Nuclear Division | 200 | ×2.0 mult | — | node_membrane |
| `node_mito1` | Mitochondria | 200 | — | +2.0 | node_atp |
| `node_colony` | Colony Formation | 1000 | — | +10.0 | node_mito1 |

Drag all 6 into `TechTreeManager.nodes` array in the inspector.

### Shop Items
Right-click > *Create > CellGame > ShopItem*

| Asset name | Display name | Base cost | CPS bonus |
|---|---|---|---|
| `shop_ribosome` | Ribosome | 15 | +0.1 |
| `shop_cellwall` | Cell Wall | 100 | +0.5 |
| `shop_nucleus` | Nucleus | 500 | +2.0 |
| `shop_stem` | Stem Cell Factory | 5000 | +20.0 |

Drag all into `ShopManager.items` array.

---

## 7. Wire up prefabs

On `TechTreeManager` inspector:
- `nodePrefab` → TechNodePrefab
- `linePrefab` → SpringLinePrefab
- `contentParent` → TechTreePanel's Content transform

On `ShopManager` inspector:
- `itemPrefab` → ShopItemPrefab
- `contentParent` → ShopPanel's Content transform

On `HUDController`:
- Wire `CellCountLabel`, `CPSLabel`, `CPCLabel`

On `ClickManager`:
- Wire `CellButton`

On `FloatingTextSpawner`:
- `prefab` → FloatingTextPrefab
- `canvas` → Canvas

---

## 8. Hit Play

The game should start with:
- A procedurally drawn cell button (green blob with nucleus)
- Click it to divide cells
- HUD shows cell count, CPS, CPC
- Shop panel shows 4 items
- Tech tree panel shows 6 nodes connected by springy lines

If something's missing, check the Console for errors — usually a missing prefab reference or unassigned inspector field.

---

## Tips

- *Tech tree nodes look the same?* — Each node gets a unique hash from its `id` string, driving the blob shape and colour. Make sure each TechNodeData has a unique `id` field.
- *No sound?* — Make sure AudioSource is on the same GO as AudioSynth, and AudioHooks is subscribed (it hooks into EventBus.OnCellCountChanged etc on Start).
- *Save getting in the way of testing?* — Call `SaveSystem.DeleteAll()` from a button or use `PlayerPrefs.DeleteAll()` in the console.
- *Spring lines too wobbly / stiff?* — Tune `stiffness` (default 80) and `damping` (default 5) on each SpringLine, or change the defaults in `SpringLine.cs`.
