using UnityEngine;

public class PhysicsHoldStrategy : IHoldStrategy
{
    private const float MAX_VELOCITY = 50f;
    private const float POS_STRENGTH = 4000f;
    private const float ROT_STRENGTH = 200f;
    
    private Vector3 _localAnchorPosition;
    private Quaternion _localAnchorRotation;

    public void InitializeGrab(Transform grabberTransform, Rigidbody heldBody)
    {

        _localAnchorPosition = grabberTransform.InverseTransformPoint(heldBody.position);
        _localAnchorRotation = Quaternion.Inverse(grabberTransform.rotation) * heldBody.rotation;
    }

    public void Hold(Rigidbody heldBody, Transform grabberTransform, Transform centerOfMassTransform)
    {
        float dt = Time.fixedDeltaTime;

        Vector3 targetPosition = grabberTransform.TransformPoint(_localAnchorPosition);
        Vector3 positionDelta = targetPosition - heldBody.position;
        Vector3 targetVelocity = positionDelta * POS_STRENGTH * dt;
        if (targetVelocity.sqrMagnitude > MAX_VELOCITY * MAX_VELOCITY)
        {
            targetVelocity = targetVelocity.normalized * MAX_VELOCITY;
        }
        
        heldBody.linearVelocity = Vector3.Lerp(heldBody.linearVelocity, targetVelocity, 0.5f);
        Quaternion targetRotation = grabberTransform.rotation * _localAnchorRotation;
        Quaternion deltaRot = targetRotation * Quaternion.Inverse(heldBody.rotation);
        
        float angle;
        Vector3 axis;
        deltaRot.ToAngleAxis(out angle, out axis);
        
        if (angle > 180f) angle -= 360f;
        
        if (Mathf.Abs(angle) > 0.01f)
        {
            Vector3 angularTarget = axis * (angle * Mathf.Deg2Rad * ROT_STRENGTH);
            heldBody.angularVelocity = Vector3.Lerp(heldBody.angularVelocity, angularTarget, 0.5f);
        }
    }
}