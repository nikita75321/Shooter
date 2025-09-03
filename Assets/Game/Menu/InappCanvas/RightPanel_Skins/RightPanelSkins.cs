using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RightPanelSkins : MonoBehaviour
{

    public GameObject content;
    public GameObject chooseHero;

    public RewardConfig[] specialRewardArray;
    public Button[] buttonsHero;

    public void Start()
    {
        for (int i = 0; i < buttonsHero.Length; i++)
        {
            var index = i;
            buttonsHero[i].onClick.AddListener(() => ChooseHero(index));
        }
    }
    public void ChooseHero(int index)
    {
        for(int i = 0; i < Geekplay.Instance.PlayerData.persons[index].openSkinBody.Length; i++)
        {
            Geekplay.Instance.PlayerData.persons[index].openSkinBody[i] = 1;
        }
    }
}
