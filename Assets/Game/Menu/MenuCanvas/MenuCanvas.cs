using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuCanvas : MonoBehaviour
{
    public static MenuCanvas Instance { get; private set; }

    [Header("Mode References")]
    [SerializeField] private Color[] modeColors;
    [SerializeField] private Sprite[] modeIcons;
    [SerializeField] private Sprite[] modeIconsInFrame;

    [SerializeField] private Image modeBack;
    [SerializeField] private Image modeIcon;
    [SerializeField] private Image modeIconInFrame;
    [SerializeField] private TMP_Text modeText;
    

    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private GameObject[] heroModels;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdatePlayerInfo();
        SwitchMode(0); // Режим по умолчанию
    }

    public void SwitchMode(int modeIndex)
    {
        Debug.Log($"SwitchMode index - {modeIndex}");
        modeText.text = GetModeName(modeIndex);
        SetModeVisual(modeIndex);
        Geekplay.Instance.PlayerData.currentMode = modeIndex + 1;
        Geekplay.Instance.Save();
    }

    private string GetModeName(int index)
    {
        return index switch
        {
            0 => "Сам за себя",
            1 => "Королевская битва",
            2 => "Красные против синих",
            _ => "Неизвестный режим"
        };
    }
    private void SetModeVisual(int index)
    {
        switch (index)
        {
            case 0:
                modeBack.color = modeColors[0];
                modeIcon.sprite = modeIcons[0];
                modeIconInFrame.sprite = modeIconsInFrame[0];
                break;
            case 1:
                modeBack.color = modeColors[1];
                modeIcon.sprite = modeIcons[1];
                modeIconInFrame.sprite = modeIconsInFrame[1];
                break;
            case 2:
                modeBack.color = modeColors[2];
                modeIcon.sprite = modeIcons[2];
                modeIconInFrame.sprite = modeIconsInFrame[2];
                break;
        }
    }

    private void UpdatePlayerInfo()
    {
        playerNameText.text = string.IsNullOrEmpty(Geekplay.Instance.PlayerData.name)
            ? "Без имени"
            : Geekplay.Instance.PlayerData.name;

        for (int i = 0; i < heroModels.Length; i++)
            heroModels[i].SetActive(i == Geekplay.Instance.PlayerData.currentHero);
    }
}