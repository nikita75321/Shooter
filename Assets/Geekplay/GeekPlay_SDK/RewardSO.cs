using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using CrazyGames;

[CreateAssetMenu(fileName = "RewardAd", menuName = "Geekplay/RewardAD")]
public class RewardSO : ScriptableObject
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

    public bool ShowRewardedAd() //РЕКЛАМА С ВОЗНАГРАЖДЕНИЕМ - ПОКАЗАТЬ
    {
        switch (Geekplay.Instance.Platform)
        {
            
            case Platform.Editor:
                GeekplayEditor.Instance.onRewardAdStart.Invoke();
                Geekplay.Instance.RewardTag = rewardName;
                if (GeekplayEditor.Instance.emulateWeb)
                {
                    GeekplayEditor.Instance.EditorReward(EditorCor());
                }
                else
                {
                    Reward();
                    Geekplay.Instance.OnRewardAdClose.Invoke();
                }
                return true;
            case Platform.Yandex:
                Geekplay.Instance.RewardTag = rewardName;
                Utils.AdReward();
                return true;
            case Platform.VK:
                Geekplay_VK.Instance.RewardTag = rewardName;
                UtilsVK.VK_Rewarded();
                return true;
            case Platform.CrazyGames:
                Geekplay_Crazy.Instance.rewardTag = rewardName;
                CrazySDK.Ad.RequestAd(CrazyAdType.Midgame, () => // or CrazyAdType.Rewarded
                {
                    Geekplay_Crazy.Instance.StopMusAndGame();
                }, (error) =>
                {
                    Geekplay_Crazy.Instance.ResumeMusAndGame();
                }, () =>
                {
                    Reward();
                    Geekplay_Crazy.Instance.ResumeMusAndGame();
                });
                return true;
            case Platform.GameDistribution:
                Geekplay_GameDistribution.Instance.RewardTag = rewardName;
                if (Geekplay_GameDistribution.Instance.rewardAdLoaded)
                {
                    GameDistribution.Instance.ShowRewardedAd();
                    GameDistribution.Instance.PreloadRewardedAd();
                }
                return true;
        }
        return false;
    }

    public void Reward()
    {
    	rewardEvent.Invoke();
    }
    
    
    IEnumerator EditorCor()
    {
        GeekplayEditor.Instance.rewardPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        GeekplayEditor.Instance.rewardPanel.SetActive(false);
        Geekplay.Instance.OnRewardAdClose.Invoke();
        Reward();
    }
}