using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class SetupNpcDialogue
{
    private static Font uiFont;

    [MenuItem("Tools/Setup NPC Dialogue Demo")]
    public static void Setup()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);
        if (GameObject.Find("Demo NPC") != null)
        {
            var existingCanvas = GameObject.Find("Dialogue UI");
            if (existingCanvas != null && existingCanvas.GetComponent<GraphicRaycaster>() == null)
                existingCanvas.AddComponent<GraphicRaycaster>();
            var existingController = GameObject.Find("Player").GetComponent<DialogueController>();
            if (existingController != null)
            {
                var existingData = new SerializedObject(existingController);
                SetOtherPrompts(existingData);
                existingData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existingController);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("NPC dialogue demo updated.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null) throw new System.InvalidOperationException("Player is missing from SampleScene.");
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Material coat = GetMaterial("NPC_Coat", new Color(0.12f, 0.38f, 0.52f));
        Material skin = GetMaterial("NPC_Skin", new Color(0.93f, 0.72f, 0.52f));
        Material trousers = GetMaterial("NPC_Trousers", new Color(0.16f, 0.20f, 0.27f));
        Material dark = GetMaterial("NPC_Dark", new Color(0.08f, 0.10f, 0.14f));

        var npcObject = new GameObject("Demo NPC", typeof(CapsuleCollider), typeof(NpcDialogue));
        npcObject.transform.position = new Vector3(3.8f, 0.05f, 5.0f);
        npcObject.transform.rotation = Quaternion.LookRotation(player.transform.position - npcObject.transform.position);
        var collider = npcObject.GetComponent<CapsuleCollider>();
        collider.height = 1.9f;
        collider.radius = 0.32f;
        collider.center = new Vector3(0f, 0.95f, 0f);
        BuildCharacter(npcObject.transform, coat, skin, trousers, dark);
        SetDialogue(npcObject.GetComponent<NpcDialogue>());

        var playerBody = new GameObject("Dialogue Player Body");
        playerBody.transform.SetParent(player.transform, false);
        BuildCharacter(playerBody.transform,
            GetMaterial("Player_Jacket", new Color(0.40f, 0.31f, 0.20f)),
            skin, trousers, dark);
        playerBody.SetActive(false);

        var cameraObject = new GameObject("Dialogue Camera", typeof(Camera));
        var dialogueCamera = cameraObject.GetComponent<Camera>();
        dialogueCamera.enabled = false;
        dialogueCamera.fieldOfView = 60f;
        dialogueCamera.nearClipPlane = 0.05f;

        var canvasObject = new GameObject("Dialogue UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Text prompt = MakeText("Talk Prompt", canvasObject.transform, "[E] Поговорить — Смотритель",
            32, Color.white, TextAnchor.MiddleCenter);
        Center(prompt.rectTransform, new Vector2(650f, 60f), new Vector2(0f, -350f));

        var panel = new GameObject("Dialogue Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        Center(panel.GetComponent<RectTransform>(), new Vector2(1400f, 490f), new Vector2(0f, -275f));
        panel.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.13f, 0.94f);

        Text speaker = MakeText("Speaker", panel.transform, "", 38,
            new Color(0.93f, 0.72f, 0.39f), TextAnchor.MiddleLeft);
        Center(speaker.rectTransform, new Vector2(1280f, 54f), new Vector2(0f, 200f));
        Text line = MakeText("Dialogue Text", panel.transform, "", 32,
            Color.white, TextAnchor.UpperLeft);
        Center(line.rectTransform, new Vector2(1280f, 110f), new Vector2(0f, 120f));
        line.horizontalOverflow = HorizontalWrapMode.Wrap;
        line.verticalOverflow = VerticalWrapMode.Overflow;

        var choices = new Button[3];
        for (int i = 0; i < choices.Length; i++)
            choices[i] = MakeButton("Choice " + (i + 1), panel.transform,
                new Vector2(1250f, 63f), new Vector2(0f, 12f - i * 74f), "", 27);
        Button next = MakeButton("Continue", panel.transform,
            new Vector2(1250f, 63f), new Vector2(0f, -160f), "Продолжить  [E / Пробел]", 27);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventObject = new GameObject("Dialogue EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        var controller = player.GetComponent<DialogueController>();
        if (controller == null) controller = player.AddComponent<DialogueController>();
        var data = new SerializedObject(controller);
        data.FindProperty("playerCamera").objectReferenceValue = player.GetComponentInChildren<Camera>();
        data.FindProperty("dialogueCamera").objectReferenceValue = dialogueCamera;
        data.FindProperty("npc").objectReferenceValue = npcObject.GetComponent<NpcDialogue>();
        data.FindProperty("movement").objectReferenceValue = player.GetComponent<FirstPersonPlayer>();
        data.FindProperty("documentReader").objectReferenceValue = player.GetComponent<DocumentReader>();
        data.FindProperty("itemInspector").objectReferenceValue = player.GetComponent<ItemInspector>();
        data.FindProperty("playerBody").objectReferenceValue = playerBody;
        data.FindProperty("crosshair").objectReferenceValue = GameObject.Find("Crosshair");
        SetOtherPrompts(data);
        data.FindProperty("interactionPrompt").objectReferenceValue = prompt.gameObject;
        data.FindProperty("dialoguePanel").objectReferenceValue = panel;
        data.FindProperty("speakerText").objectReferenceValue = speaker;
        data.FindProperty("dialogueText").objectReferenceValue = line;
        var buttonProperty = data.FindProperty("choiceButtons");
        buttonProperty.arraySize = choices.Length;
        for (int i = 0; i < choices.Length; i++)
            buttonProperty.GetArrayElementAtIndex(i).objectReferenceValue = choices[i];
        data.FindProperty("continueButton").objectReferenceValue = next;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);

        prompt.gameObject.SetActive(false);
        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("NPC dialogue demo is ready. Approach the Sмотритель and press E.");
    }

    private static void SetOtherPrompts(SerializedObject data)
    {
        var prompts = data.FindProperty("otherPrompts");
        prompts.arraySize = 2;
        Transform documentUi = GameObject.Find("Document UI")?.transform;
        Transform itemUi = GameObject.Find("Item Inspection UI")?.transform;
        prompts.GetArrayElementAtIndex(0).objectReferenceValue =
            documentUi != null ? documentUi.Find("Read Prompt")?.gameObject : null;
        prompts.GetArrayElementAtIndex(1).objectReferenceValue =
            itemUi != null ? itemUi.Find("Inspect Prompt")?.gameObject : null;
    }

    private static void SetDialogue(NpcDialogue npc)
    {
        var data = new SerializedObject(npc);
        data.FindProperty("characterName").stringValue = "Смотритель";
        var nodes = data.FindProperty("nodes");
        nodes.arraySize = 4;
        SetNode(nodes, 0, "Привет! Я присматриваю за этим местом. Что тебя интересует?",
            new[] { "Что это за место?", "Что здесь можно делать?", "Пока!" }, new[] { 1, 2, 3 });
        SetNode(nodes, 1, "Это маленькая испытательная площадка. Здесь мы проверяем новые игровые механики.",
            new[] { "А что за предметы вокруг?", "Вернуться к вопросам", "Спасибо, до встречи." }, new[] { 2, 0, 3 });
        SetNode(nodes, 2, "Можно читать записки и осматривать предметы. Подойди поближе и нажми E.",
            new[] { "Понятно, спасибо!", "У меня ещё вопрос." }, new[] { 3, 0 });
        SetNode(nodes, 3, "Удачи с исследованием! Если понадобится помощь, я буду здесь.",
            new[] { "Закончить разговор", "Спросить ещё" }, new[] { -1, 0 });
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(npc);
    }

    private static void SetNode(SerializedProperty nodes, int index, string text, string[] labels, int[] destinations)
    {
        var node = nodes.GetArrayElementAtIndex(index);
        node.FindPropertyRelative("npcText").stringValue = text;
        var choices = node.FindPropertyRelative("choices");
        choices.arraySize = labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            var choice = choices.GetArrayElementAtIndex(i);
            choice.FindPropertyRelative("text").stringValue = labels[i];
            choice.FindPropertyRelative("nextNode").intValue = destinations[i];
        }
    }

    private static void BuildCharacter(Transform root, Material coat, Material skin, Material trousers, Material dark)
    {
        Part("Torso", PrimitiveType.Capsule, root, new Vector3(0f, 1.12f, 0f),
            new Vector3(0.48f, 0.56f, 0.30f), coat);
        Part("Head", PrimitiveType.Sphere, root, new Vector3(0f, 1.72f, 0f),
            new Vector3(0.39f, 0.42f, 0.37f), skin);
        Part("Hair", PrimitiveType.Sphere, root, new Vector3(0f, 1.94f, -0.02f),
            new Vector3(0.40f, 0.12f, 0.38f), dark);
        Part("Left Arm", PrimitiveType.Capsule, root, new Vector3(-0.38f, 1.13f, 0f),
            new Vector3(0.15f, 0.40f, 0.15f), coat);
        Part("Right Arm", PrimitiveType.Capsule, root, new Vector3(0.38f, 1.13f, 0f),
            new Vector3(0.15f, 0.40f, 0.15f), coat);
        Part("Left Leg", PrimitiveType.Capsule, root, new Vector3(-0.14f, 0.42f, 0f),
            new Vector3(0.19f, 0.42f, 0.20f), trousers);
        Part("Right Leg", PrimitiveType.Capsule, root, new Vector3(0.14f, 0.42f, 0f),
            new Vector3(0.19f, 0.42f, 0.20f), trousers);
        Part("Left Eye", PrimitiveType.Sphere, root, new Vector3(-0.10f, 1.75f, 0.17f),
            new Vector3(0.045f, 0.045f, 0.025f), dark);
        Part("Right Eye", PrimitiveType.Sphere, root, new Vector3(0.10f, 1.75f, 0.17f),
            new Vector3(0.045f, 0.045f, 0.025f), dark);
    }

    private static void Part(string name, PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    private static Material GetMaterial(string name, Color color)
    {
        const string folder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Text MakeText(string name, Transform parent, string content, int size, Color color, TextAnchor anchor)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.text = content;
        text.raycastTarget = false;
        return text;
    }

    private static Button MakeButton(string name, Transform parent, Vector2 size, Vector2 position, string label, int textSize)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        Center(obj.GetComponent<RectTransform>(), size, position);
        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.16f, 0.25f, 0.32f, 1f);
        Button button = obj.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.45f, 0.56f, 1f);
        colors.pressedColor = new Color(0.34f, 0.57f, 0.67f, 1f);
        button.colors = colors;
        Text text = MakeText("Label", obj.transform, label, textSize, Color.white, TextAnchor.MiddleLeft);
        Center(text.rectTransform, size - new Vector2(36f, 0f), Vector2.zero);
        return button;
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
