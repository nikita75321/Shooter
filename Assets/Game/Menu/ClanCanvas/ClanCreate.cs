using TMPro;
using UnityEngine;

public class ClanCreate : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject createPanel;
    [SerializeField] private GameObject clanPanel;
    [SerializeField] private TMP_InputField clanNameInput;
    [SerializeField] private TMP_Text needRatingText;
    [SerializeField] private TMP_Text privacyText;
    // [SerializeField] private GameObject loadingIndicator;
    // [SerializeField] private TMP_Text errorText;

    [Header("Settings")]
    [SerializeField] private int minNeedRating = 0;
    [SerializeField] private int maxNeedRating = 1000;
    [SerializeField] private int ratingStep = 100;
    [SerializeField] private int creationPrice = 15;

    [SerializeField] private int currentNeedRating = 0;
    private bool isClosed = false;

    private void OnEnable()
    {
        WebSocketBase.Instance.OnClanCreated += HandleClanCreated;
    }

    private void OnDisable()
    {
        if (WebSocketBase.Instance != null)
        {
            WebSocketBase.Instance.OnClanCreated -= HandleClanCreated;
        }
    }

    private void Start()
    {
        ResetForm();
    }

    private void ResetForm()
    {
        clanNameInput.text = "";
        currentNeedRating = minNeedRating;
        isClosed = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        needRatingText.text = currentNeedRating.ToString();
        privacyText.text = isClosed ? "Закрытый" : "Открытый";
        // errorText.text = "";
    }

    public void IncreaseRating()
    {
        currentNeedRating = Mathf.Min(currentNeedRating + ratingStep, maxNeedRating);
        UpdateUI();
    }

    public void DecreaseRating()
    {
        currentNeedRating = Mathf.Max(currentNeedRating - ratingStep, minNeedRating);
        UpdateUI();
    }

    public void TogglePrivacy()
    {
        isClosed = !isClosed;
        UpdateUI();
    }

    public void TryCreateClan()
    {
        string clanName = clanNameInput.text.Trim();
        string leaderName = Geekplay.Instance.PlayerData.name; // Получаем имя игрока

        // Валидация
        if (string.IsNullOrWhiteSpace(clanName))
        {
            ShowError("Название клана не может быть пустым!");
            return;
        }

        if (clanName.Length < 3)
        {
            ShowError("Название слишком короткое (мин. 3 символа)");
            return;
        }

        // Проверка рейтинга игрока
        // if (Geekplay.Instance.PlayerData.rate < currentNeedRating)
        // {
        //     ShowError($"Ваш рейтинг слишком низкий! Требуется: {currentNeedRating}");
        //     return;
        // }

        if (Currency.Instance.SpendDonatMoney(creationPrice))
        {
            CreateClan(clanName, leaderName);
        }
        else
        {
            ShowError("Недостаточно валюты для создания клана");
        }
    }

    private void CreateClan(string clanName, string leaderName)
    {
        // Отправляем запрос на сервер
        WebSocketBase.Instance.CreateClan(
            clanName: clanName,
            leaderName: leaderName,
            needRating: currentNeedRating,
            isOpen: !isClosed
        );

    }

    private void HandleClanCreated(string clanId, string clanName)
    {
        // loadingIndicator.SetActive(false);
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (!string.IsNullOrEmpty(clanId))
            {
                // Успешное создание
                Geekplay.Instance.PlayerData.clanId = clanId;
                Geekplay.Instance.PlayerData.clanName = clanName;
                Geekplay.Instance.PlayerData.isClanLeader = true; // Помечаем как лидера
                Geekplay.Instance.Save();

                // Переключаем UI
                createPanel.SetActive(false);
                clanPanel.SetActive(true);
                ClanCanvas.Instance.ShowClanPanel();
            }
            else
            {
                // Возвращаем валюту при ошибке
                Currency.Instance.AddDonatMoney(creationPrice);
                ShowError("Ошибка создания клана. Попробуйте позже.");
            }
        });
    }

    private void ShowError(string message)
    {
        // errorText.text = message;
        Debug.Log(message);
    }
}