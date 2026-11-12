using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ReadableDocument : MonoBehaviour
{
    [SerializeField] private string documentTitle = "Записка";
    [SerializeField, TextArea(5, 20)] private string documentText = "Текст документа.";

    public string Title => documentTitle;
    public string Text => documentText;
}
