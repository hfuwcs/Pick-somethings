using UnityEngine;

public class TrashCan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Grabbable item = other.GetComponent<Grabbable>();
        
        if (item != null)
        {
            if (item.CurrentState == GrabbableState.Grabbed) return;

            Debug.Log($"Đã xóa: {other.name}");

            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.RemoveItem(other.gameObject);
            }
            else
            {
                Destroy(other.gameObject);
            }
            
            if (ExperimentNotification.Instance != null)
                ExperimentNotification.Instance.Show("Đã xóa", "Vật phẩm đã bị hủy", ExperimentNotification.Type.Info);
        }
    }
}