using UnityEngine;

public interface IHoldStrategy
{
    void InitializeGrab(Transform grabberTransform, Rigidbody heldBody);
    void Hold(Rigidbody heldBody, Transform grabberTransform, Transform centerOfMassTransform);    
    //void Rotate(Vector2 rotationDelta, float rotationSpeed);
}