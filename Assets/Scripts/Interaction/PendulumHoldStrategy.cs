using UnityEngine;

public class PendulumHoldStrategy : IHoldStrategy
{
    private readonly Transform _pivotPoint;
    private readonly float _length;

    /// <summary>
    /// Khởi tạo chiến lược với các tham số ràng buộc của con lắc.
    /// </summary>
    /// <param name="pivot">Transform của điểm treo.</param>
    /// <param name="length">Chiều dài của con lắc.</param>
    public PendulumHoldStrategy(Transform pivot, float length)
    {
        _pivotPoint = pivot;
        _length = length;
    }

    public void Hold(Rigidbody heldBody, Transform grabberTransform, Transform centerOfMassTransform)
    {
        Vector3 grabberPosition = grabberTransform.position;
        Vector3 pivotPosition = _pivotPoint.position;

        Vector3 directionToGrabber = grabberPosition - pivotPosition;
        Vector3 projectedDirection = Vector3.ProjectOnPlane(directionToGrabber, _pivotPoint.forward);


        if (projectedDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 targetModelPosition = pivotPosition + projectedDirection.normalized * _length;

        Vector3 offset = heldBody.transform.position - centerOfMassTransform.position;
        Vector3 targetRootPosition = targetModelPosition + offset;

        heldBody.MovePosition(targetRootPosition);
    }
}