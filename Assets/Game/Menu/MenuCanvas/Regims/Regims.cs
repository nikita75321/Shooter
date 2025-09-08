using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Regims : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button mode1Button;
    [SerializeField] private Button mode2Button;
    [SerializeField] private Button mode3Button;
    [SerializeField] private GameObject mode2Lock;
    [SerializeField] private GameObject mode3Lock;
    [SerializeField] private TMP_Text mode2TimerText;
    [SerializeField] private TMP_Text mode3TimerText;

    [Header("InfoPanel")]
    [SerializeField] private Button firstInfoButton;
    [SerializeField] private Button secondInfoButton;
    [SerializeField] private Button thirdInfoButton;
    [SerializeField] private TMP_Text firstTXT;
    [SerializeField] private TMP_Text secondTXT;
    [SerializeField] private TMP_Text thirdTXT;

    private void OnEnable()
    {
        // Синхронизация при открытии
        GameModeService.Instance.regimsIsOpen = true;
        GameModeService.Instance.RequestUpdate();
        // UpdateDisplays();
    }

    private void OnDisable()
    {
        GameModeService.Instance.regimsIsOpen = false;
    }

    private void Start()
    {
        mode1Button.onClick.AddListener(() =>
        {
            MenuCanvas.Instance.SwitchMode(0);
            gameObject.SetActive(false);
            MainMenu.Instance.OpenMenu();
        });

        mode2Button.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            MainMenu.Instance.OpenMenu();
            if (GameModeService.Instance.IsMode2Available)
                MenuCanvas.Instance.SwitchMode(1);
        });
        mode3Button.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            MainMenu.Instance.OpenMenu();
            if (GameModeService.Instance.IsMode3Available)
                MenuCanvas.Instance.SwitchMode(2);
        });

        firstInfoButton.onClick.AddListener(() => ShowInfo(0));
        secondInfoButton.onClick.AddListener(() => ShowInfo(1));
        thirdInfoButton.onClick.AddListener(() => ShowInfo(2));
    }

    private void Update()
    {
        UpdateDisplays();
    }

    private void UpdateDisplays()
    {
        UpdateModeDisplay(
            GameModeService.Instance.IsMode2Available,
            GameModeService.Instance.Mode2TimeLeft,
            mode2Button,
            mode2Lock,
            mode2TimerText,
            "Королевская битва");
        //----------------------------КОГДА РЕЖИМ БУДЕТ ГОТОВ РАСКОМЕНТИТЬ----------------------------
        // UpdateModeDisplay(
        //     GameModeService.Instance.IsMode3Available,
        //     GameModeService.Instance.Mode3TimeLeft,
        //     mode3Button,
        //     mode3Lock,
        //     mode3TimerText,
        //     "Красные против синих");
        //----------------------------КОГДА РЕЖИМ БУДЕТ ГОТОВ РАСКОМЕНТИТЬ----------------------------
    }

    private void UpdateModeDisplay(bool isAvailable, float timeLeft,
                                 Button button, GameObject lockIcon,
                                 TMP_Text timerText, string modeName)
    {
        // Debug.Log("UpdateModeDisplay");
        button.interactable = isAvailable;
        lockIcon.SetActive(!isAvailable);

        if (isAvailable)
        {
            // timerText.text = $"{modeName}\n<color=green>Доступен {FormatTime(timeLeft)}</color>";
            timerText.text = $"<color=green>Доступен {FormatTime(timeLeft)}</color>";
        }
        else
        {
            if (timeLeft > 0)
            {
                // timerText.text = $"{modeName}\n<color=red>Откроется через {FormatTime(timeLeft)}</color>";
                timerText.text = $"<color=red>Откроется через {FormatTime(timeLeft)}</color>";
            }
        }
    }

    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        return $"{mins:D2}:{secs:D2}";
    }

    public void ShowInfo(int value)
    {
        switch (value)
        {
            case 0:
                firstTXT.text = "В битве учавствует 5 команд в каждой из которых по 3 бойца, зона битвы со временем сужается";
                secondTXT.text = "Истребляйте чужие команды и не забывайте поднимать павших товарищей";
                thirdTXT.text = "Останьтесь единственной живой командой";
                break;
            case 1:
                firstTXT.text = "В битве участвует по 12 бойцов";
                secondTXT.text = "Не забывайте искать лучшее снаряжение, чтобы быть готовым к бою";
                thirdTXT.text = "Последний выживший будет объявлен победителем";
                break;
            case 2:
                firstTXT.text = "В битве учавтсвуют 2 команды по 5 бойцов, бой длится 2 минуты";
                secondTXT.text = "Убивайте бойцов чужой команды, чтобы набирать очки";
                thirdTXT.text = "Побеждает та команда, которая набрала больше очков";
                break;
        }
    }

    private void OnDestroy()
    {
        mode1Button.onClick.RemoveListener(() =>
        {
            MenuCanvas.Instance.SwitchMode(0);
            gameObject.SetActive(false);
            MainMenu.Instance.OpenMenu();
        });

        mode2Button.onClick.RemoveListener(() =>
        {
            gameObject.SetActive(false);
            MainMenu.Instance.OpenMenu();
            if (GameModeService.Instance.IsMode2Available)
                MenuCanvas.Instance.SwitchMode(1);
        });
        mode3Button.onClick.RemoveListener(() =>
        {
            gameObject.SetActive(false);
            MainMenu.Instance.OpenMenu();
            if (GameModeService.Instance.IsMode3Available)
                MenuCanvas.Instance.SwitchMode(2);
        });

        firstInfoButton.onClick.RemoveListener(()=> ShowInfo(0));
        secondInfoButton.onClick.RemoveListener(()=> ShowInfo(1));
        thirdInfoButton.onClick.RemoveListener(()=> ShowInfo(2));
    }
}