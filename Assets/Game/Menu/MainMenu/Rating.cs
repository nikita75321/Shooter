using TMPro;
using UnityEngine;

public class Rating : MonoBehaviour
{
    public static Rating Instance;
    public int Rate => rate;
    [SerializeField] private int rate;
    [SerializeField] private TMP_Text[] rateTxt;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        rate = Geekplay.Instance.PlayerData.rate;
        UpdateRatingTXT();
    }

    public void UpdateUI()
    {
        UpdateRatingTXT();
    }
    private void UpdateRatingTXT()
    {
        rate = Geekplay.Instance.PlayerData.rate;
        foreach (var txt in rateTxt)
        {
            txt.text = rate.ToString();
        }
    }

    public void AddRating(int value)
    {
        // RatingReward.Instance.AddRating(value);
        Geekplay.Instance.PlayerData.rate += value;
        if (Geekplay.Instance.PlayerData.rate > Geekplay.Instance.PlayerData.maxRate)
        {
            Geekplay.Instance.PlayerData.maxRate = Geekplay.Instance.PlayerData.rate;
        }
        else
            Debug.Log(2);

        Geekplay.Instance.Save();

        UpdateRatingTXT();
    }
    public void SpendRating(int value)
    {
        if (Geekplay.Instance.PlayerData.rate >= value)
        {
            Geekplay.Instance.PlayerData.rate -= value;
        }
        else
        {
            Geekplay.Instance.PlayerData.rate = 0;
        }
        
        UpdateRatingTXT();
        Geekplay.Instance.Save();
    }
}
