using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility: Tools > Cell Game > Setup Scene
/// Creates all prefabs, ScriptableObjects, and wires up the scene in one click.
/// </summary>
public static class CellGameSetup
{
    private const string PrefabDir = "Assets/Prefabs";
    private const string SODir     = "Assets/Data";

    [MenuItem("Tools/Cell Game/Setup Scene")]
    public static void SetupScene()
    {
        EnsureFolders();

        var prefabs = CreatePrefabs();
        var nodes   = CreateTechNodes();
        var items   = CreateShopItems();

        BuildScene(prefabs, nodes, items);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[CellGameSetup] Done! Press Ctrl+S to save the scene.");
    }

    // ─── Folders ──────────────────────────────────────────────────────────────

    static void EnsureFolders()
    {
        foreach (var rel in new[] { "Prefabs", "Data" })
        {
            var full = Application.dataPath + "/" + rel;
            if (!System.IO.Directory.Exists(full))
                System.IO.Directory.CreateDirectory(full);
        }
        AssetDatabase.Refresh();
    }

    // ─── Prefabs ──────────────────────────────────────────────────────────────

    struct Prefabs
    {
        public GameObject techNode, springLine, shopItem, floatingText;
    }

    static Prefabs CreatePrefabs() => new Prefabs
    {
        techNode     = MakeTechNodePrefab(),
        springLine   = MakeSpringLinePrefab(),
        shopItem     = MakeShopItemPrefab(),
        floatingText = MakeFloatingTextPrefab(),
    };

    // All prefab helpers: build GO hierarchy, wire refs on the GO, THEN save.

    static GameObject MakeTechNodePrefab()
    {
        const string path = PrefabDir + "/TechNodePrefab.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        // Build hierarchy
        var root = new GameObject("TechNodePrefab");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 200);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.15f, 0.25f, 0.15f, 0.9f);
        var btn = root.AddComponent<Button>();
        root.AddComponent<TechNode>();

        var spriteChild = MakeChild(root, "NodeSprite");
        var spriteImg   = spriteChild.AddComponent<Image>();
        spriteImg.raycastTarget = false;
        var spriteRt = spriteChild.GetComponent<RectTransform>();
        spriteRt.anchorMin = spriteRt.anchorMax = new Vector2(0.5f, 0.6f);
        spriteRt.sizeDelta = new Vector2(120, 120);

        var nameGO  = MakeTMPLabel(root.transform, "NameLabel", "Node", 14);
        SetAnchors(nameGO, 0, 0.15f, 1, 0.45f);

        var costGO  = MakeTMPLabel(root.transform, "CostLabel", "0", 12);
        SetAnchors(costGO, 0, 0f, 1, 0.2f);

        // Wire on scene GO before saving
        var tnSO = new SerializedObject(root.GetComponent<TechNode>());
        tnSO.FindProperty("bgImage").objectReferenceValue      = rootImg;
        tnSO.FindProperty("nameLabel").objectReferenceValue    = nameGO.GetComponent<TextMeshProUGUI>();
        tnSO.FindProperty("costLabel").objectReferenceValue    = costGO.GetComponent<TextMeshProUGUI>();
        tnSO.FindProperty("unlockButton").objectReferenceValue = btn;
        tnSO.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject MakeSpringLinePrefab()
    {
        const string path = PrefabDir + "/SpringLinePrefab.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("SpringLinePrefab");
        var lr   = root.AddComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(0.4f, 0.8f, 0.4f, 0.8f);
        root.AddComponent<SpringLine>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject MakeShopItemPrefab()
    {
        const string path = PrefabDir + "/ShopItemPrefab.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("ShopItemPrefab");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);
        root.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.1f, 0.9f);
        var cg = root.AddComponent<CanvasGroup>();
        root.AddComponent<ShopItemUI>();

        var nameGO  = MakeTMPLabel(root.transform, "NameLabel", "Item", 16);
        SetAnchors(nameGO, 0, 0.6f, 0.45f, 1f); SetOffset(nameGO, 10, 0, 0, 0);

        var descGO  = MakeTMPLabel(root.transform, "DescLabel", "", 11);
        SetAnchors(descGO, 0, 0.3f, 0.45f, 0.6f); SetOffset(descGO, 10, 0, 0, 0);
        descGO.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.8f, 0.8f);

        var costGO  = MakeTMPLabel(root.transform, "CostLabel", "0", 13);
        SetAnchors(costGO, 0, 0f, 0.45f, 0.3f); SetOffset(costGO, 10, 0, 0, 0);

        var countGO = MakeTMPLabel(root.transform, "CountLabel", "x0", 13);
        SetAnchors(countGO, 0.45f, 0f, 0.65f, 1f);

        var btnGO  = MakeChild(root, "BuyButton");
        SetAnchors(btnGO, 0.67f, 0.1f, 0.98f, 0.9f);
        btnGO.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.2f);
        var buyBtn = btnGO.AddComponent<Button>();
        var buyLbl = MakeTMPLabel(btnGO.transform, "BuyLabel", "Buy", 14);
        SetAnchors(buyLbl, 0, 0, 1, 1);

        var siuSO = new SerializedObject(root.GetComponent<ShopItemUI>());
        siuSO.FindProperty("nameLabel").objectReferenceValue  = nameGO.GetComponent<TextMeshProUGUI>();
        siuSO.FindProperty("descLabel").objectReferenceValue  = descGO.GetComponent<TextMeshProUGUI>();
        siuSO.FindProperty("costLabel").objectReferenceValue  = costGO.GetComponent<TextMeshProUGUI>();
        siuSO.FindProperty("countLabel").objectReferenceValue = countGO.GetComponent<TextMeshProUGUI>();
        siuSO.FindProperty("buyButton").objectReferenceValue  = buyBtn;
        siuSO.FindProperty("canvasGroup").objectReferenceValue = cg;
        siuSO.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject MakeFloatingTextPrefab()
    {
        const string path = PrefabDir + "/FloatingTextPrefab.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var root = new GameObject("FloatingTextPrefab");
        root.AddComponent<RectTransform>();
        root.AddComponent<CanvasGroup>();
        root.AddComponent<FloatingText>();

        var lbl = MakeTMPLabel(root.transform, "Label", "+1", 18);
        SetAnchors(lbl, 0, 0, 1, 1);

        var ftSO = new SerializedObject(root.GetComponent<FloatingText>());
        ftSO.FindProperty("label").objectReferenceValue = lbl.GetComponent<TextMeshProUGUI>();
        ftSO.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ─── ScriptableObjects ────────────────────────────────────────────────────

    static TechNodeData[] CreateTechNodes() => new[]
    {
        MakeNode("node_mitosis",  "Mitosis Boost",    10,   0.5f, 0f,   1.0f),
        MakeNode("node_membrane", "Membrane",         50,   1.0f, 0f,   1.0f, "node_mitosis"),
        MakeNode("node_atp",      "ATP Synthesis",    50,   0f,   0.5f, 1.0f, "node_mitosis"),
        MakeNode("node_nuclear",  "Nuclear Division", 200,  0f,   0f,   2.0f, "node_membrane"),
        MakeNode("node_mito1",    "Mitochondria",     200,  0f,   2.0f, 1.0f, "node_atp"),
        MakeNode("node_colony",   "Colony Formation", 1000, 0f,   10f,  1.0f, "node_mito1"),
    };

    static TechNodeData MakeNode(string id, string name, double cost,
        float cpcBonus, float cpsBonus, float cpcMult, params string[] prereqs)
    {
        var assetPath = $"{SODir}/{id}.asset";
        var existing  = AssetDatabase.LoadAssetAtPath<TechNodeData>(assetPath);
        if (existing != null) return existing;

        var node = ScriptableObject.CreateInstance<TechNodeData>();
        var so   = new SerializedObject(node);
        so.FindProperty("id").stringValue           = id;
        so.FindProperty("displayName").stringValue  = name;
        so.FindProperty("unlockCost").doubleValue   = cost;
        so.FindProperty("cpcFlatBonus").floatValue  = cpcBonus;
        so.FindProperty("cpsBonus").floatValue      = cpsBonus;
        so.FindProperty("cpcMultiplier").floatValue = cpcMult;
        var arr = so.FindProperty("prerequisiteIds");
        arr.arraySize = prereqs.Length;
        for (int i = 0; i < prereqs.Length; i++)
            arr.GetArrayElementAtIndex(i).stringValue = prereqs[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(node, assetPath);
        return node;
    }

    static ShopItemData[] CreateShopItems() => new[]
    {
        MakeShopItem("shop_ribosome", "Ribosome",          15,   1.15f, 0f,   0.1f),
        MakeShopItem("shop_cellwall", "Cell Wall",         100,  1.15f, 0.5f, 0f),
        MakeShopItem("shop_nucleus",  "Nucleus",           500,  1.15f, 0f,   2.0f),
        MakeShopItem("shop_stem",     "Stem Cell Factory", 5000, 1.15f, 0f,   20f),
    };

    static ShopItemData MakeShopItem(string assetName, string displayName,
        double baseCost, float scaling, float cpcBonus, float cpsBonus)
    {
        var assetPath = $"{SODir}/{assetName}.asset";
        var existing  = AssetDatabase.LoadAssetAtPath<ShopItemData>(assetPath);
        if (existing != null) return existing;

        var item = ScriptableObject.CreateInstance<ShopItemData>();
        var so   = new SerializedObject(item);
        so.FindProperty("displayName").stringValue  = displayName;
        so.FindProperty("baseCost").doubleValue     = baseCost;
        so.FindProperty("costScaling").floatValue   = scaling;
        so.FindProperty("cpcFlatBonus").floatValue  = cpcBonus;
        so.FindProperty("cpsBonus").floatValue      = cpsBonus;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(item, assetPath);
        return item;
    }

    // ─── Scene ────────────────────────────────────────────────────────────────

    static void BuildScene(Prefabs p, TechNodeData[] nodes, ShopItemData[] items)
    {
        // GameManager
        var gmGO  = GetOrCreate("GameManager");
        AddIfMissing<GameManager>(gmGO);
        var ttGO  = GetOrCreateChild(gmGO, "TechTreeManager");
        AddIfMissing<TechTreeManager>(ttGO);
        var smGO  = GetOrCreateChild(gmGO, "ShopManager");
        AddIfMissing<ShopManager>(smGO);

        // AudioManager — AudioSynth creates its own AudioSource at runtime
        var audioGO = GetOrCreate("AudioManager");
        AddIfMissing<AudioSynth>(audioGO);
        AddIfMissing<AudioHooks>(audioGO);

        // Canvas
        var canvasGO = GetOrCreate("Canvas");
        var canvas   = AddIfMissing<Canvas>(canvasGO);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = AddIfMissing<CanvasScaler>(canvasGO);
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        AddIfMissing<GraphicRaycaster>(canvasGO);

        // HUD
        var hudGO  = GetOrCreateChild(canvasGO, "HUD");
        StretchFull(hudGO);
        AddIfMissing<HUDController>(hudGO);
        var cellCountLbl = GetOrCreateTMP(hudGO, "CellCountLabel", "0 Cells", 22);
        var cpsLbl       = GetOrCreateTMP(hudGO, "CPSLabel", "0 CPS", 18);
        var cpcLbl       = GetOrCreateTMP(hudGO, "CPCLabel", "1 CPC", 18);
        SetAnchors(cellCountLbl, 0.02f, 0.93f, 0.6f, 0.99f);
        SetAnchors(cpsLbl,       0.02f, 0.87f, 0.4f, 0.93f);
        SetAnchors(cpcLbl,       0.02f, 0.81f, 0.4f, 0.87f);
        var hudSO = new SerializedObject(hudGO.GetComponent<HUDController>());
        hudSO.FindProperty("cellCountLabel").objectReferenceValue = cellCountLbl.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("cpsLabel").objectReferenceValue       = cpsLbl.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("cpcLabel").objectReferenceValue       = cpcLbl.GetComponent<TextMeshProUGUI>();
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        // Cell button
        var cellBtnGO = GetOrCreateChild(canvasGO, "CellButton");
        SetAnchors(cellBtnGO, 0.35f, 0.2f, 0.65f, 0.75f);
        AddIfMissing<Image>(cellBtnGO);        // CellButtonVisual will set the sprite
        AddIfMissing<CellButtonVisual>(cellBtnGO);
        AddIfMissing<Button>(cellBtnGO);
        AddIfMissing<ClickManager>(cellBtnGO);
        AddIfMissing<ClickFeedback>(cellBtnGO);

        // FloatingTextSpawner
        var ftsGO = GetOrCreateChild(canvasGO, "FloatingTextSpawner");
        AddIfMissing<FloatingTextSpawner>(ftsGO);
        var ftsSO = new SerializedObject(ftsGO.GetComponent<FloatingTextSpawner>());
        ftsSO.FindProperty("prefab").objectReferenceValue      = p.floatingText.GetComponent<FloatingText>();
        ftsSO.FindProperty("spawnAnchor").objectReferenceValue = cellBtnGO.GetComponent<RectTransform>();
        ftsSO.ApplyModifiedPropertiesWithoutUndo();

        // Tab bar
        var tabBarGO = GetOrCreateChild(canvasGO, "TabBar");
        SetAnchors(tabBarGO, 0f, 0f, 1f, 0.07f);

        // Shop panel
        var shopPanelGO = GetOrCreateChild(canvasGO, "ShopPanel");
        var shopContent = BuildScrollPanel(shopPanelGO, 0f, 0.07f, 0.5f, 0.78f);

        // Tech tree panel
        var techPanelGO = GetOrCreateChild(canvasGO, "TechTreePanel");
        var techContent = BuildScrollPanel(techPanelGO, 0.5f, 0.07f, 1f, 0.78f);

        // Wire TechTreeManager
        var ttSO = new SerializedObject(ttGO.GetComponent<TechTreeManager>());
        ttSO.FindProperty("nodePrefab").objectReferenceValue  = p.techNode.GetComponent<TechNode>();
        ttSO.FindProperty("linePrefab").objectReferenceValue  = p.springLine.GetComponent<SpringLine>();
        ttSO.FindProperty("contentRoot").objectReferenceValue = techContent.GetComponent<RectTransform>();
        var nodeArr = ttSO.FindProperty("allNodeData");
        nodeArr.arraySize = nodes.Length;
        for (int i = 0; i < nodes.Length; i++)
            nodeArr.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
        ttSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire ShopManager
        var smSO = new SerializedObject(smGO.GetComponent<ShopManager>());
        smSO.FindProperty("shopItemPrefab").objectReferenceValue  = p.shopItem.GetComponent<ShopItemUI>();
        smSO.FindProperty("shopContentRoot").objectReferenceValue = shopContent.transform;
        var itemArr = smSO.FindProperty("allItems");
        itemArr.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            itemArr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        smSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire ClickManager
        var cmSO = new SerializedObject(cellBtnGO.GetComponent<ClickManager>());
        cmSO.FindProperty("floatingTextSpawner").objectReferenceValue = ftsGO.GetComponent<FloatingTextSpawner>();
        cmSO.FindProperty("clickFeedback").objectReferenceValue       = cellBtnGO.GetComponent<ClickFeedback>();
        cmSO.ApplyModifiedPropertiesWithoutUndo();

        // AudioSynth has no serialized fields — it self-initialises at runtime
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static T AddIfMissing<T>(GameObject go) where T : Component
        => go.GetComponent<T>() ?? go.AddComponent<T>();

    static GameObject GetOrCreate(string name)
        => GameObject.Find(name) ?? new GameObject(name);

    static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        var t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject MakeChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject MakeTMPLabel(Transform parent, string name, string text, int size = 18)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    static GameObject GetOrCreateTMP(GameObject parent, string name, string text, int size = 18)
    {
        var t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        return MakeTMPLabel(parent.transform, name, text, size);
    }

    static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetAnchors(GameObject go, float x0, float y0, float x1, float y1)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetOffset(GameObject go, float left, float bottom, float right, float top)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static GameObject BuildScrollPanel(GameObject panelGO,
        float x0, float y0, float x1, float y1)
    {
        SetAnchors(panelGO, x0, y0, x1, y1);
        AddIfMissing<Image>(panelGO).color = new Color(0.05f, 0.08f, 0.05f, 0.95f);
        var sr = AddIfMissing<ScrollRect>(panelGO);
        sr.horizontal = false;

        var viewportGO = GetOrCreateChild(panelGO, "Viewport");
        StretchFull(viewportGO);
        AddIfMissing<Image>(viewportGO).color = new Color(0, 0, 0, 0.01f);
        AddIfMissing<Mask>(viewportGO).showMaskGraphic = false;
        sr.viewport = viewportGO.GetComponent<RectTransform>();

        var contentGO = GetOrCreateChild(viewportGO, "Content");
        var contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot     = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 400);
        var vlg = AddIfMissing<VerticalLayoutGroup>(contentGO);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4;
        AddIfMissing<ContentSizeFitter>(contentGO).verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRt;

        return contentGO;
    }
}
