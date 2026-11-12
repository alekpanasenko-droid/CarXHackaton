using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ItemInspector : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FirstPersonPlayer movement;
    [SerializeField] private DocumentReader documentReader;
    [SerializeField, Min(0.1f)] private float inspectionDistance = 3.5f;
    [SerializeField, Min(0.01f)] private float rotationSensitivity = 0.3f;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Text promptText;
    [SerializeField] private GameObject inspectionPanel;
    [SerializeField] private Text itemTitle;
    [SerializeField] private RawImage previewImage;

    public bool IsInspecting => inspectedItem != null;

    private const int PreviewLayer = 31;
    private static readonly Vector3 PreviewPosition = new Vector3(2000f, 2000f, 2000f);

    private InspectableItem inspectedItem;
    private GameObject previewRoot;
    private Camera previewCamera;
    private RenderTexture previewTexture;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (movement == null) movement = GetComponent<FirstPersonPlayer>();
        if (documentReader == null) documentReader = GetComponent<DocumentReader>();
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (inspectionPanel != null) inspectionPanel.SetActive(false);

        previewTexture = new RenderTexture(640, 640, 16) { name = "Item Inspection Preview" };
        previewTexture.Create();
        if (previewImage != null) previewImage.texture = previewTexture;

        var cameraObject = new GameObject("Inspection Preview Camera");
        cameraObject.transform.position = PreviewPosition + Vector3.back * 6f;
        cameraObject.transform.LookAt(PreviewPosition);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.11f, 0.14f, 0.19f, 1f);
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 1.5f;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.targetTexture = previewTexture;
        previewCamera.enabled = false;
    }

    private void Update()
    {
        if (Keyboard.current == null || playerCamera == null) return;

        bool pressedE = Keyboard.current.eKey.wasPressedThisFrame;
        if (inspectedItem != null)
        {
            if (pressedE || Keyboard.current.escapeKey.wasPressedThisFrame) CloseInspection();
            else RotatePreview();
            return;
        }

        if (documentReader != null && documentReader.IsReading)
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            return;
        }

        InspectableItem target = FindItem();
        if (interactionPrompt != null) interactionPrompt.SetActive(target != null);
        if (target != null && promptText != null) promptText.text = "[E] Осмотреть — " + target.ItemName;
        if (pressedE && target != null) OpenInspection(target);
    }

    private InspectableItem FindItem()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, inspectionDistance, ~0, QueryTriggerInteraction.Ignore))
            return null;
        return hit.collider.GetComponentInParent<InspectableItem>();
    }

    private void OpenInspection(InspectableItem item)
    {
        inspectedItem = item;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (itemTitle != null) itemTitle.text = item.ItemName;

        previewRoot = Instantiate(item.gameObject);
        previewRoot.name = "Inspection Preview Model";
        previewRoot.transform.SetPositionAndRotation(PreviewPosition, Quaternion.identity);
        SetPreviewLayer(previewRoot.transform);
        foreach (Collider collider in previewRoot.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (MonoBehaviour behaviour in previewRoot.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        foreach (Rigidbody body in previewRoot.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        foreach (AudioSource source in previewRoot.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
        foreach (Light light in previewRoot.GetComponentsInChildren<Light>(true)) light.enabled = false;

        Renderer[] renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            previewRoot.transform.position += PreviewPosition - bounds.center;
            previewCamera.orthographicSize = Mathf.Max(0.6f, Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.6f);
        }
        previewRoot.transform.rotation = Quaternion.Euler(15f, -25f, 0f);

        previewCamera.enabled = true;
        if (inspectionPanel != null) inspectionPanel.SetActive(true);
        if (movement != null) movement.enabled = false;
    }

    private void RotatePreview()
    {
        if (Mouse.current == null || previewRoot == null) return;
        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * rotationSensitivity;
            previewRoot.transform.Rotate(Vector3.up, -delta.x, Space.World);
            previewRoot.transform.Rotate(previewCamera.transform.right, delta.y, Space.World);
        }

        float wheel = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(wheel) > 0.01f)
            previewCamera.orthographicSize = Mathf.Clamp(previewCamera.orthographicSize - wheel * 0.002f, 0.4f, 6f);
    }

    private static void SetPreviewLayer(Transform root)
    {
        root.gameObject.layer = PreviewLayer;
        foreach (Transform child in root) SetPreviewLayer(child);
    }

    private void CloseInspection()
    {
        inspectedItem = null;
        if (inspectionPanel != null) inspectionPanel.SetActive(false);
        if (previewCamera != null) previewCamera.enabled = false;
        if (previewRoot != null) Destroy(previewRoot);
        previewRoot = null;
        if (movement != null) movement.enabled = true;
    }

    private void OnDisable()
    {
        if (inspectedItem != null) CloseInspection();
    }

    private void OnDestroy()
    {
        if (previewCamera != null) Destroy(previewCamera.gameObject);
        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }
    }
}
