using System.Collections;
using TMPro;
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
        cor = StartCoroutine(StartTimer());
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

    private void OnDisable()
    {
        if (cor != null)
        {
            txt.text = $"Поиск игроков ({0}с)";
            StopCoroutine(cor);
        }
    }
}
