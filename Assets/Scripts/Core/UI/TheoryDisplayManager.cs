using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class TheoryDisplayManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject pagePrefab;
    [SerializeField] private ScrollRect scrollRect;
    public void DisplayDownloadedLesson(List<Texture2D> textures)
    {
        StartCoroutine(DisplayRoutine(textures));
    }

    private IEnumerator DisplayRoutine(List<Texture2D> textures)
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        yield return null;

        foreach (Texture2D tex in textures)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            GameObject newPage = Instantiate(pagePrefab, contentContainer);
            
            RawImage rawImg = newPage.GetComponent<RawImage>();
            if (rawImg != null) rawImg.texture = tex;

            AspectRatioFitter fitter = newPage.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                float ratio = (float)tex.width / (float)tex.height;
                fitter.aspectRatio = ratio;
                
                Debug.Log($"Load ảnh {tex.width}x{tex.height}. Ratio: {ratio}");
            }
        }

        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer.GetComponent<RectTransform>());
        
        scrollRect.verticalNormalizedPosition = 1f;
    }
}