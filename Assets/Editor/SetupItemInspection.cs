using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupItemInspection
{
    private static Font uiFont;

    [MenuItem("Tools/Setup Item Inspection Demo")]
    public static void Setup()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);
        if (GameObject.Find("Item Inspection UI") != null)
        {
            Debug.Log("Item inspection demo is already in this scene.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null) throw new System.InvalidOperationException("Player is missing from SampleScene.");
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateItem("Inspect Cube", "Красный куб", PrimitiveType.Cube,
            new Vector3(-1.5f, 0.4f, 2.7f), new Vector3(0.8f, 0.8f, 0.8f),
            GetMaterial("Inspect_Red", new Color(0.85f, 0.16f, 0.16f)));
        CreateItem("Inspect Sphere", "Синяя сфера", PrimitiveType.Sphere,
            new Vector3(1.5f, 0.4f, 2.7f), Vector3.one * 0.8f,
            GetMaterial("Inspect_Blue", new Color(0.13f, 0.43f, 0.86f)));
        CreateItem("Inspect Capsule", "Зелёная капсула", PrimitiveType.Capsule,
            new Vector3(-2.6f, 0.65f, 3.4f), new Vector3(0.6f, 0.65f, 0.6f),
            GetMaterial("Inspect_Green", new Color(0.18f, 0.68f, 0.31f)));
        CreateItem("Inspect Cylinder", "Жёлтый цилиндр", PrimitiveType.Cylinder,
            new Vector3(2.6f, 0.55f, 3.4f), new Vector3(0.6f, 0.55f, 0.6f),
            GetMaterial("Inspect_Yellow", new Color(0.92f, 0.64f, 0.12f)));

        var canvasObject = new GameObject("Item Inspection UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Text prompt = MakeText("Inspect Prompt", canvasObject.transform, "[E] Осмотреть", 32,
            Color.white, TextAnchor.MiddleCenter);
        SetCentered(prompt.rectTransform, new Vector2(660f, 60f), new Vector2(0f, -350f));

        var panelObject = new GameObject("Inspection Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        SetCentered(panelObject.GetComponent<RectTransform>(), new Vector2(1000f, 820f), Vector2.zero);
        panelObject.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f, 0.98f);

        Text title = MakeText("Item Title", panelObject.transform, "Предмет", 48,
            Color.white, TextAnchor.MiddleCenter);
        SetCentered(title.rectTransform, new Vector2(900f, 75f), new Vector2(0f, 345f));

        var previewObject = new GameObject("Item Preview", typeof(RectTransform), typeof(RawImage));
        previewObject.transform.SetParent(panelObject.transform, false);
        var previewImage = previewObject.GetComponent<RawImage>();
        previewImage.color = Color.white;
        SetCentered(previewImage.rectTransform, new Vector2(650f, 600f), new Vector2(0f, 0f));

        Text hint = MakeText("Inspect Hint", panelObject.transform,
            "ЛКМ + движение мыши — вращать    Колесо — масштаб    E / Esc — закрыть",
            26, new Color(0.82f, 0.85f, 0.9f), TextAnchor.MiddleCenter);
        SetCentered(hint.rectTransform, new Vector2(940f, 55f), new Vector2(0f, -350f));

        var inspector = player.GetComponent<ItemInspector>();
        if (inspector == null) inspector = player.AddComponent<ItemInspector>();
        var data = new SerializedObject(inspector);
        data.FindProperty("playerCamera").objectReferenceValue = player.GetComponentInChildren<Camera>();
        data.FindProperty("movement").objectReferenceValue = player.GetComponent<FirstPersonPlayer>();
        data.FindProperty("documentReader").objectReferenceValue = player.GetComponent<DocumentReader>();
        data.FindProperty("interactionPrompt").objectReferenceValue = prompt.gameObject;
        data.FindProperty("promptText").objectReferenceValue = prompt;
        data.FindProperty("inspectionPanel").objectReferenceValue = panelObject;
        data.FindProperty("itemTitle").objectReferenceValue = title;
        data.FindProperty("previewImage").objectReferenceValue = previewImage;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(inspector);

        prompt.gameObject.SetActive(false);
        panelObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Item inspection demo is ready: cube, sphere, capsule and cylinder.");
    }

    private static void CreateItem(string objectName, string displayName, PrimitiveType shape,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null) obj = GameObject.CreatePrimitive(shape);
        obj.name = objectName;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;

        var item = obj.GetComponent<InspectableItem>();
        if (item == null) item = obj.AddComponent<InspectableItem>();
        var data = new SerializedObject(item);
        data.FindProperty("itemName").stringValue = displayName;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material GetMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = "Assets/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Text MakeText(string name, Transform parent, string content, int fontSize,
        Color color, TextAnchor anchor)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.text = content;
        return text;
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
