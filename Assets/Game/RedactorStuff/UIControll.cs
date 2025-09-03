using UnityEngine;
using UnityEngine.UI;
// using Sirenix.OdinInspector;

public class UIControll : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasScaler canvasScaler;

    [SerializeField] private Vector2 resolutionPC = new Vector2(1920, 1080);
    // [Button("PC")]
    [ContextMenu("PC")]
    private void SetPC()
    {
        canvasScaler.referenceResolution = resolutionPC;
    }
    
    [SerializeField] private Vector2 resolutionMobile = new Vector2(1080, 1920);
    // [Button("Mobile")]
    [ContextMenu("Mobile")]
    private void SetMobile()
    {
        canvasScaler.referenceResolution = resolutionMobile;
    }

    private void OnValidate()
    {
        canvas = canvas != null ? canvas : GetComponentInChildren<Canvas>();
        canvasScaler = canvasScaler != null ? canvasScaler : GetComponentInChildren<CanvasScaler>();
    }

    private void Start()
    {
        // if(Geekplay.Instance != null)
        // {
        //     if (Geekplay.Instance.Mobile)
        //     {
        //         canvasScaler.referenceResolution = resolutionMobile;
        //     }
        //     else
        //     {
        //         canvasScaler.referenceResolution = resolutionPC;
        //     }
        // }
    }
}