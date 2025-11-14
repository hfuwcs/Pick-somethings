public interface ISnapReceiver
{
    /// <summary>
    /// Được gọi bởi SnapZone khi một Grabbable có chứa component này
    /// hoàn tất một kết nối DirectConnection.
    /// </summary>
    /// <param name="snappedConnector">Connector vừa được kết nối.</param>
    void OnSnapConnection(Connector snappedConnector);

    /// <summary>
    /// Được gọi bởi SnapZone khi một Grabbable có chứa component này
    /// bị ngắt kết nối khỏi một DirectConnection.
    /// </summary>
    /// <param name="disconnectedConnector">Connector vừa bị ngắt kết nối.</param>
    void OnSnapDisconnection(Connector disconnectedConnector);
}