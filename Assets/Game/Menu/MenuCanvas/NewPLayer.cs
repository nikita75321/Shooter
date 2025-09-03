using TMPro;
using UnityEngine;

public class NewPlayer : MonoBehaviour
{
    [SerializeField] private GameObject newPlayerPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private string playerName;
    [SerializeField] private TMP_Text nameTXT;

    private void Start()
    {
        inputField.onValueChanged.AddListener(OnValueChanged);
        WebSocketBase.Instance.OnPlayerRegister += NewPlayerFinal;

        if (!string.IsNullOrEmpty(Geekplay.Instance.PlayerData.name))
        {
            newPlayerPanel.SetActive(false);
        }
        else
        {
            newPlayerPanel.SetActive(true);
        }
    }

    public void ChangeName()
    {
        playerName = inputField.text;
    }

    public void RegisterNewPlayer()
    {
        if (WebSocketBase.Instance == null)
        {
            Debug.LogError("WebSocketBase instance is not initialized!");
            return;
        }

        WebSocketBase.Instance.RegisterPlayer(playerName);
    }

    private void NewPlayerFinal()
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Geekplay.Instance.PlayerData.name = playerName;
            Geekplay.Instance.Save();

            newPlayerPanel.SetActive(false);
            nameTXT.text = playerName;
            // Можно добавить логирование
            Debug.Log($"Player registered with name: {playerName}");
        });
    }

    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(OnValueChanged);
        // Отписываемся от события при уничтожении объекта
        if (WebSocketBase.Instance != null)
        {
            WebSocketBase.Instance.OnPlayerRegister -= NewPlayerFinal;
        }
    }
    private void OnValueChanged(string text)
    {
        // Оставляем только допустимые символы
        string filteredText = System.Text.RegularExpressions.Regex.Replace(
        text, 
        @"[^a-zA-Zа-яА-ЯёЁ0-9 ]", 
        "");
        
        if (filteredText != text)
        {
            inputField.text = filteredText;
        }
    }
}