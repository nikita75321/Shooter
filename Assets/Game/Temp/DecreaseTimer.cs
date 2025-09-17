using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class DecreaseTimer : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds1 = new(1);
    [SerializeField] private TMP_Text txt;
    [SerializeField] private int startTime;
    private int tempTime = 0;
    private Coroutine cor;

    private void OnEnable()
    {
        tempTime = startTime;
        WebSocketBase.Instance.OnMatchStart += StartTimer;
    }

    private void OnDisable()
    {
        if (cor != null)
        {
            StopCoroutine(cor);
        }

        WebSocketBase.Instance.OnMatchStart -= StartTimer;
    }

    private void StartTimer(MatchStartResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            cor = StartCoroutine(StartTimer());
        });
    }

    private IEnumerator StartTimer()
    {
        while (tempTime > 0)
        {
            yield return _waitForSeconds1;
            tempTime--;

            int minutes = tempTime / 60;
            int seconds = tempTime % 60;

            txt.text = $"{minutes:00}:{seconds:00}";
        }
        Debug.Log("Time is end");
    }
}