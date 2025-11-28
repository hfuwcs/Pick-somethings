using UnityEngine;

public enum ComponentType
{
    Source, Resistor, Bulb, Switch, Ammeter, Voltmeter, WireNode
}

public class SchematicIcon : MonoBehaviour
{
    [Header("2D Representation")]
    public ComponentType type;
    public Sprite symbol;
    

    public float GetRotation()
    {
        float yRot = transform.eulerAngles.y;
        return Mathf.Round(yRot / 90) * 90;
    }
}