using System.Net.WebSockets;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class CanvasDisconect : MonoBehaviour
{
    public static CanvasDisconect Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text disconectTXT;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        WebSocketBase.Instance.OnClose += StartReconect;
        InitSocket.socketConnected += StopReconect;
    }
    private void OnDisable()
    {
        WebSocketBase.Instance.OnClose -= StartReconect;
        InitSocket.socketConnected -= StopReconect;
    }

    public void StartReconect()
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log("StartReconect");
            panel.SetActive(true);
            InitSocket.Instance.ManualReconnect();
            if (InitSocket.ISCONECTED) return;

            var text = disconectTXT.text;
            DOVirtual.Int(0, 10, 10, t =>
            {
                int dotsCount = t % 6; // от 0 до 5
                disconectTXT.text = "Подключение" + new string('.', dotsCount);
            })
            .SetEase(Ease.Linear);
        });
    }

    private void StopReconect(bool status)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log($"StopReconect status={status}");
            if (status)
            {
                
                panel.SetActive(false);
            }
        });
    }
}