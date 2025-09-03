using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "InApp", menuName = "Geekplay/InApp")]
public class InAppSO : ScriptableObject
{
    public string rewardName;
    public UnityEvent rewardEvent;

    public void Subscribe(UnityAction action)
    {
    	rewardEvent.AddListener(action);
    }

    public void Unsubscribe(UnityAction action)
    {
        rewardEvent.RemoveListener(action);
    }

    public void UnsubscribeAll()
    {
        rewardEvent.RemoveAllListeners();
    }

    public void BuyItem() //открыть окно покупки
    {
        switch (Geekplay.Instance.Platform)
        {
            case Platform.Editor:
                Geekplay.Instance.PurchasedTag = rewardName;
                Reward();
                Debug.Log($"<color=yellow>PURCHASE: </color> {Geekplay.Instance.PurchasedTag}");
                break;
            case Platform.Yandex:
                Geekplay.Instance.PlayerData.lastBuy = rewardName;
                Geekplay.Instance.PurchasedTag = rewardName;
                string jsonString = "";
                jsonString = JsonUtility.ToJson(Geekplay.Instance.PlayerData);
                Utils.BuyItem(rewardName, jsonString);
                break;
            case Platform.VK:
                Geekplay_VK.Instance.purchasedTag = rewardName;
                UtilsVK.VK_RealBuy(rewardName);
                //ПОКУПКА ЗА ГОЛОСА
                break;
        }
    }

    public void Reward()
    {
    	rewardEvent.Invoke();
    }
}