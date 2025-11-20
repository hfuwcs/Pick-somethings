using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TheoryDisplayManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject pagePrefab;
    [SerializeField] private ScrollRect scrollRect;

    public void DisplayDownloadedLesson(List<Texture2D> textures)
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        foreach (Texture2D tex in textures)
        {
            GameObject newPage = Instantiate(pagePrefab, contentContainer);
            
            RawImage rawImg = newPage.GetComponent<RawImage>();
            if (rawImg != null)
            {
                rawImg.texture = tex;
                
                AspectRatioFitter fitter = newPage.GetComponent<AspectRatioFitter>();
                if (fitter != null)
                {
                    fitter.aspectRatio = (float)tex.width / tex.height;
                }
            }
        }

        scrollRect.verticalNormalizedPosition = 1f;
    }
}