using UnityEngine;

/// <summary>
/// Interface định nghĩa một "strategy" cho việc cầm/giữ một đối tượng.
/// Bất kỳ class nào triển khai interface này sẽ cung cấp một logic cụ thể
/// về cách đối tượng di chuyển theo tay người chơi.
/// </summary>
public interface IHoldStrategy
{
    /// <summary>
    /// Thực thi logic di chuyển cho đối tượng được giữ.
    /// </summary>
    /// <param name="heldBody">Rigidbody của đối tượng đang được giữ.</param>
    /// <param name="grabberTransform">Transform của điểm cầm nắm trên tay người chơi.</param>
    /// <param name="centerOfMassTransform">Transform của khối tâm vật lý của đối tượng.</param>
    void Hold(Rigidbody heldBody, Transform grabberTransform, Transform centerOfMassTransform);
}