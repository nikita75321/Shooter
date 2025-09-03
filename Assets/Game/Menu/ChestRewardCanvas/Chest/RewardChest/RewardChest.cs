using UnityEngine;

public class RewardChest : Chest
{
    [SerializeField] private RewardSO rewardSO;

    public override void Start()
    {
        base.Start();

        rewardSO.Subscribe(GainReward);
    }

    public void WatchAd()
    {
        rewardSO.ShowRewardedAd();
    }

    public override void OpenChest()
    {
        ChestRewardCanvas.Instance.OpenCurrentChest();
    }

    private void GainReward()
    {
        Debug.Log("Gain reward");
        // ChestRewardCanvas.Instance.InitChest(this);
    }
}
