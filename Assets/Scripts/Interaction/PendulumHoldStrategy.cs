using UnityEngine;

public class PendulumHoldStrategy : IHoldStrategy
{
    private readonly Transform _pivotPoint;
    private readonly float _length;
    private readonly float _maxAngleDegrees;
    private readonly bool _enforceAngleLimit;
    private readonly Vector3 _rotationAxis;
    public PendulumHoldStrategy(Transform pivot, float length, float maxAngleDegrees = 90f, bool enforceAngleLimit = false, Vector3 rotationAxis = default)
    {
        _pivotPoint = pivot;
        _length = length;
        _maxAngleDegrees = maxAngleDegrees;
        _enforceAngleLimit = enforceAngleLimit;
        _rotationAxis = rotationAxis == default ? pivot.forward : rotationAxis;
    }
    public void InitializeGrab(Transform grabberTransform, Rigidbody heldBody)
    {
    }
    public void Hold(Rigidbody heldBody, Transform grabberTransform, Transform centerOfMassTransform)
    {
        Vector3 grabberPosition = grabberTransform.position;
        Vector3 pivotPosition = _pivotPoint.position;
        Vector3 directionToGrabber = grabberPosition - pivotPosition;

        Vector3 projectedDirection = Vector3.ProjectOnPlane(directionToGrabber, _rotationAxis);

        if (projectedDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 desiredDirection = projectedDirection.normalized;

        if (_enforceAngleLimit)
        {
            float angleFromDown = Vector3.Angle(Vector3.down, desiredDirection);
            if (angleFromDown > _maxAngleDegrees)
            {
                Vector3 axis = Vector3.Cross(Vector3.down, desiredDirection).normalized;
                if (axis.sqrMagnitude < 0.001f)
                {
                    axis = _rotationAxis;
                }
                desiredDirection = Quaternion.AngleAxis(_maxAngleDegrees, axis) * Vector3.down;
            }
        }

        Vector3 targetModelPosition = pivotPosition + desiredDirection * _length;
        Vector3 offset = heldBody.transform.position - centerOfMassTransform.position;
        Vector3 targetRootPosition = targetModelPosition + offset;
        heldBody.MovePosition(targetRootPosition);
    }
}