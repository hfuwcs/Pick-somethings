using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Experiment/Spawnable Item")]
public class SpawnableItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab;
    [TextArea] public string description;
}