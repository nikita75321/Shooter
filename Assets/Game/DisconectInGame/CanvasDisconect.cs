using DG.Tweening;
using TMPro;
using UnityEngine;

public class CanvasDisconect : MonoBehaviour
{
    public static CanvasDisconect canvasDisconect;

    [SerializeField] private TMP_Text disconectTXT;

    public void StartReconect()
    {
        DOVirtual.Int(0, 10, 10, (t) => Debug.Log(t));
    }
}
