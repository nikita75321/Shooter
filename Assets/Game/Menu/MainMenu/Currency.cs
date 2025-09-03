using TMPro;
using UnityEngine;

public class Currency : MonoBehaviour
{
    public static Currency Instance;
    [SerializeField] private int money;
    [SerializeField] private TMP_Text[] moneyTxt;
    [SerializeField] private int donatMoney;
    [SerializeField] private TMP_Text[] donatTxt;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        money = Geekplay.Instance.PlayerData.money;
        donatMoney = Geekplay.Instance.PlayerData.donatMoney;

        UpdateMoneyTXT();
        UpdateDonatMoneyTXT();
    }

    public void UpdateAllTXT()
    {
        UpdateMoneyTXT();
        UpdateDonatMoneyTXT();
    }

    private void UpdateMoneyTXT()
    {
        money = Geekplay.Instance.PlayerData.money;
        foreach (var txt in moneyTxt)
        {
            txt.text = money.ToString();
        }
    }
    private void UpdateDonatMoneyTXT()
    {
        donatMoney = Geekplay.Instance.PlayerData.donatMoney;
        foreach (var txt in donatTxt)
        {
            txt.text = donatMoney.ToString();
        }
    }

    public void AddMoney(int value)
    {
        // Debug.Log($"AddMoney {value}");
        Geekplay.Instance.PlayerData.money += value;
        Geekplay.Instance.Save();

        // WebSocketBase.Instance.AddCurrency(value, 0);
        UpdateMoneyTXT();
    }
    public void AddDonatMoney(int value)
    {
        // Debug.Log($"AddDonatMoney {value}");
        Geekplay.Instance.PlayerData.donatMoney += value;
        Geekplay.Instance.Save();

        // WebSocketBase.Instance.AddCurrency(0, value);
        UpdateDonatMoneyTXT();
    }

    public bool SpendMoney(int value)
    {
        // Debug.Log($"SpendMoney {value}");
        if (money >= value)
        {
            Geekplay.Instance.PlayerData.money -= value;
            UpdateMoneyTXT();
            Geekplay.Instance.Save();
            return true;
        }
        return false;
    }
    public bool SpendDonatMoney(int value)
    {
        // Debug.Log($"SpendDonatMoney {value}");
        if (donatMoney > value)
        {
            Geekplay.Instance.PlayerData.donatMoney -= value;
            UpdateDonatMoneyTXT();
            Geekplay.Instance.Save();
            return true;
        }
        return false;
    }
}
