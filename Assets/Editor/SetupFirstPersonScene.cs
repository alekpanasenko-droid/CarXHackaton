using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupFirstPersonScene
{
    [MenuItem("Tools/Setup First Person Demo Scene")]
    public static void Setup()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        GameObject player = GameObject.Find("Player");
        if (player == null) player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.05f, 0f);

        var controller = player.GetComponent<CharacterController>();
        if (controller == null) controller = player.AddComponent<CharacterController>();
        controller.radius = 0.3f;
        controller.height = 1.8f;
        controller.center = Vector3.up * 0.9f;

        var source = player.GetComponent<AudioSource>();
        if (source == null) source = player.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }
        camera.transform.SetParent(player.transform, false);
        camera.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        camera.transform.localRotation = Quaternion.identity;

        var movement = player.GetComponent<FirstPersonPlayer>();
        if (movement == null) movement = player.AddComponent<FirstPersonPlayer>();
        var serializedMovement = new SerializedObject(movement);
        serializedMovement.FindProperty("playerCamera").objectReferenceValue = camera;
        serializedMovement.FindProperty("footstepSource").objectReferenceValue = source;
        serializedMovement.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(movement);

        GameObject floor = GameObject.Find("Test Floor");
        if (floor == null) floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Test Floor";
        floor.transform.position = new Vector3(0f, -0.25f, 0f);
        floor.transform.localScale = new Vector3(20f, 0.5f, 20f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("First person demo scene is ready. Assign AudioClip assets in the Player inspector for footsteps.");
    }
}
