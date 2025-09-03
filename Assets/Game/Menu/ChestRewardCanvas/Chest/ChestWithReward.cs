using UnityEngine;
using UnityEngine.UI;

public class ChestWithReward : MonoBehaviour
{
    [SerializeField] private Image chestImage;
    [SerializeField] private RewardInChest rewardInChest;
    public void InitChest(Image image)
    {
        chestImage.sprite = image.sprite;
    }

    public void OpenChest()
    {
        Debug.Log("aga");
    }
}
