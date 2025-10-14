using UnityEngine;

public class PlayerLookSync : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    private void Update()
    {
        float cameraYaw = cameraTransform.rotation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, cameraYaw, 0);
    }
}
