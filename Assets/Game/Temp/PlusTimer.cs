using System.Collections;
using TMPro;
using UnityEngine;

public class PlusTimer : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds1 = new(1);
    [SerializeField] private TMP_Text txt;
    [SerializeField] private int time;
    private Coroutine cor;

    private void OnEnable()
    {
        time = 0;
        cor = StartCoroutine(StartTimer());
    }

    private IEnumerator StartTimer()
    {
        while (true)
        {
            yield return _waitForSeconds1;
            time++;
            txt.text = $"Поиск игроков ({time}с)";
        }
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
