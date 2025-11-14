using UnityEngine;

public interface IMultiPointSnappable
{
    /// <summary>
    /// Thực hiện hành động kết nối một connector cụ thể vào một snap zone.
    /// Logic này sẽ chỉ tạo một khớp nối vật lý tại điểm đó.
    /// </summary>
    /// <param name="connector">Connector đang thực hiện kết nối.</param>
    /// <param name="snapZone">SnapZone mà nó đang kết nối vào.</param>
    void SnapPoint(Connector connector, SnapZone snapZone);

    /// <summary>
    /// Thực hiện hành động ngắt kết nối một connector cụ thể.
    /// </summary>
    /// <param name="connector">Connector đang thực hiện ngắt kết nối.</param>
    void UnsnapPoint(Connector connector);
}