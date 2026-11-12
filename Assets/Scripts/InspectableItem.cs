using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class InspectableItem : MonoBehaviour
{
    [SerializeField] private string itemName = "Предмет";

    public string ItemName => itemName;
}
