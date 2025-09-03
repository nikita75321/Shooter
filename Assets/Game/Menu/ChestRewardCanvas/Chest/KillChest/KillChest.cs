using TMPro;
using UnityEngine;

public class KillChest : MonoBehaviour
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
        if (Geekplay.Instance.PlayerData.killCaseValue > 15)
        {
            conditionTXT.text = $"15/15";
        }
        else
        {
            conditionTXT.text = $"{Geekplay.Instance.PlayerData.killCaseValue}/15";
        }

    }
    public void OpenChest()
    {
        if (Geekplay.Instance.PlayerData.killCaseValue >= 15)
        {
            InitChest();
            Geekplay.Instance.PlayerData.killCaseValue = 0;

            Geekplay.Instance.Save();
        }
        else
        {
            Debug.Log("Need more kill");
        }
    }
    public void InitChest()
    {
        menuCanvas.SetActive(false);
        chestCanvas.SetActive(true);

        ChestRewardCanvas.Instance.InitChest(chest, true);
    }
}
