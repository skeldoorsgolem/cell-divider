using System.IO;
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
    private const string PrefabDir  = "Assets/Prefabs";
    private const string SODir      = "Assets/Data";
    private const string SceneDir   = "Assets/Scenes";

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
        Debug.Log("[CellGameSetup] Scene setup complete!");
    }

    // ─── Folders ──────────────────────────────────────────────────────────────

    static void EnsureFolders()
    {
        foreach (var path in new[] { PrefabDir, SODir, SceneDir })
        {
            // Use System.IO to create the actual directory first,
            // then refresh so Unity picks it up without needing CreateFolder
            var fullPath = Application.dataPath + "/" + path.Substring("Assets/".Length);
            if (!System.IO.Directory.Exists(fullPath))
                System.IO.Directory.CreateDirectory(fullPath);
        }
        AssetDatabase.Refresh();
    }

    // ─── Prefabs ──────────────────────────────────────────────────────────────

    struct Prefabs
    {
        public GameObject techNode;
        public GameObject springLine;
        public GameObject shopItem;
        public GameObject floatingText;
    }

    static Prefabs CreatePrefabs()
    {
        var p = new Prefabs
        {
            techNode    = MakeTechNodePrefab(),
            springLine  = MakeSpringLinePrefab(),
            shopItem    = MakeShopItemPrefab(),
            floatingText = MakeFloatingTextPrefab(),
        };
        return p;
    }

    static GameObject MakeTechNodePrefab()
    {
        const string path = PrefabDir + "/TechNodePrefab.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var root = new GameObject("TechNodePrefab");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 200);

        root.AddComponent<TechNode>();

        // Button on root
        root.AddComponent<Button>();
        root.AddComponent<Image>(); // Button needs an Image

        // Sprite child
        var spriteGO  = new GameObject("NodeSprite");
        spriteGO.transform.SetParent(root.transform, false);
        var spriteImg = spriteGO.AddComponent<Image>();
        spriteImg.raycastTarget = false;
        var spriteRt  = spriteGO.GetComponent<RectTransform>();
        spriteRt.anchorMin = spriteRt.anchorMax = new Vector2(0.5f, 0.5f);
        spriteRt.sizeDelta = new Vector2(160, 160);

        // Name label
        var nameGO  = CreateTMPLabel(root.transform, "NameLabel", "Node Name");
        var nameRt  = nameGO.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0.15f);
        nameRt.anchorMax = new Vector2(1, 0.45f);
        nameRt.offsetMin = nameRt.offsetMax = Vector2.zero;

        // Cost label
        var costGO  = CreateTMPLabel(root.transform, "CostLabel", "0 cells");
        var costRt  = costGO.GetComponent<RectTransform>();
        costRt.anchorMin = new Vector2(0, 0f);
        costRt.anchorMax = new Vector2(1, 0.2f);
        costRt.offsetMin = costRt.offsetMax = Vector2.zero;

        // Wire TechNode fields via SerializedObject
        var prefab  = SavePrefab(root, path);
        var so      = new SerializedObject(prefab.GetComponent<TechNode>());
        so.FindProperty("nodeSprite").objectReferenceValue   = spriteImg;
        so.FindProperty("nameLabel").objectReferenceValue    = nameGO.GetComponent<TextMeshProUGUI>();
        so.FindProperty("costLabel").objectReferenceValue    = costGO.GetComponent<TextMeshProUGUI>();
        so.FindProperty("unlockButton").objectReferenceValue = prefab.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        Object.DestroyImmediate(root);
        PrefabUtility.SavePrefabAsset(prefab);
        return prefab;
    }

    static GameObject MakeSpringLinePrefab()
    {
        const string path = PrefabDir + "/SpringLinePrefab.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var root = new GameObject("SpringLinePrefab");
        var lr   = root.AddComponent<LineRenderer>();
        lr.startWidth  = 0.05f;
        lr.endWidth    = 0.05f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(0.4f, 0.8f, 0.4f, 0.8f);
        root.AddComponent<SpringLine>();

        var prefab = SavePrefab(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject MakeShopItemPrefab()
    {
        const string path = PrefabDir + "/ShopItemPrefab.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var root = new GameObject("ShopItemPrefab");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.15f, 0.1f, 0.9f);

        root.AddComponent<ShopItemUI>();

        // Name label (left)
        var nameGO = CreateTMPLabel(root.transform, "NameLabel", "Item Name", 16);
        var nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0.5f); nameRt.anchorMax = new Vector2(0.4f, 1f);
        nameRt.offsetMin = new Vector2(10, 0);   nameRt.offsetMax = Vector2.zero;

        // Cost label (middle)
        var costGO = CreateTMPLabel(root.transform, "CostLabel", "0 cells", 14);
        var costRt = costGO.GetComponent<RectTransform>();
        costRt.anchorMin = new Vector2(0, 0);    costRt.anchorMax = new Vector2(0.4f, 0.5f);
        costRt.offsetMin = new Vector2(10, 0);   costRt.offsetMax = Vector2.zero;

        // Count label (right of middle)
        var countGO = CreateTMPLabel(root.transform, "CountLabel", "x0", 14);
        var countRt = countGO.GetComponent<RectTransform>();
        countRt.anchorMin = new Vector2(0.4f, 0); countRt.anchorMax = new Vector2(0.65f, 1f);
        countRt.offsetMin = countRt.offsetMax = Vector2.zero;

        // Buy button (right)
        var btnGO = new GameObject("BuyButton");
        btnGO.transform.SetParent(root.transform, false);
        var btnRt = btnGO.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.68f, 0.1f); btnRt.anchorMax = new Vector2(0.98f, 0.9f);
        btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.5f, 0.2f);
        var btn = btnGO.AddComponent<Button>();
        var btnLabelGO = CreateTMPLabel(btnGO.transform, "BuyLabel", "Buy", 14);
        var btnLabelRt = btnLabelGO.GetComponent<RectTransform>();
        btnLabelRt.anchorMin = Vector2.zero; btnLabelRt.anchorMax = Vector2.one;
        btnLabelRt.offsetMin = btnLabelRt.offsetMax = Vector2.zero;

        var prefab = SavePrefab(root, path);
        var siuSO  = new SerializedObject(prefab.GetComponent<ShopItemUI>());
        siuSO.FindProperty("nameLabel").objectReferenceValue  = nameGO.GetComponent<TextMeshProUGUI>();
        siuSO.FindProperty("costLabel").objectReferenceValue  = costGO.GetComponent<TextMeshProUGUI>();
        siuSO.FindProperty("countLabel").objectReferenceValue = countGO.GetComponent<TextMeshProUGUI>();
        siuSO.FindProperty("buyButton").objectReferenceValue  = btn;
        siuSO.ApplyModifiedPropertiesWithoutUndo();

        Object.DestroyImmediate(root);
        PrefabUtility.SavePrefabAsset(prefab);
        return prefab;
    }

    static GameObject MakeFloatingTextPrefab()
    {
        const string path = PrefabDir + "/FloatingTextPrefab.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var root = new GameObject("FloatingTextPrefab");
        root.AddComponent<RectTransform>();
        var cg = root.AddComponent<CanvasGroup>();
        root.AddComponent<FloatingText>();

        var labelGO = CreateTMPLabel(root.transform, "Label", "+1");
        var labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

        var prefab = SavePrefab(root, path);
        var ftSO   = new SerializedObject(prefab.GetComponent<FloatingText>());
        ftSO.FindProperty("label").objectReferenceValue        = labelGO.GetComponent<TextMeshProUGUI>();
        ftSO.FindProperty("canvasGroup").objectReferenceValue  = cg;
        ftSO.ApplyModifiedPropertiesWithoutUndo();

        Object.DestroyImmediate(root);
        PrefabUtility.SavePrefabAsset(prefab);
        return prefab;
    }

    // ─── ScriptableObjects ────────────────────────────────────────────────────

    static TechNodeData[] CreateTechNodes()
    {
        return new[]
        {
            MakeNode("node_mitosis",  "Mitosis Boost",     10,   0.5f, 0f,   1.0f, 0),
            MakeNode("node_membrane", "Membrane",          50,   1.0f, 0f,   1.0f, 0, "node_mitosis"),
            MakeNode("node_atp",      "ATP Synthesis",     50,   0f,   0.5f, 1.0f, 0, "node_mitosis"),
            MakeNode("node_nuclear",  "Nuclear Division",  200,  0f,   0f,   2.0f, 0, "node_membrane"),
            MakeNode("node_mito1",    "Mitochondria",      200,  0f,   2.0f, 1.0f, 0, "node_atp"),
            MakeNode("node_colony",   "Colony Formation",  1000, 0f,   10f,  1.0f, 0, "node_mito1"),
        };
    }

    static TechNodeData MakeNode(string id, string displayName, double cost,
        float cpcBonus, float cpsBonus, float cpcMult, int tier, params string[] prereqs)
    {
        var path = $"{SODir}/{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TechNodeData>(path);
        if (existing != null) return existing;

        var node = ScriptableObject.CreateInstance<TechNodeData>();
        var so   = new SerializedObject(node);
        so.FindProperty("id").stringValue           = id;
        so.FindProperty("displayName").stringValue  = displayName;
        so.FindProperty("unlockCost").doubleValue   = cost;
        so.FindProperty("cpcFlatBonus").floatValue  = cpcBonus;
        so.FindProperty("cpsBonus").floatValue      = cpsBonus;
        so.FindProperty("cpcMultiplier").floatValue = cpcMult;
        so.FindProperty("tier").intValue            = tier;
        var prereqProp = so.FindProperty("prerequisiteIds");
        prereqProp.arraySize = prereqs.Length;
        for (int i = 0; i < prereqs.Length; i++)
            prereqProp.GetArrayElementAtIndex(i).stringValue = prereqs[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(node, path);
        return node;
    }

    static ShopItemData[] CreateShopItems()
    {
        return new[]
        {
            MakeShopItem("shop_ribosome", "Ribosome",           15,   1.15f, 0f,   0.1f),
            MakeShopItem("shop_cellwall", "Cell Wall",          100,  1.15f, 0.5f, 0f),
            MakeShopItem("shop_nucleus",  "Nucleus",            500,  1.15f, 0f,   2.0f),
            MakeShopItem("shop_stem",     "Stem Cell Factory",  5000, 1.15f, 0f,   20f),
        };
    }

    static ShopItemData MakeShopItem(string assetName, string displayName,
        double baseCost, float costScaling, float cpcBonus, float cpsBonus)
    {
        var path = $"{SODir}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
        if (existing != null) return existing;

        var item = ScriptableObject.CreateInstance<ShopItemData>();
        var so   = new SerializedObject(item);
        so.FindProperty("displayName").stringValue  = displayName;
        so.FindProperty("baseCost").doubleValue     = baseCost;
        so.FindProperty("costScaling").floatValue   = costScaling;
        so.FindProperty("cpcFlatBonus").floatValue  = cpcBonus;
        so.FindProperty("cpsBonus").floatValue      = cpsBonus;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    // ─── Scene ────────────────────────────────────────────────────────────────

    static void BuildScene(Prefabs prefabs, TechNodeData[] nodes, ShopItemData[] items)
    {
        // ── GameManager ──
        var gmGO = GetOrCreate("GameManager");
        AddIfMissing<GameManager>(gmGO);

        var ttGO = GetOrCreateChild(gmGO, "TechTreeManager");
        AddIfMissing<TechTreeManager>(ttGO);

        var smGO = GetOrCreateChild(gmGO, "ShopManager");
        AddIfMissing<ShopManager>(smGO);

        // ── AudioManager ──
        var audioGO = GetOrCreate("AudioManager");
        AddIfMissing<AudioSource>(audioGO);
        AddIfMissing<AudioSynth>(audioGO);
        AddIfMissing<AudioHooks>(audioGO);

        // ── Canvas ──
        var canvasGO = GetOrCreate("Canvas");
        var canvas   = AddIfMissing<Canvas>(canvasGO);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        AddIfMissing<CanvasScaler>(canvasGO).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)canvasGO.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1920, 1080);
        AddIfMissing<GraphicRaycaster>(canvasGO);

        // HUD
        var hudGO      = GetOrCreateChild(canvasGO, "HUD");
        StretchFull(hudGO);
        AddIfMissing<HUDController>(hudGO);
        var cellCountLabel = GetOrCreateTMPLabel(hudGO, "CellCountLabel", "0 Cells");
        var cpsLabel       = GetOrCreateTMPLabel(hudGO, "CPSLabel",       "0 CPS");
        var cpcLabel       = GetOrCreateTMPLabel(hudGO, "CPCLabel",       "1 CPC");
        PositionLabel(cellCountLabel, 0.02f, 0.92f, 0.5f, 0.98f);
        PositionLabel(cpsLabel,       0.02f, 0.86f, 0.3f, 0.92f);
        PositionLabel(cpcLabel,       0.02f, 0.80f, 0.3f, 0.86f);

        var hudSO = new SerializedObject(hudGO.GetComponent<HUDController>());
        hudSO.FindProperty("cellCountLabel").objectReferenceValue = cellCountLabel.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("cpsLabel").objectReferenceValue       = cpsLabel.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("cpcLabel").objectReferenceValue       = cpcLabel.GetComponent<TextMeshProUGUI>();
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        // Cell button
        var cellBtnGO = GetOrCreateChild(canvasGO, "CellButton");
        var cellBtnRt = cellBtnGO.GetComponent<RectTransform>() ?? cellBtnGO.AddComponent<RectTransform>();
        cellBtnRt.anchorMin = new Vector2(0.35f, 0.2f);
        cellBtnRt.anchorMax = new Vector2(0.65f, 0.7f);
        cellBtnRt.offsetMin = cellBtnRt.offsetMax = Vector2.zero;
        AddIfMissing<Image>(cellBtnGO).color = new Color(0, 0, 0, 0); // transparent backing
        AddIfMissing<Button>(cellBtnGO);
        AddIfMissing<ClickManager>(cellBtnGO);
        AddIfMissing<ClickFeedback>(cellBtnGO);
        AddIfMissing<SquishyCell>(cellBtnGO);

        // FloatingTextSpawner
        var ftsGO = GetOrCreateChild(canvasGO, "FloatingTextSpawner");
        AddIfMissing<FloatingTextSpawner>(ftsGO);
        var ftsSO = new SerializedObject(ftsGO.GetComponent<FloatingTextSpawner>());
        ftsSO.FindProperty("prefab").objectReferenceValue      = prefabs.floatingText.GetComponent<FloatingText>();
        ftsSO.FindProperty("spawnAnchor").objectReferenceValue = cellBtnGO.GetComponent<RectTransform>();
        ftsSO.ApplyModifiedPropertiesWithoutUndo();

        // Tab bar
        var tabBarGO  = GetOrCreateChild(canvasGO, "TabBar");
        var tabBarRt  = tabBarGO.GetComponent<RectTransform>() ?? tabBarGO.AddComponent<RectTransform>();
        tabBarRt.anchorMin = new Vector2(0, 0); tabBarRt.anchorMax = new Vector2(1, 0.08f);
        tabBarRt.offsetMin = tabBarRt.offsetMax = Vector2.zero;

        // Shop panel
        var shopPanelGO   = GetOrCreateChild(canvasGO, "ShopPanel");
        var shopScrollRect = AddScrollView(shopPanelGO, new Vector2(0, 0.08f), new Vector2(0.5f, 0.78f));
        var shopContent   = shopScrollRect.content.gameObject;

        // TechTree panel
        var techPanelGO   = GetOrCreateChild(canvasGO, "TechTreePanel");
        var techScrollRect = AddScrollView(techPanelGO, new Vector2(0.5f, 0.08f), new Vector2(1f, 0.78f));
        var techContent   = techScrollRect.content.gameObject;

        // Wire TechTreeManager
        var ttSO = new SerializedObject(ttGO.GetComponent<TechTreeManager>());
        ttSO.FindProperty("nodePrefab").objectReferenceValue  = prefabs.techNode.GetComponent<TechNode>();
        ttSO.FindProperty("linePrefab").objectReferenceValue  = prefabs.springLine.GetComponent<SpringLine>();
        ttSO.FindProperty("contentRoot").objectReferenceValue = techContent.GetComponent<RectTransform>();
        var nodesProp = ttSO.FindProperty("allNodeData");
        nodesProp.arraySize = nodes.Length;
        for (int i = 0; i < nodes.Length; i++)
            nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
        ttSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire ShopManager
        var smSO = new SerializedObject(smGO.GetComponent<ShopManager>());
        smSO.FindProperty("shopItemPrefab").objectReferenceValue  = prefabs.shopItem.GetComponent<ShopItemUI>();
        smSO.FindProperty("shopContentRoot").objectReferenceValue = shopContent.GetComponent<Transform>();
        var itemsProp = smSO.FindProperty("allItems");
        itemsProp.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        smSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire ClickManager
        var cmSO = new SerializedObject(cellBtnGO.GetComponent<ClickManager>());
        cmSO.FindProperty("floatingTextSpawner").objectReferenceValue = ftsGO.GetComponent<FloatingTextSpawner>();
        cmSO.FindProperty("clickFeedback").objectReferenceValue       = cellBtnGO.GetComponent<ClickFeedback>();
        cmSO.FindProperty("squishyCell").objectReferenceValue         = cellBtnGO.GetComponent<SquishyCell>();
        cmSO.ApplyModifiedPropertiesWithoutUndo();

        // Wire AudioSynth AudioSource
        var audioSynthSO = new SerializedObject(audioGO.GetComponent<AudioSynth>());
        audioSynthSO.FindProperty("audioSource").objectReferenceValue = audioGO.GetComponent<AudioSource>();
        audioSynthSO.ApplyModifiedPropertiesWithoutUndo();

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[CellGameSetup] Scene built. Hit Ctrl+S to save.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static GameObject SavePrefab(GameObject root, string path)
    {
        return PrefabUtility.SaveAsPrefabAsset(root, path);
    }

    static T AddIfMissing<T>(GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }

    static GameObject GetOrCreate(string name)
    {
        var found = GameObject.Find(name);
        return found ?? new GameObject(name);
    }

    static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        var t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject CreateTMPLabel(Transform parent, string name, string text, int fontSize = 18)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    static GameObject GetOrCreateTMPLabel(GameObject parent, string name, string text)
    {
        var t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        return CreateTMPLabel(parent.transform, name, text, 20);
    }

    static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void PositionLabel(GameObject go, float xMin, float yMin, float xMax, float yMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static ScrollRect AddScrollView(GameObject panelGO, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = panelGO.GetComponent<RectTransform>() ?? panelGO.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        AddIfMissing<Image>(panelGO).color = new Color(0.05f, 0.08f, 0.05f, 0.95f);
        var sr = AddIfMissing<ScrollRect>(panelGO);
        sr.horizontal = false;

        // Viewport
        var viewportGO = GetOrCreateChild(panelGO, "Viewport");
        StretchFull(viewportGO);
        AddIfMissing<Image>(viewportGO).color = new Color(0, 0, 0, 0.01f);
        var mask = AddIfMissing<Mask>(viewportGO);
        mask.showMaskGraphic = false;
        sr.viewport = viewportGO.GetComponent<RectTransform>();

        // Content
        var contentGO = GetOrCreateChild(viewportGO, "Content");
        var contentRt = contentGO.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot     = new Vector2(0.5f, 1);
        contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 400);
        var vlg = AddIfMissing<VerticalLayoutGroup>(contentGO);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4;
        AddIfMissing<ContentSizeFitter>(contentGO).verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content = contentRt;
        return sr;
    }
}
