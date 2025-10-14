using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform cameraPos;
    [SerializeField] private float followSpeed = 10f;

    private void LateUpdate()
    {
        if (cameraPos == null) return;

        transform.position = Vector3.Lerp(transform.position, cameraPos.position, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, cameraPos.rotation, followSpeed * Time.deltaTime);
    }
}
