using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupDocumentReading
{
    private static Font uiFont;

    [MenuItem("Tools/Setup Document Reading Demo")]
    public static void Setup()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject player = GameObject.Find("Player");
        if (player == null) throw new System.InvalidOperationException("Player is missing. Set up the first-person scene first.");

        GameObject documentObject = GameObject.Find("Test Document");
        if (documentObject == null) documentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        documentObject.name = "Test Document";
        documentObject.transform.position = new Vector3(0f, 1.55f, 2.2f);
        documentObject.transform.localScale = new Vector3(0.85f, 1.1f, 0.06f);
        var document = documentObject.GetComponent<ReadableDocument>();
        if (document == null) document = documentObject.AddComponent<ReadableDocument>();
        var documentData = new SerializedObject(document);
        documentData.FindProperty("documentTitle").stringValue = "Записка";
        documentData.FindProperty("documentText").stringValue =
            "Это пример документа.\n\nПодойдите ближе и наведите взгляд на лист. " +
            "Нажмите E, чтобы прочитать его.\n\n" +
            "Заголовок и текст этой записки можно заменить в Inspector на объекте Test Document.";
        documentData.ApplyModifiedPropertiesWithoutUndo();

        GameObject canvasObject = GameObject.Find("Document UI");
        if (canvasObject != null) Object.DestroyImmediate(canvasObject);
        canvasObject = new GameObject("Document UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Text crosshair = MakeText("Crosshair", canvasObject.transform, "+", 36, Color.white, TextAnchor.MiddleCenter);
        SetCentered(crosshair.rectTransform, new Vector2(40f, 40f), Vector2.zero);

        Text prompt = MakeText("Read Prompt", canvasObject.transform, "[E] Прочитать", 32, Color.white, TextAnchor.MiddleCenter);
        SetCentered(prompt.rectTransform, new Vector2(500f, 60f), new Vector2(0f, -350f));

        var panelObject = new GameObject("Reading Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        SetCentered(panelObject.GetComponent<RectTransform>(), new Vector2(960f, 720f), Vector2.zero);
        panelObject.GetComponent<Image>().color = new Color(0.96f, 0.94f, 0.86f, 1f);

        Text title = MakeText("Document Title", panelObject.transform, "", 48, new Color(0.14f, 0.12f, 0.1f), TextAnchor.MiddleCenter);
        SetCentered(title.rectTransform, new Vector2(840f, 90f), new Vector2(0f, 285f));

        var viewportObject = new GameObject("Text Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.transform.SetParent(panelObject.transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        SetCentered(viewport, new Vector2(830f, 475f), new Vector2(0f, -15f));

        Text body = MakeText("Document Body", viewportObject.transform, "", 30, new Color(0.16f, 0.14f, 0.12f), TextAnchor.UpperLeft);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.lineSpacing = 1.25f;
        body.rectTransform.anchorMin = new Vector2(0f, 1f);
        body.rectTransform.anchorMax = new Vector2(1f, 1f);
        body.rectTransform.pivot = new Vector2(0.5f, 1f);
        body.rectTransform.anchoredPosition = Vector2.zero;
        body.rectTransform.sizeDelta = Vector2.zero;
        var fitter = body.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewportObject.GetComponent<ScrollRect>();
        scroll.content = body.rectTransform;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        Text hint = MakeText("Close Hint", panelObject.transform, "[E / Esc] Закрыть     [Колесо мыши] Прокрутить", 24,
            new Color(0.35f, 0.32f, 0.28f), TextAnchor.MiddleCenter);
        SetCentered(hint.rectTransform, new Vector2(840f, 55f), new Vector2(0f, -320f));

        var reader = player.GetComponent<DocumentReader>();
        if (reader == null) reader = player.AddComponent<DocumentReader>();
        var readerData = new SerializedObject(reader);
        readerData.FindProperty("playerCamera").objectReferenceValue = player.GetComponentInChildren<Camera>();
        readerData.FindProperty("movement").objectReferenceValue = player.GetComponent<FirstPersonPlayer>();
        readerData.FindProperty("interactionPrompt").objectReferenceValue = prompt.gameObject;
        readerData.FindProperty("readingPanel").objectReferenceValue = panelObject;
        readerData.FindProperty("titleText").objectReferenceValue = title;
        readerData.FindProperty("bodyText").objectReferenceValue = body;
        readerData.FindProperty("bodyScrollRect").objectReferenceValue = scroll;
        readerData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(reader);

        prompt.gameObject.SetActive(false);
        panelObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Document reading demo is ready. Edit Test Document in the Inspector.");
    }

    private static Text MakeText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor anchor)
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
