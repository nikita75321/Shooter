using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance;

    [Header("Regerencess")]
    [SerializeField] private Skins skins;

    [Header("Change name")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject changeNamePanel;
    [SerializeField] private TMP_Text infoTXT;

    [Header("Main UI")]
    [SerializeField] private TMP_Text playerNameMenu;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text playerId;
    [SerializeField] private TMP_Text favoriteHero;
    [SerializeField] private TMP_Text bestRating;
    [SerializeField] private TMP_Text clan;
    [SerializeField] private TMP_Text overallKills;
    [SerializeField] private TMP_Text matchPlayes;
    [SerializeField] private TMP_Text winCount;
    [SerializeField] private TMP_Text winRate;
    [SerializeField] private TMP_Text reviveAlly;
    [SerializeField] private TMP_Text maxDamageBattle;
    [SerializeField] private TMP_Text totalShots;

    [Header("PlayerLogo")]
    [SerializeField] private Image menuLogo;
    [SerializeField] private Image playerInfoLogo;

    private PlayerData playerData;

    public void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void Start()
    {
        infoTXT.DOFade(0, 0);
        infoTXT.gameObject.SetActive(true);
        // WebSocketBase.Instance.OnNameChecked += HandleNameCheckResult;
        WebSocketBase.Instance.OnNameUpdated += HandleNameUpdateResult;

        // inputField.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        inputField.onValueChanged.AddListener(OnValueChanged);
    }
    private void OnDestroy()
    {
        // WebSocketBase.Instance.OnNameChecked -= HandleNameCheckResult;
        WebSocketBase.Instance.OnNameUpdated -= HandleNameUpdateResult;

        inputField.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void UpdateUI()
    {
        Debug.Log("Update player info");

        playerData = Geekplay.Instance.PlayerData;

        float winRatePercent;

        if (playerData.winOverral + playerData.loseOverral > 0)
        {
            winRatePercent = (float)playerData.winOverral / (playerData.winOverral + playerData.loseOverral) * 100f;
            // Если нужно целое число:
            winRatePercent = Mathf.RoundToInt(winRatePercent);
        }
        else
        {
            winRatePercent = 0;
        }


        var tempName = Geekplay.Instance.PlayerData.name;
        if (!string.IsNullOrEmpty(tempName))
        {
            playerNameMenu.text = tempName;
        }
        else
        {
            playerNameMenu.text = "Null";
        }

        playerName.text = Geekplay.Instance.PlayerData.name;
        playerId.text = Geekplay.Instance.PlayerData.id;

        bestRating.text = $"{playerData.maxRate}";
        if (playerData.clanName == string.Empty || playerData.clanName == null)
        {
            clan.text = $"Нет";
        }
        else
        {
            clan.text = $"{playerData.clanName}";
        }
        overallKills.text = $"{playerData.killOverral}";
        matchPlayes.text = $"{playerData.winOverral + playerData.loseOverral}";
        winCount.text = $"{playerData.winOverral}";
        winRate.text = $"{winRatePercent}%";
        reviveAlly.text = $"{playerData.reviveAlly}";
        maxDamageBattle.text = $"{playerData.maxDamageBattle}";
        totalShots.text = $"{playerData.totalShots}";

        int temp = 0;
        int matchs = 0;
        for (int i = 0; i < Geekplay.Instance.PlayerData.heroMatch.Length; i++)
        {
            int hero = Geekplay.Instance.PlayerData.heroMatch[i];
            if (matchs < hero)
            {
                matchs = hero;
                temp = i;
            }
        }

        switch (temp)
        {
            case 0:
                favoriteHero.text = "любимый герой: <color=yellow>Kayel</color>";
                break;
            case 1:
                favoriteHero.text = "любимый герой: <color=yellow>Coco</color>";
                break;
            case 2:
                favoriteHero.text = "любимый герой: <color=yellow>Bobby</color>";
                break;
            case 3:
                favoriteHero.text = "любимый герой: <color=yellow>Mono</color>";
                break;
            case 4:
                favoriteHero.text = "любимый герой: <color=yellow>Freddy</color>";
                break;
            case 5:
                favoriteHero.text = "любимый герой: <color=yellow>Ci-J</color>";
                break;
            case 6:
                favoriteHero.text = "любимый герой: <color=yellow>Zetta</color>";
                break;
            case 7:
                favoriteHero.text = "любимый герой: <color=yellow>Rambo</color>";
                break;
        }
    }

    public void ChangeName()
    {
        infoTXT.DOFade(0, 0);
        string newName = inputField.text.Trim();

        if (string.IsNullOrEmpty(newName) || newName == "Введите имя")
        {
            infoTXT.DOFade(1, 1);
            infoTXT.text = "Имя не может быть пустым";
            Debug.Log("Имя не может быть пустым");
            return;
        }

        if (newName.Length < 3 || newName.Length > 16)
        {
            infoTXT.DOFade(1, 1);
            infoTXT.text = "Имя должно быть от 3 до 16 символов";
            Debug.Log("Имя должно быть от 3 до 16 символов");
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(newName, @"^[a-zA-Zа-яА-ЯёЁ0-9 ]+$"))
        {
            infoTXT.DOFade(1, 1);
            infoTXT.text = "Можно использовать только буквы, цифры и пробелы";
            Debug.Log("Можно использовать только буквы, цифры и пробелы");
            return;
        }

        WebSocketBase.Instance.UpdatePlayerName(Geekplay.Instance.PlayerData.id, newName);
    }

    private void HandleNameUpdateResult(bool success, string newName)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (success)
            {
                infoTXT.DOFade(1, 1);
                infoTXT.text = "Имя успешно изменено";

                Geekplay.Instance.PlayerData.name = newName;
                Geekplay.Instance.Save();
                UpdateUI();
                changeNamePanel.SetActive(false);
                Debug.Log("Имя успешно изменено");
            }
            else
            {
                Debug.Log("Ошибка при изменении имени");
            }
        });
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