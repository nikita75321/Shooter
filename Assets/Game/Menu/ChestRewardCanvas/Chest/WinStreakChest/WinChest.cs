using TMPro;
using UnityEngine;

public class WinChest : MonoBehaviour
{
    [SerializeField] private ChestConfigSO chest;
    [SerializeField] private TMP_Text conditionTXT;

    [SerializeField] private GameObject menuCanvas, chestCanvas;

    public void Start()
    {
        UpdateUI();
    }
    private void OnEnable()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (Geekplay.Instance != null)
        if (Geekplay.Instance.PlayerData.winCaseValue > 3)
        {
            conditionTXT.text = $"3/3";
        }
        else
        {
            conditionTXT.text = $"{Geekplay.Instance.PlayerData.winCaseValue}/3";
        }

    }
    public void OpenChest()
    {
        if (Geekplay.Instance.PlayerData.winCaseValue >= 3)
        {
            InitChest();
            Geekplay.Instance.PlayerData.winCaseValue = 0;

            Geekplay.Instance.Save();
        }
        else
        {
            Debug.Log("Need more win");
        }
    }
    public void InitChest()
    {
        menuCanvas.SetActive(false);
        chestCanvas.SetActive(true);

        ChestRewardCanvas.Instance.InitChest(chest, true);
    }
}