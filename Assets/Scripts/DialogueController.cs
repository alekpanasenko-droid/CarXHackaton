using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class DialogueController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera dialogueCamera;
    [SerializeField] private NpcDialogue npc;
    [SerializeField] private FirstPersonPlayer movement;
    [SerializeField] private DocumentReader documentReader;
    [SerializeField] private ItemInspector itemInspector;
    [SerializeField] private GameObject playerBody;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject[] otherPrompts;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Button continueButton;
    [SerializeField, Min(0.1f)] private float talkDistance = 2.7f;

    private NpcDialogue activeNpc;
    private NpcDialogue nearbyNpc;
    private int nodeIndex;
    private int pendingNextNode;
    private bool showingPlayerReply;
    private bool movementWasEnabled;
    private bool readerWasEnabled;
    private bool inspectorWasEnabled;
    private bool crosshairWasActive;
    private bool[] otherPromptsWereActive;
    private Vector3 shotPosition;
    private Quaternion shotRotation;

    public bool IsTalking => activeNpc != null;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (movement == null) movement = GetComponent<FirstPersonPlayer>();
        if (documentReader == null) documentReader = GetComponent<DocumentReader>();
        if (itemInspector == null) itemInspector = GetComponent<ItemInspector>();
        if (dialogueCamera != null) dialogueCamera.enabled = false;
        if (playerBody != null) playerBody.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choiceButtons != null)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i;
                if (choiceButtons[i] != null) choiceButtons[i].onClick.AddListener(() => SelectChoice(index));
            }
        }
        if (continueButton != null) continueButton.onClick.AddListener(ContinueDialogue);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (activeNpc == null)
        {
            nearbyNpc = FindNearbyNpc();
            bool canTalk = nearbyNpc != null &&
                (documentReader == null || !documentReader.IsReading) &&
                (itemInspector == null || !itemInspector.IsInspecting);
            if (interactionPrompt != null) interactionPrompt.SetActive(canTalk);
            if (canTalk && Keyboard.current.eKey.wasPressedThisFrame) BeginDialogue(nearbyNpc);
            return;
        }

        if (dialogueCamera != null)
        {
            float t = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
            dialogueCamera.transform.position = Vector3.Lerp(dialogueCamera.transform.position, shotPosition, t);
            dialogueCamera.transform.rotation = Quaternion.Slerp(dialogueCamera.transform.rotation, shotRotation, t);
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame) EndDialogue();
        else if (showingPlayerReply &&
                 (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            ContinueDialogue();
        else if (!showingPlayerReply)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectChoice(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectChoice(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectChoice(2);
        }
    }

    private NpcDialogue FindNearbyNpc()
    {
        if (npc == null || !npc.isActiveAndEnabled) return null;
        Vector3 delta = npc.transform.position - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= talkDistance * talkDistance ? npc : null;
    }

    private void BeginDialogue(NpcDialogue npc)
    {
        if (npc.Nodes == null || npc.Nodes.Length == 0 || dialogueCamera == null) return;
        activeNpc = npc;
        movementWasEnabled = movement != null && movement.enabled;
        readerWasEnabled = documentReader != null && documentReader.enabled;
        inspectorWasEnabled = itemInspector != null && itemInspector.enabled;
        if (movement != null) movement.enabled = false;
        if (documentReader != null) documentReader.enabled = false;
        if (itemInspector != null) itemInspector.enabled = false;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (otherPrompts != null)
        {
            otherPromptsWereActive = new bool[otherPrompts.Length];
            for (int i = 0; i < otherPrompts.Length; i++)
            {
                if (otherPrompts[i] == null) continue;
                otherPromptsWereActive[i] = otherPrompts[i].activeSelf;
                otherPrompts[i].SetActive(false);
            }
        }
        if (playerBody != null)
        {
            Vector3 towardNpc = npc.transform.position - transform.position;
            towardNpc.y = 0f;
            if (towardNpc.sqrMagnitude > 0.001f)
                playerBody.transform.rotation = Quaternion.LookRotation(towardNpc);
            playerBody.SetActive(true);
        }
        crosshairWasActive = crosshair != null && crosshair.activeSelf;
        if (crosshair != null) crosshair.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (playerCamera != null) playerCamera.enabled = false;
        dialogueCamera.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        nodeIndex = 0;
        ShowNpcNode(true);
    }

    private void ShowNpcNode(bool snapCamera = false)
    {
        if (nodeIndex < 0 || nodeIndex >= activeNpc.Nodes.Length)
        {
            EndDialogue();
            return;
        }
        showingPlayerReply = false;
        NpcDialogue.Node node = activeNpc.Nodes[nodeIndex];
        if (speakerText != null) speakerText.text = activeNpc.CharacterName;
        if (dialogueText != null) dialogueText.text = node.npcText;
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i];
            if (button == null) continue;
            bool show = node.choices != null && i < node.choices.Length;
            button.gameObject.SetActive(show);
            if (show) button.GetComponentInChildren<Text>().text = (i + 1) + ". " + node.choices[i].text;
        }
        SetShot(false, snapCamera);
    }

    private void SelectChoice(int index)
    {
        if (activeNpc == null || showingPlayerReply) return;
        NpcDialogue.Choice[] choices = activeNpc.Nodes[nodeIndex].choices;
        if (choices == null || index < 0 || index >= choices.Length) return;
        NpcDialogue.Choice choice = choices[index];
        pendingNextNode = choice.nextNode;
        showingPlayerReply = true;
        if (speakerText != null) speakerText.text = "Вы";
        if (dialogueText != null) dialogueText.text = choice.text;
        foreach (Button button in choiceButtons)
            if (button != null) button.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(true);
        SetShot(true, false);
    }

    private void ContinueDialogue()
    {
        if (activeNpc == null || !showingPlayerReply) return;
        if (pendingNextNode < 0 || pendingNextNode >= activeNpc.Nodes.Length)
        {
            EndDialogue();
            return;
        }
        nodeIndex = pendingNextNode;
        ShowNpcNode();
    }

    private void SetShot(bool playerSpeaking, bool snap)
    {
        Vector3 direction = activeNpc.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        Vector3 target;
        if (playerSpeaking)
        {
            shotPosition = activeNpc.transform.position + direction * 0.8f - right * 0.65f + Vector3.up * 1.6f;
            target = transform.position + Vector3.up * 1.45f;
        }
        else
        {
            shotPosition = transform.position - direction * 0.8f + right * 0.65f + Vector3.up * 1.6f;
            target = activeNpc.transform.position + Vector3.up * 1.45f;
        }
        shotRotation = Quaternion.LookRotation(target - shotPosition, Vector3.up);
        if (snap) dialogueCamera.transform.SetPositionAndRotation(shotPosition, shotRotation);
    }

    private void EndDialogue()
    {
        activeNpc = null;
        showingPlayerReply = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueCamera != null) dialogueCamera.enabled = false;
        if (playerCamera != null) playerCamera.enabled = true;
        if (playerBody != null) playerBody.SetActive(false);
        if (crosshair != null) crosshair.SetActive(crosshairWasActive);
        if (otherPrompts != null && otherPromptsWereActive != null)
        {
            for (int i = 0; i < otherPrompts.Length; i++)
                if (otherPrompts[i] != null) otherPrompts[i].SetActive(otherPromptsWereActive[i]);
        }
        if (movement != null) movement.enabled = movementWasEnabled;
        if (documentReader != null) documentReader.enabled = readerWasEnabled;
        if (itemInspector != null) itemInspector.enabled = inspectorWasEnabled;
        if (movementWasEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (activeNpc != null) EndDialogue();
    }
}
