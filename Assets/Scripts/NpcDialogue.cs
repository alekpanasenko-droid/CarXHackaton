using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class NpcDialogue : MonoBehaviour
{
    [Serializable]
    public sealed class Choice
    {
        [TextArea(1, 2)] public string text;
        public int nextNode = -1;
    }

    [Serializable]
    public sealed class Node
    {
        [TextArea(2, 5)] public string npcText;
        public Choice[] choices;
    }

    [SerializeField] private string characterName = "Смотритель";
    [SerializeField] private Node[] nodes;

    public string CharacterName => characterName;
    public Node[] Nodes => nodes;
}
