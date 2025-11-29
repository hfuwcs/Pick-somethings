using UnityEngine;

public class FreeHoldStrategy : IHoldStrategy
{
    public void InitializeGrab(Transform grabberTransform, Rigidbody heldBody) 
    {
    }
    public void Hold(Rigidbody heldBody, Transform grabberTransform, Transform centerOfMassTransform)
    {
        Vector3 targetPosition;
        Transform bodyTransform = heldBody.transform;

        if (centerOfMassTransform != bodyTransform)
        {
            Vector3 offset = bodyTransform.position - centerOfMassTransform.position;
            targetPosition = grabberTransform.position + offset;
        }
        else
        {
            targetPosition = grabberTransform.position;
        }

        Vector3 currentPosition = heldBody.position;
        Vector3 movementVector = targetPosition - currentPosition;
        float distance = movementVector.magnitude;

        if (distance <= 0.001f) return;

        if (heldBody.SweepTest(movementVector.normalized, out RaycastHit hitInfo, distance, QueryTriggerInteraction.Ignore))
        {
            heldBody.MovePosition(currentPosition + movementVector.normalized * hitInfo.distance);
        }
        else
        {
            heldBody.MovePosition(targetPosition);
        }
    }
}