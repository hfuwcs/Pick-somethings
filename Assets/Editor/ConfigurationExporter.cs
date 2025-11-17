using UnityEngine;
using UnityEditor;
using System.Text;

public static class ConfigurationExporter
{
    [MenuItem("CONTEXT/Transform/Copy Full Configuration to Clipboard")]
    private static void CopyConfiguration(MenuCommand command)
    {
        Transform targetTransform = (Transform)command.context;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"--- CONFIGURATION FOR: {targetTransform.gameObject.name} ---");
        sb.AppendLine($"Layer: {LayerMask.LayerToName(targetTransform.gameObject.layer)}");
        sb.AppendLine($"Tag: {targetTransform.gameObject.tag}");
        sb.AppendLine();

        AppendComponentsInfo(targetTransform.gameObject, sb, "");

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log($"Configuration for '{targetTransform.gameObject.name}' has been copied to the clipboard.");
    }

    private static void AppendComponentsInfo(GameObject go, StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}[GameObject: {go.name}]");

        Component[] components = go.GetComponents<Component>();
        foreach (var component in components)
        {
            if (component == null) continue;
            sb.AppendLine($"{indent}  - Component: {component.GetType().Name}");
            
            // In ra các thông tin quan trọng của một số component thường gặp
            switch (component)
            {
                case Rigidbody rb:
                    sb.AppendLine($"{indent}    - Use Gravity: {rb.useGravity}, Is Kinematic: {rb.isKinematic}");
                    break;
                case Collider col:
                    sb.AppendLine($"{indent}    - Is Trigger: {col.isTrigger}");
                    break;
            }
        }

        // Đệ quy cho các đối tượng con
        foreach (Transform child in go.transform)
        {
            AppendComponentsInfo(child.gameObject, sb, indent + "  ");
        }
    }
}